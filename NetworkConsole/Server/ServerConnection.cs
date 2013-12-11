using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkConsole
{
    using Server;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;

    public class ServerTransferConnection : AbsTransferConnection
    {
        public ServerTransferConnection(Socket _socket)
        {
            //debug
            m_type = "Server";

            m_socket = _socket;
            m_socket.ReceiveBufferSize = TransferConnectionProtocol.bufferSize;
            m_socket.ReceiveTimeout = TransferConnectionProtocol.receiveTimeout;
            m_socket.SendTimeout = TransferConnectionProtocol.sendTimeout;
        }

    }

    public class ServerBroadcastProtocol
    {
        protected UdpClient m_connection;
        public string m_type;
        public byte[] m_info;
        static int count = 0;
        public ServerBroadcastProtocol(int _port)
        {
            try
            {
                m_connection = new UdpClient(new IPEndPoint(IPAddress.Any,Constants.serverUDPPort));
                Log.Add("Броадкаст включен");
            }
            catch
            {
                Log.Add("Броадкаст отключен, порт занят");
            }
            //m_info = Encoding.ASCII.GetBytes(_ipAddr);
        }

        private void Send(IPEndPoint _ep)
        {
            m_info = Encoding.ASCII.GetBytes("server port " + Constants.serverTCPPort);
            m_connection.Send(m_info, m_info.Length, _ep);
        }


        private void Receive(IAsyncResult _ar)
        {
            IPEndPoint ip = null;
            byte[] buf = m_connection.EndReceive(_ar, ref ip);
            int port = Convert.ToInt32(Encoding.ASCII.GetString(buf));
            Debug.WriteLine("recvd:" + Encoding.ASCII.GetString(buf));
            Debug.WriteLine("ip = " + ip.Address.ToString());

            Send(new IPEndPoint(ip.Address, port));
            this.m_connection.BeginReceive(this.Receive, new object());
        }

        public void Start()
        {
            if (m_connection != null)
                m_connection.BeginReceive(this.Receive, new object());
        }
    }


}
