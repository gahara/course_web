using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;

namespace ExplolerViaNetworkConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            
            //debug
            Debug.Indent();

            ServerExplorer explorer = new ServerExplorer();
            explorer.SetPassword("StupidClient99");
            
            ClientNetworkExplorer client = new ClientNetworkExplorer();
            //client.SetPassword("StupidClient99");

            Thread threadS = new Thread(new ThreadStart(explorer.Start));
            threadS.Start();
            
            Thread.Sleep(1000);
            if (client.Connect("127.0.0.1", Ports.serverTcpPort))
            {
                if (client.Authorize("StupidClient99"))
                {
                    client.Ls("c:\\");
                }
                else
                    Console.WriteLine("bad pass");
            }
            else
            {
                Console.WriteLine("Bad connection");
            }
            client.Close();
            Console.ReadKey();
        }
    }
}
