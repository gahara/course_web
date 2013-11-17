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

        public void start()
        {
            ClientNetworkExplorer cl = new ClientNetworkExplorer();
            if (cl.Connect("127.0.0.1"))
            {
                Console.WriteLine("Connected");
                //string addr = cl.BroadcastSearch()[0];
                int err = 0;
                if (!cl.Authorize("12345678", ref err))
                {
                    Console.WriteLine(err.ToString());
                }
                else
                {
                    Console.WriteLine("authorized");
                    string req = "";
                    while (req != "exit")
                    {
                        List<FileObject> files = null;
                        Console.Write("Directory to browse: ");
                        req = Console.ReadLine();
                        if (cl.Ls(req, ref files, ref err))
                        {
                            foreach (FileObject f in files)
                            {
                                Console.WriteLine(f.GetString());
                            }
                        }
                        else {
                            Console.WriteLine("Error " + err.ToString());
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Not Connected");
            }


        }
    }

    public class nserver
    {
        private ExplorerServer ex;
        public nserver()
        {
            ex = new ExplorerServer();
        }

        public void start()
        {
           ex.Start();
        }
    }

    class Program
    {

        static void Main(string[] args)
        {
            /*nserver s = new nserver();
            nclient c = new nclient();
            Thread t = new Thread(s.start);
            t.Start();
            //Thread.Sleep(1000);
            c.start();
            */
            /*ServerBroadcastProtocol s = new ServerBroadcastProtocol("1234", 12346);
            
            ClientBroadcastProtocol c = new ClientBroadcastProtocol(12345, 12346);
            //Thread t = new Thread(s.Start);
            s.Start();
            c.Start();
            Thread.Sleep(1000);
            IPAddress[] addr = c.Stop();
            for (int i = 0; i < addr.Count(); i++)
            {
                Console.WriteLine(addr[i].ToString());
            }
            c.Start();
            Thread.Sleep(1000);
            addr = c.Stop();
            for (int i = 0; i < addr.Count(); i++)
            {
                Console.WriteLine(addr[i].ToString());
            }
                //c.Receive();
            Console.ReadLine();*/
            nserver s = new nserver();
            nclient c = new nclient();
            Thread t = new Thread(s.start);
            t.Start();
            c.start();
            
            Console.ReadLine();
        }
    }
}
