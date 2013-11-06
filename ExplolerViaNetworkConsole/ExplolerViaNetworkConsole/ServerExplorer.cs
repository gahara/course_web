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

            while (true)
            {
                readyList = (ArrayList)socketList.Clone();
                //todo: make const for  1000 (timeout)

                //rewrite accept part on commands too (maybe)
                Socket.Select(readyList, null, null, 1000);
                foreach (Socket s in readyList)
                {
                    if (s == (Socket)socketList[0])
                    {
                        Socket client = null;
                        try
                        {
                            client = s.Accept();
                            AuthorizeTask task = new AuthorizeTask(client, m_password);
                            task.Run();
                            if (task.isAuthorized)
                            {
                                //debug mode
                                socketList.Add(client);
                                Debug.WriteLine("Client accepted: " + ((IPEndPoint)client.RemoteEndPoint).Address.ToString());
                            }
                            else
                                client.Close();
                        }
                        catch (Exception ex)
                        {
                            // here can be error
                            // i dont really know, client.connected can be executed
                            // before null cheking client 
                            if (client != null && client.Connected)
                                client.Close();
                        }
                    }
                    else if (s == (Socket)socketList[1])
                    {
 
                    }
                    else
                    {
                        //byte[] buf = new byte[m_commandBuf];
                        //s.Receive(buf, m_commandBuf, SocketFlags.None);
                        //string cmdstr = Encoding.Unicode.GetString(buf);
                        //Command cmd = m_protocol.ParseCommand(cmdstr); 
                        // here must be Thread(cmd.run, socket s);
                    }
                }
            }
        }

        
    }
}
