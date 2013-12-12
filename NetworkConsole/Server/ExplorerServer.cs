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
            private byte m_attempts; // кол-во попыток ввода пароля, которые совершил пользователь
            private byte m_maxAttempts; // макс кол-во попыток
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
        private List<Socket> m_removeSockets; // список сокетов, которые закрылись и их надо удалить из списков для select
        private ServerTransferConnection m_connection;
        private ServerAuthorizationModule m_auth;
        private Object m_memory; // здесь хранится инфа о файле, который запросил клиент
        private int m_ID; // ID клиента

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
			// если при приеме возникла ошибка, то отключаемся
            if (!m_connection.Receive(ref msg))
            {
                this.CloseConnection();
                return;
            }
			// нужно ли отключится в конце(накапливаем флаги, если хотя бы один true, то в конце закрываем коннекшн)
            bool isDelete = false; 
            if (m_auth.isAuthorized) // если клиент авторизован, то обрабатываем команды
            {
                cmd = AbsCommand.ParseCommand(msg, ref m_memory, m_ID);
            }
            else 
            { // если клиент не авторизован, то проверяем пароль, который он прислал
                m_auth.Authorize(msg);
                cmd = new AuthCommand(m_auth.isAuthorized, m_auth.isClose);
                isDelete |= m_auth.isClose;
            }

            ans = cmd.Run(); // запуск команды и получение сообщения для отправки клиенту
            isDelete |= !m_connection.Send(ans);
            if (isDelete) {this.CloseConnection();} // закрываем конекшн, если isDelete
        }
    }

    public class ExplorerServer
    {
        private string m_password;
        private Dictionary<Socket, ClientConnection> m_clients; // словарь ключ - сокет, значение - clientconnection
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
		
		// инициализация сервера
        private ArrayList InitServer()
        {
            ArrayList result = new ArrayList(); // arraylist для select
            result.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)); // добавляем в arraylist слушающий сокет
            this.InitListenSocket((Socket)result[0]);
            ((Socket)result[0]).Listen(10);

            m_password = "password";

            m_brConnection.Start(); // старт броадкаста
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
		
		// запуск сервера
        public void Start()
        {
            ArrayList socketList = InitServer(); // список сокетов для select
            ArrayList readyList; // здесь список сокетов, на которые что-то поступило
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
                    if (s == (Socket)socketList[0]) // если запрос на подключение, то создаем нового клиента
                    {
                        Socket cl_sock = s.Accept();
                        client = new ClientConnection(cl_sock, delCandidates, m_password, clientCount++);
                        m_clients.Add(cl_sock, client);
                        socketList.Add(cl_sock);
                    }
                    else { // если не запрос на подключение, а какая-то команда от клиента, то получаем ее и запускаем обработку
                        Debug.WriteLine("Server in dictionary");
                        client = m_clients[s];
                        client.Start();
                    }
                    lock (delCandidates)
                    {
						// если есть отключенные клиенты, то чистим от них socketList и  словарь
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
