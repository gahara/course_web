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
	// реализация непосредственно основной протокольной части и функций 
    public partial class ClientNetworkExplorer
    {
        ClientTransferConnection m_connection; 
        ClientBroadcastProtocol m_broadcast;
        bool m_isConnected;
        
        public bool IsConnected { get { return m_isConnected; } }

        public ClientNetworkExplorer()
        {
            m_connection = new ClientTransferConnection();
            m_broadcast = new ClientBroadcastProtocol(Constants.clientUDPPort, Constants.serverUDPPort);
            m_isConnected = false;
        }

        public IPEndPoint[] BroadcastSearch()
        {
            m_broadcast.Start();
            Thread.Sleep(1000);
            return m_broadcast.Stop();
        }

        public bool Connect(string _ip, int _port)
        {
            m_isConnected =  m_connection.Connect(_ip, _port);
            return m_isConnected;
        }

        public void Close()
        {
            m_isConnected = false;
            m_connection.Close();
        }

		// авторизация, если ок, true
        public bool Authorize(string _pass, ref int _err)
        {
            string ans = "";
			// если возвращен false, то ошибка
            if (!this.SendAndRecv(this.GetAuthHeader() + _pass, ref ans))
            {
                _err = Constants.codeErrBadConnection;
                return false;
            }
			// был ли ответ от сервера успешно распарсен и если да, то какой ответ
			// успех только если сообщение правильно формата и возвращена правильная кодовая фраза
            bool result = this.ParseAuthMessage(ans, ref _err);
			// если номер ошибки = very... , то это значит, что мы уже N раз ввели неправильно пароль
			// и сервер сейчас разорвет соединение, закрываем совет
            if (_err == Constants.codeErrVeryBadAuthorization) { this.Close(); }

            return result;
        }

		// функция получения списка файлов директории
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

		// функция получения содержимого файла
        public bool Cat(string _filepath, ref String _file, ref int _err)
        {
            _file = new String("".ToCharArray());
            bool isNotEnd = true;
            bool isFirst = true;
            _err = 0;
            while (isNotEnd)
            {
				// формируем сообщение - заголовок(является ли наше сообщение первым)
                string msg = this.GetCatHeader(isFirst);
				// если сообщение первое, то вкладываем в сообщение имя файла, который мы хотим скачать
                if (isFirst) { msg += _filepath; isFirst = false; }
                string ans = "";
				
                if (!this.SendAndRecv(msg, ref ans))
                {
                    _err = Constants.codeErrBadConnection;
                    break;
                }
				// является порция последней
                isNotEnd = ParseCatMessage(ans,ref  _file, ref _err);
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
            else if (Regex.Match(_msg, "^" + Constants.ansLsError + Constants.errNoPath, RegexOptions.Singleline).Success)
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

        private bool ParseCatMessage(string _msg, ref String _file, ref int _err)
        {
            bool result = false;
            if (Regex.Match(_msg, @"^" + Constants.ansCat, RegexOptions.Singleline).Success)
            {
                _err = 0;
                int len = Constants.ansCat.Length;
                int isUneven = 0;
                if (_msg[len] == Constants.ansCatNotLast[0]) { result = true; len += Constants.ansCatNotLast.Length;}
                else if (_msg[len] == Constants.ansCatLastUneven[0])
                {
                    len += Constants.ansCatLastUneven.Length;
                    isUneven = 1;
                }
                else { len += Constants.ansCatLastEven.Length; }
                if (isUneven == 0) {
                    _file += _msg.Substring(len); 
                } else {
                    byte[] bytes = Encoding.Default.GetBytes(_msg.Substring(len));
                    _file += Encoding.Default.GetString(bytes, 0, bytes.Length - isUneven);
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

		// отправка и прием сообщения
        private bool SendAndRecv(string _fullmessage, ref string _ans)
        {
            if (!m_connection.Send(_fullmessage)) { return false; }
            if (!m_connection.Receive(ref _ans)) { return false; }
            return true;
        }

    }
}
