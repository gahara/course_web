using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

namespace ExplolerViaNetworkConsole
{

    public struct FileObject
    {
        FileObject(string _name, string _date, bool _isFile)
        {
            string pattern = "(/d+)";
            Regex rgx = new Regex(pattern);
            MatchCollection mc = rgx.Matches(_date);
            /*if (mc.Count < 6)
                throw*/
            date = new DateTime(Convert.ToInt32(mc[0].Value), 
                Convert.ToInt32(mc[1].Value), 
                Convert.ToInt32(mc[2].Value), 
                Convert.ToInt32(mc[3].Value), 
                Convert.ToInt32(mc[4].Value),
                Convert.ToInt32(mc[5].Value));
            name = _name;
            isFile = _isFile;
        }

        public readonly bool isFile;
        public readonly string name;
        public readonly DateTime date;

    }


    /* абстрактная версия протокола
     * выделил вроде функции типа:
     * получить список файлов в директории(ну и переход в эту директорию)
     * слить файл
     * установка соединения
     * разрыв
     * ошибко
     * 
     */ 
    public abstract class AbsProtocol
    {
        FileObject[] BrowseDirectory(string directory, bool fullpath);
        byte[] GetFile(string directory, bool fullpath);
        bool TestServer(string )
    }

    interface IExplorer
    {
        bool cd();
        bool cat();
        void refresh();
    }

    public class Explorer
    {
        protected DataNode m_node;
        public bool cd ()
        {
            return false;
        }
    }

    public class DataNode
    {
        protected List<DataNode> m_children = null;
        protected DataNode m_parent = null;
        protected bool m_isFile;
        protected string m_Name;
        protected DateTime m_Date;
        
        public bool ParseChildrenFromData(byte[] data)
        {
            return false;
        }

        public bool isChldrnExist {get {return (m_children != null); }}
        public bool isRoot {get { return (m_parent == null); }}
        public bool isFile {get { return m_isFile; }}
        
    }

    public class ExplorerClient
    {
        private Socket socket;//= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public void Start()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22201);
            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.ReceiveTimeout = 1000;
            socket.Connect(endPoint);
            byte[] msg = new byte[100]; 
            msg = Encoding.ASCII.GetBytes("Hi_from_client");
            socket.Send(msg);
            socket.Receive(msg);
            Console.WriteLine("Client: " + Encoding.ASCII.GetString(msg));
            socket.Close();
            /*while (true) 
            {
                Socket s = socket.Accept();
                byte[] sizebuffer = new byte[4];
                int m = 1;
                int num = 0;
                for (int i = 3; i >= 0; i++) {
                    num += m*sizebuffer[i];
                    m *= 256;
                }
                byte[] buffer = new byte[num];
                int received = s.Receive(buffer);
                s.Close();
            }*/

        }
    }

    public class ExplorerServer
    {
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public void Start()
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, 22201));
            socket.Listen(0);

            while (true)
            {
                Socket s = socket.Accept();
                s.ReceiveTimeout = 1000;
                byte[] msg = new byte[100];
                s.Receive(msg);
                string str = Encoding.ASCII.GetString(msg);
                Console.WriteLine("Server: " + str);
                if (!str.Contains("Hi_from_client")) 
                {
                    s.Close();
                    continue;
                }
                msg = Encoding.ASCII.GetBytes("Hi_from_server");
                //Console.WriteLine(Convert.ToString(msg));
                
                //msg = Encoding.ASCII.GetBytes("I_am_server");
                //Console.WriteLine(msg);
                s.Send(msg);
                /*bool flag = true;
                msg = new byte[2048];
                byte[] req = new byte[300];
                while (flag)
                {
                    s.Receive(req);
                    if (Match(req,"ls.*"))
                }*/
                s.Close();
            }

        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            ExplorerServer server = new ExplorerServer();
            ExplorerClient client = new ExplorerClient();
            Thread threadS = new Thread(new ThreadStart(server.Start));
            Thread threadC = new Thread(new ThreadStart(client.Start));
            threadS.Start();
            threadC.Start();
        }
    }
}
