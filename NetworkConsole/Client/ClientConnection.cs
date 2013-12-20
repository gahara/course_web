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
    // клиентские добавки в протокол передачи данных(реализована функция connect и initsocket)
	public class ClientTransferConnection : AbsTransferConnection
    {
        public ClientTransferConnection()
        {
            //debug
            m_type = "Client"; //debug info
        }

		// инициализируем клиентский сокет, задаем таймаут и размер буфера
        private void InitSocket()
        {
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_socket.ReceiveBufferSize = TransferConnectionProtocol.bufferSize;
            m_socket.ReceiveTimeout = TransferConnectionProtocol.receiveTimeout;
            m_socket.SendTimeout = TransferConnectionProtocol.sendTimeout;
        }

	    // пытаемся приконектиться к серверу, если ок, возвращаем true
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

	// класс протокола для броадкаста
    public class ClientBroadcastProtocol
    {
        public string m_type; // debug info
		
		// переменная синхронизации 
        private Object m_syncvar = new Object();
        // результирующая коллекция серверов
		private static HashSet<IPEndPoint> m_serverAddrs;
		//
        private UdpClient m_connection;
        // информация, которую мы рассылаем через броадкаст
		public byte[] m_info;
		// порт, на котором сидит данный броадкаст
        private int m_serverUDPPort;

		// конструктор: блок try catch используется для детектирования
		//		того, что сокет уже кем-то занят, если такая ситуация происходит, то увеличиваем номер порта на 1 и пытаемся снова 
        public ClientBroadcastProtocol(int _port, int _serverPort)
        {
            m_serverUDPPort = _serverPort;
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
            m_info = Encoding.ASCII.GetBytes(_port.ToString());
            m_connection.EnableBroadcast = true;
            m_serverAddrs = new HashSet<IPEndPoint>();
            Monitor.Enter(m_syncvar);
            this.BeginReceive();
        }

		// посылаем в броадкаст информацию(номер порта с которого мы послали)
		// т.к. 255.255.255.255 не работает, то берем адрес каждого интерфейса и посылаем 
		// броадкаст сообщение в подсеть каждого интерфейса(если адрес интерфейса 10.23.12.3 , то броадкаст адрес его подсети 10.23.12.255) 
        private void SendAddrInfo()
        {
            foreach (IPAddress ip in (Dns.GetHostEntry(Dns.GetHostName()).AddressList))
            {
                if (ip.AddressFamily.ToString() == "InterNetwork")
                {
                    string strIp = ip.ToString();
                    Match m = Regex.Match(strIp, @"(.*\.)\d+");
                    strIp = m.Groups[1].Value + "255";
                    m_connection.Send(m_info, m_info.Length, new IPEndPoint(IPAddress.Parse(strIp), m_serverUDPPort));
                }
            }
        }

		// блокировка монитора и возврат массива серверов(то, что наброадкастили) 
        public IPEndPoint[] Stop()
        {
            Monitor.Enter(m_syncvar);
			//todo: clear collection after getting array
            Debug.WriteLine("Entered to stop client");
            IPEndPoint[] result = m_serverAddrs.ToArray();
            m_serverAddrs.Clear();
            return result;
        }

		// запуск броадкаста
		// освобождение монитора, чтобы в результирующую коллекцию записывалась инфа о серверах
        public void Start()
        {
            Monitor.Exit(m_syncvar);
            Debug.WriteLine("Entered to start client");
            this.SendAddrInfo();
        }

		// асинхронная операция начала приема
        private void BeginReceive()
        {
            m_connection.BeginReceive(this.Receive, new object());
        }

		// callback BeginReceive'а, когда на сокет поступила информация и мы должны ее принять и обработать
		// в конце вызываем снова BeginReceive, т.е. этот сокет крутиться вечно в отдельном потоке
        private void Receive(IAsyncResult _ar)
        {
            IPEndPoint ip = null;
            byte[] buf = m_connection.EndReceive(_ar, ref ip);
            string msg = Encoding.ASCII.GetString(buf);
            Debug.WriteLine("recvd:" + Encoding.ASCII.GetString(buf));
            // в монитор может войти только тогда, когда мы вызвали функцию Start и не вызвали функцию Stop
			// в другие моменты времени этот блок игнорируется
			if (Monitor.TryEnter(m_syncvar))
            {
                Debug.WriteLine("Entered to locked client receive block");
				// если регэксп "server port" + server_port успешно распарсен, то добавляем в коллекцию 
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
