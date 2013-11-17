using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkConsole
{
    public partial class ClientNetworkExplorer
    {
        ClientTransferConnection m_connection;
        ClientBroadcastProtocol m_broadcast;

        public ClientNetworkExplorer()
        {
            m_connection = new ClientTransferConnection();
            m_broadcast = new ClientBroadcastProtocol(Constants.clientUDPPort, Constants.serverUDPPort);
        }

        public string[] BroadcastSearch()
        {
            m_broadcast.Start();
            Thread.Sleep(2000);
            IPAddress[] addr = m_broadcast.Stop();
            return (addr.Select(x => x.ToString())).ToArray();
        }

        public bool Connect(string _ip)
        {
            return m_connection.Connect(_ip, Constants.serverTCPPort);
        }

        public void Close()
        {
            m_connection.Close();
        }

        public bool Authorize(string _pass, ref int _err)
        {
            string ans = "";
            if (!this.SendAndRecv(this.GetAuthHeader() + _pass, ref ans))
            {
                _err = Constants.codeErrBadConnection;
                return false;
            }
            bool result = this.ParseAuthMessage(ans, ref _err);
            if (_err == Constants.codeErrVeryBadAuthorization) { this.Close(); }

            return result;
        }

        public bool Ls(string _filepath, ref List<FileObject> _files, ref int _err)
        {
            string ans = "";

            if (!this.SendAndRecv(this.GetLsHeader() + _filepath, ref ans))
            {
                _err = Constants.codeErrBadConnection;
                return false;
            }
            
            return this.ParseLsMessage(ref ans, ref _files, ref _err);

        }

        public bool Cat(string _filepath, ref String _file, ref int _err)
        {
            _file = new String("".ToCharArray());
            bool isEnd = false;
            bool isFirst = true;
            _err = 0;
            while (!isEnd)
            {
                string msg = this.GetCatHeader(isFirst);
                if (isFirst) {msg += _filepath;}
                string ans = "";
                if (!this.SendAndRecv(msg, ref ans))
                {
                    _err = Constants.codeErrBadConnection;
                    break;
                }
                isEnd = ParseCatMessage(ref ans, ref _err);
                if (_err == 0) { _file += ans; }
            }
            if (_err != 0) { return false; }
            else { return true; }
        }
    }

    public partial class ClientNetworkExplorer
    {
        private bool ParseLsMessage(ref string _msg, ref List<FileObject> _files, ref int _err)
        {
            bool result = false;
            _files = null;
            if (Regex.Match(_msg, "^" + Constants.ansLs, RegexOptions.Singleline).Success)
            {
                _msg = _msg.Substring(Constants.ansLs.Length);
                _files = FileObject.Parse(_msg);
                result = true;
            }
            else if (Regex.Match(_msg, "^" + Constants.ansLsError + Constants.errLsNoPath, RegexOptions.Singleline).Success)
                _err = Constants.codeErrLsBadPath;
            else
                _err = Constants.codeErrUnknown;
            return result;
        }

        private bool ParseAuthMessage(string _msg, ref int _err)
        {
            _err = 0;
            bool result = false;
            if (Regex.Match(_msg, Constants.authRightPassword).Success) { result = true;}
            else if (Regex.Match(_msg, Constants.authWrongPasswordCloseConnection).Success) { _err = Constants.codeErrVeryBadAuthorization; }
            else { _err = Constants.codeErrBadAuthorization; }
            
            return result;
        }

        private string GetCatHeader(bool _isFirstRequest)
        {
            if (_isFirstRequest)
                return Constants.cmdCat + Constants.optCatFirst;
            else
                return Constants.cmdCat + Constants.optCatNext;
        }

        private bool ParseCatMessage(string _msg, ref byte[] _file, ref int _err)
        {
            bool result = false;
            if (Regex.Match(_msg, @"^" + Constants.ansCat, RegexOptions.Singleline).Success)
            {
                _err = 0;
                int len = Constants.ansCat.Length;
                if (_msg[len] == Constants.ansCatNotLast[0]) { result = true; }
                else if (_msg[len] == Constants.ansCatLastUneven[0])
                {
                }
            }
            else if (Regex.Match(_msg, @"^" + Constants.ansCatError + Constants.errNoPath, RegexOptions.Singleline).Success)
            {
                _err = Constants.codeErrCatBadPath;
            }
            else
            {
                _err = Constants.codeErrUnknown;
            }
            return result;
        }

        private string GetLsHeader()
        {
            return Constants.cmdLs;
        }

        private string GetAuthHeader()
        {
            return "";
        }

        private bool SendAndRecv(string _fullmessage, ref string _ans)
        {
            if (!m_connection.Send(_fullmessage)) { return false; }
            if (!m_connection.Receive(ref _ans)) { return false; }
            return true;
        }

    }
}
