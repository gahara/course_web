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
        public static int serverUDPPort = 43566;
        public static int clientUDPPort = 43561;

        public static string authRightPassword = "RightPass";
        public static string authWrongPassword = "WrongPass";
        public static string authWrongPasswordCloseConnection = "CloseWrongPass";

        public static byte authMaxAttempts = 10;

        public static string ansCmdUnknownHeader = "ans unknown";
        public static string cmdLs = "ls ";
        public static string cmdCat = "cat ";

        public static string ansLsRight = "ans ls ";
        public static string ansLsError = "ans err ls ";

        public static string errLsNoPath = "path";
        public static string errLsUnknown = "unknown";

        public static int codeErrBadConnection = 100;
        public static int codeErrBadAuthorization = 101;
        public static int codeErrVeryBadAuthorization = 102;


        public static int codeErrLsBadPath = 200;
        public static int codeErrLsAnother = 201;
        //public static int codeErrLsWrongObjects = 202;


        
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
            return "";
        }

        public static void Unconvert(string _rawData)
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
