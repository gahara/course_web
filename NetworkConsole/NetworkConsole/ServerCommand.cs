using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkConsole
{
    public partial class ClientConnection
    {
        public abstract class AbsCommand
        {
            public static AbsCommand ParseCommand(string _cmd, ref Object _memory)
            {
                AbsCommand cmd = null;
                Debug.WriteLine("command received: " + _cmd);
                if (_cmd.Substring(0, Constants.cmdLs.Length) == Constants.cmdLs)
                {
                    cmd = new LsCommand(_cmd.Substring(Constants.cmdLs.Length));
                }
                else if (_cmd.Substring(0, Constants.cmdCat.Length) == Constants.cmdCat)
                {
                    int len = Constants.cmdCat.Length;
                    bool isFirst = true;
                    if (_cmd[len] == Constants.optCatFirst[0])
                        len += Constants.optCatFirst.Length;
                    else
                    {
                        len += Constants.optCatNext.Length;
                        isFirst = false;
                    }
                    cmd = new CatCommand(ref _memory, _cmd.Substring(len), isFirst);
                }
                else
                {
                    cmd = new UnknownCommand();
                }
                return cmd;
            }

            protected string m_parameter;
            public abstract string Run();
        }

        public class AuthCommand : AbsCommand
        {
            private string m_result;
            public AuthCommand(bool _isAuthorized, bool _isCloseConnection)
            {
                if (_isCloseConnection) { m_result = Constants.authWrongPasswordCloseConnection; }
                else if (_isAuthorized) { m_result = Constants.authRightPassword; }
                else { m_result = Constants.authWrongPassword; }
            }

            public override string Run()
            {
                return m_result;
            }
        }

        public class UnknownCommand : AbsCommand
        {
            public UnknownCommand() { }

            public override string Run()
            {
                return Constants.ansCmdUnknownHeader;
            }
        }

        public abstract class AbsLsCommand : AbsCommand
        {
            protected abstract List<FileObject> Ls(string _fullpath);

            protected abstract List<FileObject> GetDrives();

            public override string Run()
            {
                bool isGood = true;
                string header;
                string package = "";
                List<FileObject> files = null;
 	            try
                {
                    if (m_parameter == @"\")
                        files = GetDrives();
                    else
                        files = Ls(m_parameter);
                } catch (Exception ex) { isGood = false; }
                if (files != null)
                {
                    header = Constants.ansLs;
                    foreach (FileObject f in files)
                        package += f.ToString() + '\n';
                } else
                {
                    header = Constants.ansLsError;
                    if (isGood) { header += Constants.errNoPath;}
                    else {header += Constants.errUnknown;}
                }

                return header + package;
            }
        }

        public class LsCommand : AbsLsCommand
        {
            private List<FileObject> ParseObjects(FileInfo[] _files)
            {
                List<FileObject> result = new List<FileObject>();
                for (int i = 0; i < _files.Count(); i++)
                {
                    result.Add(new FileObject(
                        true,
                        _files[i].Name,
                        _files[i].Length,
                        _files[i].LastWriteTime
                        ));
                }
                return result;
            }

            private List<FileObject> ParseObjects(DirectoryInfo[] _dirs)
            {
                List<FileObject> result = new List<FileObject>();
                for (int i = 0; i < _dirs.Count(); i++)
                {
                    result.Add(new FileObject(
                        false,
                        _dirs[i].Name,
                        0,
                        _dirs[i].LastWriteTime
                        ));
                }
                return result;
            }

            protected override List<FileObject> GetDrives()
            {
                List<FileObject> result = new List<FileObject>();
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    result.Add(new FileObject(false, d.Name, 0, new DateTime(0)));
                }
                return result;
            }

            protected override List<FileObject> Ls(string _fullpath)
            {
                //todo: implement root search
                DirectoryInfo dirInfo = new DirectoryInfo(_fullpath);
                
                if (!dirInfo.Exists)
                {
                    return null;
                }
                DirectoryInfo[] dirs = dirInfo.GetDirectories();
                FileInfo[] files = dirInfo.GetFiles();

                List<FileObject> result = ParseObjects(files);
                result.AddRange(ParseObjects(dirs));

                return result;
            }

            public LsCommand( string _param)
            {
                m_parameter = _param;
            }
        }


        public class CatCommand : AbsCommand
        {

            public class MemoryModule {

                private bool m_isPathExists;
                private long m_fileLength;
                private long m_bytesRead;
                public string filename;
                private FileStream m_fs;
                public byte[] m_buf;

                public bool isPathExists { get { return m_isPathExists; } }

                public bool isEnd { get { return (m_bytesRead == m_fileLength && m_fileLength != 0); } } 

                public MemoryModule(int _bufSize)
                {
                    m_buf = new byte[_bufSize];
                    m_fileLength = 0;
                    m_bytesRead = 0;
                }

                public bool OpenFile(string _filename)
                {
                    bool result = false;
                    try
                    {
                        if (File.Exists(_filename))
                        {
                            m_fileLength = (new FileInfo(_filename)).Length;
                            m_fs = File.OpenRead(_filename);
                            m_bytesRead = 0;
                            result = true;
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    m_isPathExists = result;
                    return result;
                }

                public bool ReadPortion(ref int _count, int _maxSize) // buf must be
                {
                    //only even _maxSize, because after we shall convert to utf8 string
                    bool result = false;
                    try
                    {
                        _count = m_fs.Read(m_buf, 0, _maxSize);
                        m_bytesRead += _count;
                        result = true;
                        if (this.isEnd) { m_fs.Close(); }
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    return result;
                }
           }

            private MemoryModule m_memory;
            public CatCommand(ref Object _memory, string _param, bool _isFirst)
            {
                Debug.WriteLine("is first: " + _isFirst.ToString());

                if (_isFirst)
                {
                    if (_memory == null) { _memory = new MemoryModule(Constants.filePackageSize); }
                    m_memory = (MemoryModule)_memory;
                    m_memory.OpenFile(_param);
                }
                else
                {
                    m_memory = (MemoryModule)_memory;
                }

            }

            public override string Run()
            {
                string msg = "";
                if (m_memory.isPathExists)
                {
                    int count = 0;
                    if (m_memory.ReadPortion(ref count, Constants.filePackageSize))
                    {
                        msg = Constants.ansCat;
                        Debug.WriteLine("cat read :" + count.ToString() + " bytes");
                        if (m_memory.isEnd)
                            { msg += Constants.ansCatLastEven; }
                        else { msg += Constants.ansCatNotLast; }
                        msg += Encoding.Default.GetString(m_memory.m_buf, 0, count);
                    } else { msg = Constants.ansCatError + Constants.errUnknown; }
                }
                else { msg = Constants.ansCatError + Constants.errNoPath; }
                return msg;
            }
        }
    }
}