﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public partial class ServerExplorer
    {

        /// <summary>
        /// this is the wrapper for some
        /// functions for parsing requests
        /// </summary>
        /// todo: make absclass ProtocolModule and the inherit 2 realizations: server and client
        public class ProtocolModule
        {
            private static string m_serverAccept = "You_are_chosen_one";
            private static string m_serverDecline = "Wrong_password";

            public static string Authorize(ref bool _isSuccess, string _param, string _pass)
            {
                if (_pass == _param)
                {
                    _isSuccess = true;
                    return m_serverAccept;
                }
                else
                {
                    _isSuccess = false;
                    return m_serverDecline;
                }
            }

            public static string ExtractMsg(string _a)
            {
                string num = "";
                int startIndex = -1;
                for (int i = 0; i < _a.Count(); i++)
                {
                    if (_a[i] != ' ')   { num += _a[i];}
                    else                { startIndex = i + 1; break;}
                }
                if (startIndex < 0)
                    Debug.WriteLine("server protocol extract param: start index error\r\n");
                return _a.Substring(startIndex, Convert.ToInt32(num));
            }

            public static string WrapMsg(string _a)
            {
                int num = _a.Count();
                return (num.ToString() + " " + _a);
            }

            public static Command ParseCommand(string _cmd)
            {
                if (_cmd.Substring(0, 3) == "ls ")
                {
                    return new LsCommand(_cmd.Substring(3));
                }
                else if (_cmd.Substring(0, 3) == "cat ")
                {
                    return new CatCommand(_cmd.Substring(4));
                }
                else
                {
                    return new UnknownCommand();
                }
            }
        }


        /// <summary>
        /// commands class give us interface for
        /// forming 
        /// </summary>
        public abstract class Command
        {
            // todo: make error class 
            // maybe
            protected string m_parameter;
            public abstract String[] Start();
        }

        public class CatCommand : Command
        {
            public CatCommand(string _param)
            {
                m_parameter = _param;
            }

            public override string[] Start()
            {
                throw new NotImplementedException();
            }
        }

        public class LsCommand : Command
        {
            const int m_errNoPath = 100;
            const string m_ansHeader = "ls ans ";
            const string m_errHeader = "ls ans err ";
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
                        -1,
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

            public override string[] Start()
            {
                List<FileObject> files = Ls(m_parameter);
                string[] result = null;
                if (files == null)
                {
                    result = new string[1];
                    result[0] = m_errHeader + m_errNoPath.ToString();
                }
                else
                {

                }
                return result;
            }
        }

        public class UnknownCommand : Command
        {
            public override string[] Start()
            {
                return null;
            }
        }

    }
}
