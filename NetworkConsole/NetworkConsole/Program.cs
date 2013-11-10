using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkConsole
{

    /// <summary>
    /// here is the testing enviroment =)
    /// you must learn the power of my classes, young padawan
    /// SO, fucking phrase, i want to watch star wars now =(
    /// </summary>
    public class nclient
    {
        public nclient()
        { }

        public void start()
        {
            ClientTransferConnection ctc = new ClientTransferConnection();
            ctc.Connect("127.0.0.1", 24567);
            string ss = "";
            for (int i = 0; i < 400; i++)
                ss += "Fuuu1uuu1";
            ctc.Send(ss);
            string msg = "";
            if (ctc.Receive(ref msg))
            {
                Console.WriteLine("client: " + msg);
            }
        }
    }

    public class nserver
    {
        public nserver()
        {
            
        }

        public void start()
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(new IPEndPoint(IPAddress.Any, 24567));
            s.Listen(10);
            Socket s1 = s.Accept();
            ServerTransferConnection stc = new ServerTransferConnection(s1);
            string msg = "";
            if (stc.Receive(ref msg)){
                Console.WriteLine("Server: " +msg);
                Console.WriteLine(msg.Count().ToString());
                stc.Send("SHOPOLOLOLOLO");
            }
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            nserver s = new nserver();
            nclient c = new nclient();
            Thread t = new Thread(s.start);
            t.Start();
            Thread.Sleep(1000);
            c.start();
            Console.ReadLine();
        }
    }
}
