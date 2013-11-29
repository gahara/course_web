using System;
using System.Collections.Concurrent;
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

        public Socket GetSocket()
        {
            return m_socket;
        }

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
                string package = "";
                while (TransferConnectionProtocol.GetNextPackage(ref package, ref _message))
                {
                    Debug.WriteLine(m_type + ": " + " began send " + " package");

                    m_socket.Send(Encoding.Unicode.GetBytes(package));
                }
                m_socket.Send(Encoding.Unicode.GetBytes(package));
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
                string rawData = "";
                bool isError = false;
                while (!isLastPackage)
                {
                    Debug.WriteLine(m_type + ": " + "began receive");
                    //todo: check if we have odd count of bytes
                    int count = m_socket.Receive(m_buffer);
                    if (count == 0) throw new Exception("Connection closed");
                    rawData += Encoding.Unicode.GetString(m_buffer).Substring(0, count / 2); // 2 because UTF-16
                    string tmpMsg = "";
                    //Debug.WriteLine("raw data before extract: " + rawData);
                    while (TransferConnectionProtocol.ExtractMessage(ref rawData, ref tmpMsg, ref isLastPackage, ref isError)) {
                        _message += tmpMsg;
                        if (isLastPackage) break;
                        Debug.WriteLine(m_type + " receive :" + tmpMsg);
                        Debug.WriteLine(m_type + " receive :lp " + isLastPackage.ToString());
                        Debug.WriteLine(m_type + " receive :er " + isError.ToString());


                    }
                    if (isError) throw new Exception("Wrong package");
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
   
    /// <summary>
    /// description of protocol connection
    /// 
    /// </summary>
    public class TransferConnectionProtocol
    {
        private TransferConnectionProtocol() { }

        public static int bufferSize = 4096;
        public static int receiveTimeout = 120000;
        private static int m_maxMsgLength = 1500;
        public static int sendTimeout = 6000;

        public static bool GetNextPackage(ref string _package, ref string _message)
        {
            int len = m_maxMsgLength;
            string rawPackage;
            bool isLast = false;
            if (_message.Count() <= len)
            {
                rawPackage = _message;
                isLast = true;
                _message = "";
            }
            else
            {
                rawPackage = _message.Substring(0, len);
                _message = _message.Remove(0, len);
            }
            _package = WrapPackage(rawPackage, isLast);
            return !isLast;
        }

        private static string WrapPackage(string _message, bool _isLastPackage)
        {
            /// SERVER MESSAGE STRUCT =  LENGTH_OF_CURRENT_MESSAGE + SPACE + 
            ///     CURRENT_NUMBER_OF_PACKAGE + SPACE + NUMBER_OF_PACKAGES + SPACE + MSG 
            ///
            /// IMPORTANT: PACKAGE CUR NUMBER START VALUE = 1
            int num = _message.Count();
            char c = (_isLastPackage == true) ? '1' : '0';
            return (
                num.ToString()
                + " "
                + c
                + " "
                + _message);
        }

        public static bool ExtractMessage(ref string _rawData, ref string _result, ref bool _isLastPackage, ref bool _isError)
        {
            char serviceInfo = '0';
            int msgLength = 0;

            string message = "";
            bool success;
            Regex regex = new Regex(@"^(\d+) (\d) (.*)", RegexOptions.Singleline);
            Match match = regex.Match(_rawData);
            bool result = true;
            if (success = match.Success)
            {
                msgLength = Convert.ToInt32(match.Groups[1].Value);
                serviceInfo = Convert.ToChar(match.Groups[2].Value);
                message = match.Groups[3].Value;
                //Debug.WriteLine("msg length  = " + msgLength.ToString());
                //Debug.WriteLine("message info = " + serviceInfo + "  " + (serviceInfo == '0' ? "common" : "last"));
            }

            // if msg must be parsed and its cannot be
            // then message wrong
            if (_rawData.Count() > (m_maxMsgLength + 20) && !success) ///+20 - this is very bad
            {
                _isError = true;
            } else 
                _isError = false;

            // if data is not full
            if (message.Count() < msgLength || !success) { result =  false;}
            else
                if (serviceInfo == '1') { _isLastPackage = true; } else { _isLastPackage = false; }

            
            //if last packag
            ///Debug.WriteLine("Extract: msg data: " + message);
            //Debug.WriteLine("Extract: msg data count = " + message.Count().ToString());
            //Debug.WriteLine("Extract:length: " + msgLength.ToString());
            //Debug.WriteLine("");
            if (result)
            {
                _rawData = message.Substring(msgLength);
                _result = message.Substring(0, msgLength);
            }
            return result;
        }
    }

    
}
