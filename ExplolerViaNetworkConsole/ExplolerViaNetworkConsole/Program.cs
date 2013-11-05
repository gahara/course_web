using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;

namespace ExplolerViaNetworkConsole
{
    public static class Ports
    {
        public static int serverTcpPort = 22701;
        public static int serverUdpPort = 22702;
    }

    [Serializable]
    public class FileObject
    {
        public FileObject(bool _isFile, string _name, long _size, DateTime _date)
        {
            isFile = _isFile;
            name = _name;
            size = _size;
            date = _date;
        }
        public readonly bool isFile;
        public readonly string name;
        public readonly long size;
        public readonly DateTime date;

        // debug info
        /*public string GetString()
        {
            string res;
            if (!isFile)            {
                res = "dir  ";
            }
            else {
                res = "file ";
            }
            int len = 15;
            if (name.Length >= len)
                res += name.Substring(0,len);
            else 
                res += name.PadRight(len);
            res += " ";
            res += ((size / 1024).ToString() + "KB").PadRight(10);
            res += " ";
            res += date.ToString();
            return res;
        }*/


    }

    public class DataNode
    {
        private List<DataNode> m_children = null;
        private DataNode m_parent = null;
        private bool m_isFile;
        private string m_name;
        private DateTime m_date;
        private long m_length;

        public DataNode(FileObject _f)
        {
            m_isFile = _f.isFile;
            m_name = _f.name;
            m_length = _f.size;
            m_date = _f.date;
        }

        //debug mode
        public DataNode()
        { }

        public void ParseChildrens (List<FileObject> _files)
        {
            m_children = new List<DataNode>();
            foreach (FileObject f in _files)
            {
                DataNode d = new DataNode(f);
                d.m_parent = this;
                m_children.Add(d);
            }
        }

        public bool isChldrnExist { get {
            if (isFile) return false;    
            return (m_children.Count > 0 ? true : false); 
            } 
        }
        public bool isRoot { get { return (m_parent == null); } }
        public bool isFile { get { return m_isFile; } }

    }

    public class ClientNetworkExplorer
    {
        private const int connectBuffer = 24;
        private Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public string pass = "StupidClient99";

        public bool isConnected { get { return clientSocket.Connected; } }

        public bool Connect(IPAddress _ipAddr, string _pass)
        {
            
            bool res = false;
            IPEndPoint endPoint = new IPEndPoint(_ipAddr, Ports.serverTcpPort);
            ClientExplorerProtocol protocol = new ClientExplorerProtocol();
            clientSocket.ReceiveTimeout = 1000;
            try {
                clientSocket.Connect(endPoint);
                // todo: clear msg after receiving and sending
                byte[] msg = new byte[connectBuffer];
                clientSocket.Send(Encoding.Unicode.GetBytes(_pass));
                clientSocket.Receive(msg);
                res = protocol.isAccepted(Encoding.Unicode.GetString(msg));
                res = true;
            }
            catch (Exception ex){
                Console.WriteLine(ex.Message);
                if (clientSocket.Connected)
                    clientSocket.Close();
            }
            return res;
        }

        public void Close()
        {
 
        }

        public IPAddress[] BroadcastServerSearch()
        {
            return new IPAddress[0];
        }

        public DataNode Ls(string _fullpath)
        {
            return new DataNode();
        }

        public string Cat(string _fullpath)
        {
            return "";
        }

        public class ClientExplorerProtocol
        {
            // UNIX-style description of commands
            private string m_broadcastPhrase = "";
            private string m_serverAccept = "You_are_chosen_one";

            public bool isAccepted(string _msg)
            {
                if (_msg == m_serverAccept)
                    return true;
                else
                    return false;
            }

            public string BroadcastSearch()
            {
                return m_broadcastPhrase;
            }

            public string Cat(string _fullpath)
            {
                return "";
            }

            public string Ls(string fullpath)
            {
                return "";
            }
        }
    }

    public class ServerExplorer
    {

        //private Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private ProtocolModule m_protoModule = new ProtocolModule();
        private const int m_acceptBuf = 36;
        private const int m_commandBuf = 540;

        public void CreatePassword(string _pass)
        {
            m_protoModule.CreatePassword(_pass);
        }

