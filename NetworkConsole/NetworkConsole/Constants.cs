using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkConsole
{
    public static class Constants
    {
        public static int serverTCPPort = 43562;
        public static int serverUDPPort = 43563;
        public static int clientUDPPort = 43561;

        public static string authRightPassword = "RightPass";
        public static string authWrongPassword = "WrongPass";
        public static string authWrongPasswordCloseConnection = "WrongPassClose";

        public static byte authMaxAttempts = 10;

        public static string ansCmdUnknownHeader = "ans unknown";
        public static string cmdLs = "ls ";
        public static string cmdCat = "cat ";
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

        public string Convert()
        {
            
        }

        public static FileObject Unconvert(string _rawData)
        {
 
        }

        // debug info
        public string GetString()
        {
            string res;
            if (!isFile)
            {
                res = "dir  ";
            }
            else
            {
                res = "file ";
            }
            int len = 15;
            if (name.Length >= len)
                res += name.Substring(0, len);
            else
                res += name.PadRight(len);
            res += " ";
            res += ((size / 1024).ToString() + "KB").PadRight(10);
            res += " ";
            res += date.ToString();
            return res;
        }


    }
}
