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
            Thread threadS = new Thread(new ThreadStart(explorer.Start));
            threadS.Start();
            ClientNetworkExplorer client = new ClientNetworkExplorer();
            Thread.Sleep(1000);
            Console.WriteLine("I'm here");
            if (client.Connect(IPAddress.Parse("127.0.0.1"), client.m_password))
            {
                Console.WriteLine("Good connection");
            }
            else
            {
                Console.WriteLine("Bad connection");
            }
            Console.ReadKey();
            
            /*while (true) {
                Console.Write("Введите путь до директории: ");
                str = Console.ReadLine();
                if (str == "exit")
                {
                    break;
                }
               List<FileObject> files = explorer.Ls(str);
                if (files != null)
                {
                    Console.WriteLine("\r\nСписок файлов:");
                    foreach (FileObject f in files) {
                        Console.WriteLine(f.GetString());
                    }
                }
                Console.WriteLine("");
            }*/
        }
    }
}
