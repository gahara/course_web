using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkConsole
{
    public class ClientTransferConnection : AbsTransferConnection
    {
        public ClientTransferConnection()
        {
            //debug
            m_type = "Client";
        }

        private void InitSocket()
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.ReceiveBufferSize = TransferConnectionProtocol.bufferSize;
            m_socket.ReceiveTimeout = TransferConnectionProtocol.receiveTimeout;
            m_socket.SendTimeout = TransferConnectionProtocol.sendTimeout;
        }

        public bool Connect(string _ip, int _port)
        {
            bool result = true;
            try
            {
                InitSocket();
                m_socket.Connect(new IPEndPoint(IPAddress.Parse(_ip), _port));
            }
            catch (Exception ex)
            {
                result = false;
                Debug.WriteLine(ex.Message);
            }
            return result;
        }
    }

    public class ClientBroadcastProtocol
    {
        public string m_type;

        private Object m_syncvar = new Object();
        private static HashSet<IPEndPoint> m_serverAddrs;

        private UdpClient m_connection;
        public byte[] m_info;
        private int m_serverUDPPort;

        public ClientBroadcastProtocol(int _port, int _serverPort)
        {
            m_serverUDPPort = _serverPort;
            m_info = Encoding.ASCII.GetBytes(_port.ToString());
            bool flag = true;
            while (flag)
            {
                try
                {
                    m_connection = new UdpClient(_port);
                    flag = false;
                }
                catch { _port++; }
            }
            m_serverAddrs = new HashSet<IPEndPoint>();
            Monitor.Enter(m_syncvar);
            this.BeginReceive();
        }

        private void SendAddrInfo()
        {
            m_connection.Send(m_info, m_info.Length, new IPEndPoint(IPAddress.Broadcast, m_serverUDPPort));
        }

        public IPEndPoint[] Stop()
        {
            Monitor.Enter(m_syncvar);
            Debug.WriteLine("Entered to stop client");
            return m_serverAddrs.ToArray();
        }

        public void Start()
        {
            Monitor.Exit(m_syncvar);
            Debug.WriteLine("Entered to start client");
            this.SendAddrInfo();
        }

        private void BeginReceive()
        {
            m_connection.BeginReceive(this.Receive, new object());
        }

        private void Receive(IAsyncResult _ar)
        {
            IPEndPoint ip = null;
            byte[] buf = m_connection.EndReceive(_ar, ref ip);
            string msg = Encoding.ASCII.GetString(buf);
            Debug.WriteLine("recvd:" + Encoding.ASCII.GetString(buf));
            if (Monitor.TryEnter(m_syncvar))
            {
                Debug.WriteLine("Entered to locked client receive block");
                //todo: regex server port 1234...
                Match m = Regex.Match(msg,(@"server port (\d+)"));
                if (m.Success)
                {
                    m_serverAddrs.Add(new IPEndPoint(ip.Address, Convert.ToInt32(m.Groups[1].Value)));
                }
                Monitor.Exit(m_syncvar);
            }
            BeginReceive();
        }
    }
}
