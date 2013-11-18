using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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
                        req = Console.ReadLine();
                        Match m = Regex.Match(req, @"^ls (.*)");
                        if (m.Success)
                        {
                            if (cl.Ls(m.Groups[1].Value, ref files, ref err))
                            {
                                foreach (FileObject f in files)
                                {
                                    Console.WriteLine(f.GetString());
                                }
                            }
                            else
                            {
                                Console.WriteLine("Error " + err.ToString());
                            }
                        }
                        m = Regex.Match(req, "^cat (.*)");
                        if (m.Success)
                        {
                            String f =null;
                            if (cl.Cat(m.Groups[1].Value, ref f, ref err))
                            {
                                FileStream fs = File.OpenWrite("__tmp.txt");
                                fs.Write(Encoding.Default.GetBytes(f),0,f.Length);
                                fs.Close();
                                Process.Start("notepad.exe", "__tmp.txt");

                            }
                            else
                            {
                                Console.WriteLine("Error " + err.ToString());
                            }
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
            nserver s = new nserver();
            nclient c = new nclient();
            Thread t = new Thread(s.start);
            t.Start();
            c.start();


            Console.ReadLine();
        }
    }
}
