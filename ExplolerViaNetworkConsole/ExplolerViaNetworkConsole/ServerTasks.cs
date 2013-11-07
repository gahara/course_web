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
        public abstract class AbsProtocolConnection
        {
            protected Socket m_connection;

            public abstract Command Receive();
            public abstract void Send(string msg);
        }

        public class TCPProtocolConnection : AbsProtocolConnection
        {
            private static byte m_maxAttempts = 10;

            private const int m_bufSz = 540;

            private ArrayList m_sockets = null;
            private Dictionary<Socket, AbsProtocolConnection> m_dictionary;
            private string m_password;
            private bool m_isAuthorized;
            private int m_authorizeAttempt;
            private byte[] m_rcvBuf = new byte[m_bufSz];

            public TCPProtocolConnection(ArrayList _sockets, Dictionary<Socket, AbsProtocolConnection> _dictionary,  Socket _connection, string _password)
            {
                m_dictionary = _dictionary;
                m_password = _password;
                m_connection = _connection;
                m_sockets = _sockets;
                m_isAuthorized = false;
                m_authorizeAttempt = 0;
            }

            private void Close()
            {
                if (m_connection.Connected) { m_connection.Close(); m_dictionary.Remove(m_connection); }
                m_sockets.Remove(m_connection);
            }

            public override Command Receive()
            {
                int count = m_connection.Receive(m_rcvBuf, m_bufSz, SocketFlags.None);
                if (count == 0)
                {
                    Close();
                    return ProtocolModule.
                }
                string msg = ProtocolModule.ExtractMsg(Encoding.Unicode.GetString(m_rcvBuf));
                Command result = null;
                
                if (m_isAuthorized) { result = ProtocolModule.ParseCommand(msg); }
                else
                {
                    m_authorizeAttempt++;
                    result = ProtocolModule.Authorize(ref m_isAuthorized, msg, m_password);
                    if (!m_isAuthorized && m_authorizeAttempt > 9) { this.Close(); }
                }
                return result;
            }

            public override void Send(string _msg)
            {
                //devide msg on packets
                if (m_connection.Connected)
                {
                    string[] packages = ProtocolModule.DevideMsg(_msg);
                    int count = packages.Count();
                    for (int i = 0; i < count; i++)
                        m_connection.Send(Encoding.Unicode.GetBytes(ProtocolModule.WrapMsg(packages[i], i, count))); 
                }
            }
        }

        public class UDPProtocolConnection : AbsProtocolConnection
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
        }
    }
}
