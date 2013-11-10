using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public partial class ServerExplorer
    {
        public class ServerTCPConnection: AbsTCPConnection
        {
            public ServerTCPConnection(Socket _socket)
            {
                m_socket = _socket;
                m_isConnected = true;
            }
        }


        public class ExplorerClientHost
        {
            private static byte m_maxAttempts = 10;
            private ArrayList m_sockets = null;
            private ServerTCPConnection m_connection;
            private Dictionary<Socket, ExplorerClientHost> m_dictionary;
            private string m_password;
            private bool m_isAuthorized;
            private int m_authorizeAttempt;

            public ExplorerClientHost(
                ArrayList _sockets, 
                Dictionary<Socket, ExplorerClientHost> _dictionary,  
                Socket _connection, 
                string _password)
            {
                m_dictionary = _dictionary;
                m_password = _password;
                m_sockets = _sockets;
                m_isAuthorized = false;
                m_authorizeAttempt = 0;
                m_connection = new ServerTCPConnection(_connection);
                m_sockets.Add(_connection);
                m_dictionary.Add(_connection, this);

            }

            private void Close()
            {
                m_dictionary.Remove(m_connection.GetSocket());
                m_sockets.Remove(m_connection.GetSocket());
            }

            public Command Receive()
            {
                string msg = "";
                if (!m_connection.Receive(ref msg))
                {
                    Close();
                    return ServerProtocolModule.ReturnNullCommand();
                }

                Command result = null;                
                if (m_isAuthorized) { result = ServerProtocolModule.ParseCommand(msg); }
                else
                {
                    m_authorizeAttempt++;
                    result = ServerProtocolModule.Authorize(ref m_isAuthorized, msg, m_password);
                    if (!m_isAuthorized && m_authorizeAttempt > m_maxAttempts-1) { this.Close(); }
                }
                return result;
            }

            public void Send(string _msg)
            {
                if (!m_connection.Send(_msg)) { Close();}
            }
        }

        /*public class UDPProtocolConnection : AbsProtocolConnection
        {
            private const int m_brdcstBuf = 36;

            public UDPProtocolConnection(Socket _connection)
            {
                m_connection = _connection;
            }

            public override Command Receive()
            {
                throw new NotImplementedException();
            }

            public override void Send(string _msg)
            {
                throw new NotImplementedException();
            }
        }*/
    }
}
