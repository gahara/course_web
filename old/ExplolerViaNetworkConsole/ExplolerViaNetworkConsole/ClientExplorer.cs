using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
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

        public void ParseChildrens(List<FileObject> _files)
        {
            m_children = new List<DataNode>();
            foreach (FileObject f in _files)
            {
                DataNode d = new DataNode(f);
                d.m_parent = this;
                m_children.Add(d);
            }
        }

        public bool isChldrnExist
        {
            get
            {
                if (isFile) return false;
                return (m_children.Count > 0 ? true : false);
            }
        }
        public bool isRoot { get { return (m_parent == null); } }
        public bool isFile { get { return m_isFile; } }

    }

    //todo: add async rcv from server when connection closing on server side
    // maybe create thread with locks and other shit for listening of closing effect

    public class ClientNetworkExplorer
    {
        public class TCPClientConnection
        {
            private Socket m_connection = null;
            private const int m_bufSz = 1200;
            private const int m_rcvTimeout = 1000;
            private byte[] m_buf = new byte[m_bufSz];
            private bool m_isConnected = false;

            public bool isConnected {get { return m_isConnected;}}

            public TCPClientConnection()
            {
                m_connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_connection.ReceiveTimeout = m_rcvTimeout;
            }

            public bool Connect(string _serverIp, int _port)
            {
                bool result = true;
                try
                {
                    m_connection.Connect(new IPEndPoint(IPAddress.Parse(_serverIp), _port));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    result = false;
                }
                m_isConnected = result;
                return result;
            }

            public void Send(string _msg)
            {
                string package = ClientExplorerProtocol.WrapMsg(_msg);
                m_connection.Send(Encoding.Unicode.GetBytes(package));     
            }

            public string Receive()
            {
                string result = "";
                bool isLastPackage = false;
                while (!isLastPackage)
                {
                    m_connection.Receive(m_buf);
                    result += ClientExplorerProtocol.ExtractMsg(Encoding.Unicode.GetString(m_buf), ref isLastPackage);
                }
                return result;
            }

            public void Close()
            {
                if (this.isConnected)
                    m_connection.Close();
            }
        }

        public TCPClientConnection m_connection;
        private bool m_isAuthorized = false;

        public bool isAuthorized { get { return m_isAuthorized;} }
        public bool isConnected { get { return m_connection.isConnected; } }


        public bool Connect(string _ip, int port)
        {
            m_connection = new TCPClientConnection();
            return m_connection.Connect(_ip, port);
        }

        public bool Authorize(string _pass)
        {
            bool result = false;
            m_connection.Send(_pass);
            string ans = m_connection.Receive();
            if (ClientExplorerProtocol.isAuthorized(ans))
            {
                result = true;
                m_isAuthorized = true;
            }
            return result;
        }

        public void Close()
        {
            m_connection.Close();
        }

        public IPAddress[] BroadcastServerSearch()
        {
            return new IPAddress[0];
        }

        public DataNode Ls(string _fullpath)
        {
            string msg = ClientExplorerProtocol.Ls(_fullpath);
            m_connection.Send(msg);
            string ans = m_connection.Receive();
            List<FileObject> list = ClientExplorerProtocol.LsAns(ans);
            if (list != null)
            {
                foreach (FileObject f in list)
                    Console.WriteLine(f.GetString());
            }
            return new DataNode();
        }

        public string Cat(string _fullpath)
        {
            return "";
        }

        public class ClientExplorerProtocol:ProtocolModule
        {
            // UNIX-style description of commands
            
            public static bool isAuthorized(string _msg)
            {
                if (_msg == ProtocolConstants.authorizeAccept)
                    return true;
                else
                    return false;
            }

            public static string BroadcastSearch()
            {
                return "";//m_broadcastPhrase;
            }

            public static string Cat(string _fullpath)
            {
                return ProtocolConstants.reqCatHeader + _fullpath;
            }

            public static string Ls(string fullpath)
            {
                return ProtocolConstants.reqLsHeader + fullpath;
            }

            public static List<FileObject> LsAns(string msg)
            {
                List<FileObject> result = null;
                if (msg.Substring(0, 11) == "ls ans err ")
                {
                    
                }
                else if (msg.Substring(0, 7) == "ls ans ")
                {
                    // FILEOBJECT STRUCT = 
                /// FILETYPE(f or d) + \t
                /// + FILENAME + \t
                /// + FILESIZE + \t
                /// + DATETIME(long ticks) + \n
                /// 
                    result = new List<FileObject>();
                    Regex regex = new Regex(@"([fd]{1})\t(.+?)\t(\d+)\t(\d+)\n", RegexOptions.Singleline);
                    foreach (Match m in regex.Matches(msg))
                    {
                        bool isFile = (m.Groups[1].Value == "f");
                        string filename = m.Groups[2].Value;
                        long filesize = Convert.ToInt64(m.Groups[3].Value);
                        long ticks = Convert.ToInt64(m.Groups[4].Value);
                        result.Add(new FileObject(
                            isFile,
                            filename,
                            filesize,
                            new DateTime(ticks)));
                    }
                }

                return result ;
            }

            public static string CatAns(string msg)
            {
                return "";
            }

        }
    }
}
