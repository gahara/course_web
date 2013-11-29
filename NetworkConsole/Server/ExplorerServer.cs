using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkConsole
{
    using Server;
    public partial class ClientConnection
    {
        public class ServerAuthorizationModule
        {
            private string m_password;
            private byte m_attempts;
            private byte m_maxAttempts;
            private bool m_isAuthorized;

            public bool isAuthorized { get { return m_isAuthorized; } }
            public bool isClose { get { return (m_maxAttempts <= m_attempts && !m_isAuthorized); } }

            public void SetPassword(string _pass)
            {
                m_password = _pass;
            }

            public ServerAuthorizationModule(string _password)
            {
                this.SetPassword(_password);
                m_attempts = 0;
                m_maxAttempts = Constants.authMaxAttempts;
                m_isAuthorized = false;
            }

            public bool Authorize(string _param)
            {
                m_attempts++;
                if (_param == m_password) { m_isAuthorized = true; }
                return m_isAuthorized;
            }

        }
    }

    public partial class ClientConnection
    {
        private List<Socket> m_removeSockets;
        private ServerTransferConnection m_connection;
        private ServerAuthorizationModule m_auth;
        private Object m_memory;
        private int m_ID;

        private void AddLog(string _msg)
        {
            Log.Add(_msg);
        }

        public void SetPassword(string _password)
        {
            m_auth.SetPassword(_password);
        }

        

        public ClientConnection(Socket _socket, List<Socket> _removeSocket, string _password, int _clientNum)
        {
            m_ID = _clientNum;
            m_removeSockets = _removeSocket;
            m_connection = new ServerTransferConnection(_socket);
            m_auth = new ServerAuthorizationModule(_password);
            m_memory = null;
            IPEndPoint endPoint = (IPEndPoint)_socket.RemoteEndPoint;
            AddLog("Клиент " + m_ID.ToString() + " подключился" + ", адрес " + endPoint.Address.ToString() + ":" + endPoint.Port.ToString());
        }

        public void CloseConnection()
        {
            lock (m_removeSockets)
            {
                m_removeSockets.Add(m_connection.GetSocket());
            }
            AddLog("Клиент " + m_ID.ToString() + " отключился");
        }

        public void Start()
        {
            string msg = "";
            string ans = "";
            AbsCommand cmd;
            if (!m_connection.Receive(ref msg))
            {
                this.CloseConnection();
                return;
            }
            bool isDelete = false;
            if (m_auth.isAuthorized)
            {
                cmd = AbsCommand.ParseCommand(msg, ref m_memory, m_ID);
            }
            else 
            {
                m_auth.Authorize(msg);
                cmd = new AuthCommand(m_auth.isAuthorized, m_auth.isClose);
                isDelete |= m_auth.isClose;
            }

            ans = cmd.Run();
            isDelete |= !m_connection.Send(ans);
            if (isDelete) {this.CloseConnection();}
        }
    }

    public class ExplorerServer
    {
        private string m_password;
        private Dictionary<Socket, ClientConnection> m_clients;
        private List<Socket> m_socketForRemove;
        private ServerBroadcastProtocol m_brConnection;

        private void AddLog(string _msg)
        {
            Log.Add(_msg);
        }

        public ExplorerServer()
        {
            m_clients = new Dictionary<Socket, ClientConnection>();
            m_socketForRemove = new List<Socket>();
            if (!ThreadPool.SetMaxThreads(20, 10)) {throw new Exception("Thread pool exception");}
            m_brConnection = new ServerBroadcastProtocol(Constants.serverUDPPort);
        }

        private void InitListenSocket(Socket _lstn)
        {
            bool flag = true;
            while (flag)
            {
                try {
                    _lstn.Bind(new IPEndPoint(IPAddress.Any, Constants.serverTCPPort));
                    flag = false;
                }
                catch { Constants.serverTCPPort++; }
            }
            
            IPHostEntry host;
            string localIP = "?";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    localIP = ip.ToString();
                    AddLog("Сервер " + localIP + " : " + Constants.serverTCPPort.ToString());
                }
            }
        }

        private ArrayList InitServer()
        {
            ArrayList result = new ArrayList();
            result.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            this.InitListenSocket((Socket)result[0]);
            ((Socket)result[0]).Listen(10);

            m_password = "password";

            m_brConnection.Start();
            return result;
        }

        public string Password
        {
            get { return m_password; }
            set 
            {
                foreach (ClientConnection c in m_clients.Values)  { c.SetPassword(value);}
                m_password = value;
            }
        }

        public void Start()
        {
            ArrayList socketList = InitServer();
            ArrayList readyList;
            List<Socket> delCandidates = new List<Socket>();
            ClientConnection client;

            AddLog("Сервер запущен");
            int clientCount = 0;
            while (true)
            {
                readyList = (ArrayList)socketList.Clone();
                Socket.Select(readyList, null, null, 1000);
                foreach (Socket s in readyList)
                {
                    if (s == (Socket)socketList[0])
                    {
                        Socket cl_sock = s.Accept();
                        client = new ClientConnection(cl_sock, delCandidates, m_password, clientCount++);
                        m_clients.Add(cl_sock, client);
                        socketList.Add(cl_sock);
                    }
                    else {
                        Debug.WriteLine("Server in dictionary");
                        client = m_clients[s];
                        client.Start();
                        //ThreadPool.QueueUserWorkItem(new WaitCallback(client.Start));
                    }
                    lock (delCandidates)
                    {
                        foreach (Socket delS in delCandidates)
                        {
                            m_clients.Remove(delS);
                            socketList.Remove(delS);
                            
                        }
                        delCandidates.Clear();
                    }
                }
            }
        }

        public void Stop()
        {
            foreach (Socket s in m_clients.Keys)
            {
                s.Close();
            }
            //todo: add close for broadcast
        }

    }
}
