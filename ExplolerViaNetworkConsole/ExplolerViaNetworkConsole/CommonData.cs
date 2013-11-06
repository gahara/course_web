using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public static class Ports
    {
        public static int serverTcpPort = 22701;
        public static int serverUdpPort = 22702;
    }

    public class FileObject
    {
        public FileObject(bool _isFile, string _name, long _size, DateTime _date)
        {
            isFile = _isFile;
            name = _name;
            size = _size;
            date = _date;
        }
        public readonly bool isFile;
        public readonly string name;
        public readonly long size;
        public readonly DateTime date;

        // debug info
        /*public string GetString()
        {
            string res;
            if (!isFile)            {
                res = "dir  ";
            }
            else {
                res = "file ";
            }
            int len = 15;
            if (name.Length >= len)
                res += name.Substring(0,len);
            else 
                res += name.PadRight(len);
            res += " ";
            res += ((size / 1024).ToString() + "KB").PadRight(10);
            res += " ";
            res += date.ToString();
            return res;
        }*/


    }
}
