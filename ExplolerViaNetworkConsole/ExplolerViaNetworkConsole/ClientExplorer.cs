using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        private const int connectBuffer = 36;
        private Socket m_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public string m_password = "StupidClient99";

        public bool isConnected { get { return m_client.Connected; } }

        public bool Connect(IPAddress _ipAddr, string _pass)
        {

            bool result = false;
            IPEndPoint endPoint = new IPEndPoint(_ipAddr, Ports.serverTcpPort);
            ClientExplorerProtocol protocol = new ClientExplorerProtocol();
            m_client.ReceiveTimeout = 1000;
            try
            {
                m_client.Connect(endPoint);
                // todo: clear msg after receiving and sending
                // maybe =)
                byte[] msg = new byte[connectBuffer];
                m_client.Send(Encoding.Unicode.GetBytes(_pass));
                m_client.Receive(msg);
                result = ClientExplorerProtocol.isAccepted(Encoding.Unicode.GetString(msg));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                if (m_client.Connected)
                    m_client.Close();
            }
            return result;
        }

        public void Close()
        {
            if (m_client.Connected)
                m_client.Close();
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
            private static string m_broadcastPhrase = "";
            private static string m_serverAccept = "You_are_chosen_one";

            public static string ExtractMsg(string _a)
            {
                string num = "";
                int startIndex = -1;
                for (int i = 0; i < _a.Count(); i++)
                {
                    if (_a[i] != ' ') { num += _a[i]; }
                    else { startIndex = i + 1; break; }
                }
                if (startIndex < 0)
                    Debug.WriteLine("Client protocol Extract param: start index error\r\n");

                return _a.Substring(startIndex, Convert.ToInt32(num));
            }

            public static string WrapMsg(string _a)
            {
                int num = _a.Count();
                return (num.ToString() + " " + _a);
            }

            public static bool isAccepted(string _msg)
            {
                if (_msg == m_serverAccept)
                    return true;
                else
                    return false;
            }

            public static string BroadcastSearch()
            {
                return m_broadcastPhrase;
            }

            public static string Cat(string _fullpath)
            {
                return "";
            }

            public static string Ls(string fullpath)
            {
                return "";
            }
        }
    }
}
