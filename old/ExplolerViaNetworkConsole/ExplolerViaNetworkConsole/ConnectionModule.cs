using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public abstract class AbsTCPConnection
    {
        // think about closing sockets
        // does they close automatically or do i must do that
        protected Socket m_socket;
        protected bool m_isConnected;
        protected byte[] buf = new byte[ProtocolConstants.maxBufSize];

        public bool isConnected { get { return m_isConnected; } }
        public Socket GetSocket() { return m_socket; }
        public bool Send(string _msg)
        {
            bool result = true;
            try
            {
                string[] packages = ProtocolModule.WrapMsg(_msg);
                for (int i = 0; i < packages.Count(); i++)
                    m_socket.Send(Encoding.Unicode.GetBytes(packages[i]));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                m_isConnected = false;
                result = false;
            }
            return result;
        }

        public bool Receive(ref string _msg)
        {
            bool result = true;
            _msg = "";

            try
            {
                bool isLastPackage = false;
                while(!isLastPackage) {
                    int count = m_socket.Receive(buf, ProtocolConstants.maxBufSize, SocketFlags.None);
                    if (count == 0) throw new Exception("Connection closed");
                    _msg += ProtocolModule.ExtractMsg(
                        Encoding.Unicode.GetString(buf),
                        ref isLastPackage);
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                m_isConnected = false;
                result = false;
            }
            return result;
        }
    }


}