        public void Start()
        {
            ArrayList readyList;
            ArrayList socketList = new ArrayList();
            
            socketList.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            socketList.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp));
            ((Socket)socketList[0]).Bind(new IPEndPoint(IPAddress.Any, Ports.serverTcpPort));
            ((Socket)socketList[1]).Bind(new IPEndPoint(IPAddress.Any, Ports.serverUdpPort));
            //todo: make const for max number in queue for connection
            ((Socket)socketList[0]).Listen(10);

            while (true)
            {
                readyList = (ArrayList)socketList.Clone();
                //todo: make const for  1000 (timeout)

                //rewrite accept part on commands too (maybe)
                Socket.Select(readyList, null, null, 1000);
                foreach (Socket s in readyList)
                {
                    if (s == (Socket)socketList[0])
                    {
                        Socket newSocket = null;
                        byte[] buf = new byte[m_acceptBuf];
                        try
                        {
                            newSocket = s.Accept();
                            newSocket.ReceiveTimeout = 1000;
                            newSocket.Receive(buf, m_acceptBuf, SocketFlags.None);
                            string ans = m_protoModule.Authorize(Encoding.Unicode.GetString(buf));
                            newSocket.Send(Encoding.Unicode.GetBytes(ans));
                            if (m_protoModule.isAuthorised)
                                socketList.Add(
                        }
                        catch (Exception ex)
                        {
                            if (newSocket != null)
                                if (newSocket.Connected)
                                    newSocket.Close();
                        }
                    }
                    else if (s == (Socket)socketList[1])
                    {
 
                    }
                    else
                    {
                        byte[] buf = new byte[m_commandBuf];
                        s.Receive(buf, m_commandBuf, SocketFlags.None);
                        string cmdstr = Encoding.Unicode.GetString(buf);
                        Command cmd = m_protocol.ParseCommand(cmdstr); 
                        // here must be Thread(cmd.run, socket s);
                    }
                }
            }
        }

        /// <summary>
        ///  классы команд, которые послал клиент и которые сервер должен выполнить
        /// </summary>
        /// 

        public class ProtocolModule
        {
            private string m_serverAccept = "You_are_chosen_one";
            private string m_serverDecline = "Wrong_password";

            private string m_password;
            private bool m_isAuthorised;

            public bool isAuthorised { get { return m_isAuthorised; } }

            public void CreatePassword(string _pass)
            {
                m_password = _pass;
            }

            public string Authorize(string _param)
            {
                if (m_password == _param)
                {
                    m_isAuthorised = true;
                    return m_serverAccept;
                }
                else {
                    m_isAuthorised = false;
                    return m_serverDecline
                }
            }

            
            public Command ParseCommand(string _cmd)
            {
                if (_cmd.Substring(0, 3) == "ls ")
                {
                    return new LsCommand(_cmd.Substring(3));
                }
                else if (_cmd.Substring(0, 3) == "cat ")
                {
                    return new CatCommand(_cmd.Substring(4));
                }
                else {
                    return new UnknownCommand();
                }
            }
        }

        public abstract class Command
        {

            //todo: make error class 
            protected string m_parameter;

            //public bool isError { get { return m_isError; } }

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

    class Program
    {
        static void Main(string[] args)
        {
            FileObject t = new FileObject(true, "new", 12, new DateTime());
            object b = (object)t;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, b);
            ms.Position = 0;
            
            int size = (ms.ToArray()).Count();
            Console.WriteLine(size.ToString());
            Console.WriteLine(ms.ToArray());
            /*ServerExplorer explorer = new ServerExplorer();
            
            Thread threadS = new Thread(new ThreadStart(explorer.Start));
            threadS.Start();
            ClientNetworkExplorer client = new ClientNetworkExplorer();
            Thread.Sleep(1000);
            if (client.Connect(IPAddress.Parse("127.0.0.1"), "password"))
            {
                Console.WriteLine("Good connection");
            }*/
            Console.ReadKey();
            
            /*while (true) {
                Console.Write("Введите путь до директории: ");
                str = Console.ReadLine();
                if (str == "exit")
                {
                    break;
                }
               List<FileObject> files = explorer.Ls(str);
                if (files != null)
                {
                    Console.WriteLine("\r\nСписок файлов:");
                    foreach (FileObject f in files) {
                        Console.WriteLine(f.GetString());
                    }
                }
                Console.WriteLine("");
            }*/
        }
    }
}
