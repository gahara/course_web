using System;
using System.Collections.Generic;
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
                if (_cmd.Substring(0, Constants.cmdLs.Length) == Constants.cmdLs)
                {
                    cmd = new LsCommand(_cmd.Substring(Constants.cmdLs.Length));
                }
                else if (_cmd.Substring(0, Constants.cmdCat.Length) == Constants.cmdCat)
                {
                    cmd = new CatCommand(ref _memory, _cmd.Substring(Constants.cmdCat.Length));
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
                if (_isCloseConnection) { m_parameter = Constants.authWrongPasswordCloseConnection; }
                else if (_isAuthorized) { m_parameter = Constants.authRightPassword; }
                else { m_parameter = Constants.authWrongPassword; }
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

            public override string Run()
            {
                bool isGood = true;
                List<FileObject> files = null;
 	            try
                {
                    files = Ls(m_parameter);
                } catch (Exception ex) { isGood = false; }
                return "";
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

            protected override List<FileObject> Ls(string _fullpath)
            {
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

        public class AbsCatCommand : AbsCommand
        {
   
        }

        public class CatCommand : AbsCatCommand
        {
            class MemoryModule
            {

            }
            private MemoryModule m_memory;
            public CatCommand(ref Object _memory, string _param)
        }
    }

}
