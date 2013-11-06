using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public partial class ServerExplorer
    {
        /// <summary>
        ///  tasks for handling client requests
        ///  
        /// </summary>
        /// 

        public abstract class Task
        {
            protected const int m_acceptBuf = 36;
            protected const int m_commandBuf = 540;
            protected const int m_brdcstBuf = 36;

            protected Socket m_socket = null;
            public abstract void Run();
        }

        public class AuthorizeTask : Task
        {
            /// <summary>
            /// This is class for authorization of client 
            ///     in Run section we receive some string from client 
            ///     extract from this string password(in current 
            ///     realization this string contains only pass)
            ///     and compare with real pass and send "string result" 
            ///     (this our decision about authorization)
            ///     
            ///     methods for extracting pass from str
            ///     are contained in ProtocolModule 
            /// </summary>

            private string m_password;

            private bool m_isAuthorized = false;

            public bool isAuthorized { get { return m_isAuthorized; } }

            public AuthorizeTask(Socket _s, string _pass)
            {
                m_socket = _s;
                m_password = _pass;
            }

            public override void Run()
            {
                byte[] buf = new byte[m_acceptBuf];

                m_socket.Receive(buf, m_acceptBuf, SocketFlags.None);
                string param = Encoding.Unicode.GetString(buf);
                //debug mode:
                Debug.WriteLine("~~~~~Run method param: " + param);
                Debug.WriteLine("~~~~~Run method pass: " + m_password);
                string result = ProtocolModule.Authorize(ref m_isAuthorized, param, m_password);
                Debug.WriteLine("~~~~~Run method: asnwer " + result);
                m_socket.Send(Encoding.Unicode.GetBytes(result));
            }
        }

        public class BroadcastRequestTask : Task
        {

            public BroadcastRequestTask(Socket _rcvSocket)
            {
                m_socket = _rcvSocket;
            }

            public override void Run()
            {
                byte[] buf = new byte[m_brdcstBuf];
                m_socket.Receive(buf, m_brdcstBuf, SocketFlags.None);
                string rcvd = Encoding.Unicode.GetString(buf);
                // todo: check ip from rcvd socket
                // is there some nats or other devices which can change ip on the way
                // form socket for sending
                // for this: get ip and port
                //
                // must read whole book about networks >< fucking shit

            }
        }

        public class CommonTask : Task
        {
            public override void Run()
            {
                throw new NotImplementedException();
            }
        }

    }
}
