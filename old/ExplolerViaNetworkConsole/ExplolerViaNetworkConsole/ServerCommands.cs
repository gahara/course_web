using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public partial class ServerExplorer
    {

        /// <summary>
        /// commands class give us interface for
        /// transforming real actions to answer string
        /// </summary>
        public abstract class Command
        {
            protected string m_parameter;
            public abstract String Start();
        }

        public class NullCommand : Command
        {
            public NullCommand() { }
            public override string Start() { return ""; }
        }

        public class CatCommand : Command
        {
            public CatCommand(string _param)
            {
                m_parameter = _param;
            }

            public override string Start()
            {
                throw new NotImplementedException();
            }
        }

        public class LsCommand : Command
        {
            const int m_maxPackageLen = 2048;
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

            private List<FileObject> Ls(string _fullpath)
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

            public LsCommand(string _param)
            {
                m_parameter = _param;
            }

            private string PackFileObject(FileObject _file)
            {
                // FILEOBJECT STRUCT = 
                /// FILETYPE(f or d) + \t
                /// + FILENAME + \t
                /// + FILESIZE + \t
                /// + DATETIME(long ticks) + \n
                string devider = "\t";
                string fieldDevider = "\n";
                string result = "";
                if (_file.isFile) { result += "f"; }
                else { result += "d"; }
                result += devider;
                result += _file.name;
                result += devider;
                result += _file.size.ToString();
                result += devider;
                result += _file.date.Ticks.ToString();
                result += fieldDevider;
                return result;
            }

            public override string Start()
            {
                List<FileObject> files = Ls(m_parameter);
                string result = "";
                if (files == null)
                {
                    // CURRENT BRANCH => IF (FILES == NULL) THEN THERE IS NO PATH
                    result = ProtocolConstants.ansLsErrHeader;
                }
                else
                {
                    result = ProtocolConstants.ansLsHeader;
                    for (int i = 0; i < files.Count; i++)
                        result += PackFileObject(files[i]);
                }
                return result;
            }
        }

        public class UnknownCommand : Command
        {
            public override string Start() { return ""; }
        }

        public class AuthorizeCommand : Command
        {
            private bool m_authorized;
            public AuthorizeCommand(bool _authorized)
            {
                m_authorized = _authorized;
            }
            public override string Start()
            {
                return m_authorized ? ProtocolConstants.authorizeAccept : ProtocolConstants.authorizeDecline ; 
            }
        }

    }
}
