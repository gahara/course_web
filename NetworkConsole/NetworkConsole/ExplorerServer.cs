using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkConsole
{
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

        public void SetPassword(string _password)
        {
            m_auth.SetPassword(_password);
        }

        public ClientConnection(Socket _socket, List<Socket> _removeSocket, string _password)
        {
            m_removeSockets = _removeSocket;
            m_connection = new ServerTransferConnection(_socket);
            m_auth = new ServerAuthorizationModule(_password);
            m_memory = null;
        }

        public void CloseConnection()
        { 
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
                cmd = AbsCommand.ParseCommand(msg, ref m_memory);
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
        public ExplorerServer()
        {
            m_clients = new Dictionary<Socket, ClientConnection>();
            m_socketForRemove = new List<Socket>();
            if (!ThreadPool.SetMaxThreads(20, 10)) {throw new Exception("Thread pool bo bo bo");}

        }

        public bool SetPassword(string _password)
        {
            bool result = true;
            
            if (_password.Length < 8) { result = false; }
            else 
            { 
                m_password = _password; 
            }

            return result;
        }

        public void Start
        {
 
        }
    }
}
