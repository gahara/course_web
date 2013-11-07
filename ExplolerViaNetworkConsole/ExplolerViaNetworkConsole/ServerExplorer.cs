using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public partial class ServerExplorer
    {
        private string m_password;
        private bool m_isPasswordCreated = false;

        public bool isPasswordCreated { get { return m_isPasswordCreated; } }
        public void SetPassword(string _pass)
        {
            //we can add some restrictions for pass
            //
            m_password = _pass;
            // if ok then 
            m_isPasswordCreated = true;
        }

        public void Start()
        {
            ArrayList readyList;
            ArrayList socketList = new ArrayList();
            
            socketList.Add(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
            socketList.Add(new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp));
            ((Socket)socketList[0]).Bind(new IPEndPoint(IPAddress.Any, Ports.serverTcpPort));
            ((Socket)socketList[1]).Bind(new IPEndPoint(IPAddress.Any, Ports.serverUdpPort));
            // 
            // todo: make const for max number in queue for connection
            ((Socket)socketList[0]).Listen(10);

            Dictionary<Socket, AbsProtocolConnection> dictionary = new Dictionary<Socket,AbsProtocolConnection>();
            //dictionary.Add((Socket)socketList[1], new UDPProtocolConnection((Socket)socketList[1]));

            AbsProtocolConnection connection;


            while (true)
            {
                readyList = (ArrayList)socketList.Clone();
                //todo: make const for  1000 (timeout)

                Socket.Select(readyList, null, null, 1000);
                foreach (Socket s in readyList)
                {
                    if (s == (Socket)socketList[0])
                    {
                        Socket clientSocket = null;
                        clientSocket = s.Accept();
                        AbsProtocolConnection clientConnection = new TCPProtocolConnection(socketList, dictionary, clientSocket, m_password);
                    } else
                    {
                        connection = dictionary[s];
                        Command cmd = connection.Receive();
                        connection.Send(cmd.Start());
                    }
                }
            }
        }

        
    }
}
