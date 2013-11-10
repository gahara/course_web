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
    public abstract class AbsTransferConnection
    {
        protected Socket m_socket;
        protected string m_type = "";
        protected byte[] m_buffer = new byte[TransferConnectionProtocol.bufferSize];

        public void Close()
        {
            //todo: check autoclosing
            // if another side close socket;
            m_socket.Close();
        }

        public bool Send(string _message)
        {
            bool result = true;
            try
            {
                int count = TransferConnectionProtocol.CountPackages(_message);
                for (int i = 0; i < count; i++)
                {
                    string package = TransferConnectionProtocol.GetNextPackage(ref _message, i + 1, count);
                    Debug.WriteLine(m_type + ": " + " began send " + (i+1).ToString() + " package");
                    m_socket.Send(Encoding.Unicode.GetBytes(package));
                    Thread.Sleep(300); ///WTF!!!!!!!!!!!!!!!!!!!!!! Ебаное говно
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(m_type + ": " + "caused exception in send");
                Debug.WriteLine(ex.Message);
                result = false;
            }
            return result;
        }

        public bool Receive(ref string _message)
        {
            bool result = true;
            _message = "";
            try
            {
                bool isLastPackage = false;
                bool isPenultimatePackage = false;
                string rawData = "";
                while (!isLastPackage)
                {
                    Debug.WriteLine(m_type + ": " + "began receive");
                    int count = m_socket.Receive(m_buffer);
                    if (count == 0) throw new Exception("Connection closed");
                    rawData += Encoding.Unicode.GetString(m_buffer).Substring(0, count / 2); // 2 because UTF-16
                    Debug.WriteLine("rcv: " + rawData);
                    string tmpMsg = "";
                    if (TransferConnectionProtocol.ExtractMessage(
                        ref rawData, ref tmpMsg, ref isPenultimatePackage, ref isLastPackage))
                    {
                        _message += tmpMsg;
                        if (isPenultimatePackage)
                            if (TransferConnectionProtocol.ExtractMessage(
                                ref rawData, ref tmpMsg, ref isPenultimatePackage, ref isLastPackage)) { _message += tmpMsg; }
                    }
                    else
                    {
                        throw new Exception("Wrong package");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(m_type + ": " + "caused exception in receive");
                Debug.WriteLine(ex.Message);
                result = false;
            }
            return result;
        }

    }

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

    public class ClientTransferConnection : AbsTransferConnection
    {
        public ClientTransferConnection()
        {
            //debug
            m_type = "Client";

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

    /// <summary>
    /// description of protocol connection
    /// 
    /// </summary>
    public class TransferConnectionProtocol
    {
        private TransferConnectionProtocol() { }

        public static int bufferSize = 512;
        public static int receiveTimeout = 6000;
        private static int m_maxMsgLength = 200;
        public static int sendTimeout = 6000;

        public static int CountPackages(string _message)
        {
            int len = m_maxMsgLength;
            int count = (_message.Count() + len - 1) / len;
            if (count == 0) // if msg is empty
                count = 1;
            return count;
        }

        public static string GetNextPackage(ref string _message, int _numberInPackage, int _packagesCount)
        {
            int len = m_maxMsgLength;
            string rawPackage;
            if (_message.Count() < len)
            {
                rawPackage = _message;
                _message = "";
            }
            else
            {
                rawPackage = _message.Substring(0, len);
                _message = _message.Remove(0, len);
            }
            return WrapPackage(rawPackage, _numberInPackage, _packagesCount);
        }

        private static string WrapPackage(string _message, int _numberInPackage, int _packagesCount)
        {
            /// SERVER MESSAGE STRUCT =  LENGTH_OF_CURRENT_MESSAGE + SPACE + 
            ///     CURRENT_NUMBER_OF_PACKAGE + SPACE + NUMBER_OF_PACKAGES + SPACE + MSG 
            ///
            /// IMPORTANT: PACKAGE CUR NUMBER START VALUE = 1
            int num = _message.Count();
            return (
                num.ToString()
                + " "
                + _numberInPackage.ToString()
                + " "
                + _packagesCount.ToString()
                + " "
                + _message);
        }

        public static bool ExtractMessage(ref string _rawData, ref string _result, ref bool _isPenultimate, ref bool _isLastPackage)
        {
           
            int packageCount = 0;
            int packageNumber = 0;
            int msgLength = 0;
            string message = "";

            Regex regex = new Regex(@"^(\d+) (\d+) (\d+) (.*)", RegexOptions.Singleline);
            Match match = regex.Match(_rawData);
            bool success;
            if (success = match.Success)
            {
                msgLength = Convert.ToInt32(match.Groups[1].Value);
                packageNumber = Convert.ToInt32(match.Groups[2].Value);
                packageCount = Convert.ToInt32(match.Groups[3].Value);
                message = match.Groups[4].Value;
                Debug.WriteLine("msg length  = " + msgLength.ToString());
                Debug.WriteLine("pack number = " + packageNumber.ToString());
                Debug.WriteLine("packCount = " + packageCount.ToString());
            }

            if (message.Count() < msgLength) { return false; }
            if (packageNumber == packageCount) { _isLastPackage = true; } else { _isLastPackage = false; }
            if (packageCount - packageNumber == 1) { _isPenultimate = true; } else { _isPenultimate = false; }
            _rawData = message.Substring(Convert.ToInt32(msgLength));
            _result = message.Substring(0, Convert.ToInt32(msgLength));
            return success;
        }


    }
}
