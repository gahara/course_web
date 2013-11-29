using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public static string optCatFirst = "f ";
        public static string optCatNext = "n";

        public static string ansLs = "ans ls ";
        public static string ansLsError = "ans err ls ";

        public static string ansCat = "ans cat ";
        public static string ansCatError = "ans err cat ";
        public static string ansCatNotLast = "0 ";
        public static string ansCatLastEven = "1 ";
        public static string ansCatLastUneven = "2 ";


        public static string errNoPath = "path";
        public static string errUnknown = "unknown";

        public static int codeErrBadConnection = 100;
        public static int codeErrBadAuthorization = 101;
        public static int codeErrVeryBadAuthorization = 102;

        public static int codeErrUnknown = 13;

        public static int codeErrLsBadPath = 200;

        public static int codeErrCatBadPath = 300;

        public static int filePackageSize = 800; //only even
        
        //public static int codeErrLsWrongObjects = 202;


        
    }

    public class FileObject
    {
        public FileObject(bool _isFile, string _name, long _size, DateTime _date)
        {
            m_isFile = _isFile;
            m_name = _name;
            m_size = _size;
            m_date = _date;
        }
        private bool m_isFile;
        private string m_name;
        private long m_size;
        private DateTime m_date;

        public bool FileType { get { return m_isFile; } }
        public string Name { get { return m_name; } }
        public long Size { get { return m_size;} }
        public DateTime CreationDate { get { return m_date; } }


        public override string ToString()
        {
            string result = "";
            if (m_isFile) { result += 'f'; }
            else { result += 'd'; }
            result += '\t';
            result += m_name + '\t';
            result += m_size.ToString() + '\t';
            result += m_date.Ticks.ToString();

            return result;
        }

        public static List<FileObject> Parse(string _msg)
        {
            List<FileObject> files = new List<FileObject>();
            string pattern = @"([fd]{1})\t(.+?)\t(\d+)\t(\d+)\n";
            foreach (Match m in Regex.Matches(_msg, pattern, RegexOptions.Singleline))
            {
                bool isFile = (m.Groups[1].Value == "f");
                string filename = m.Groups[2].Value;
                long filesize = Convert.ToInt64(m.Groups[3].Value);
                long ticks = Convert.ToInt64(m.Groups[4].Value);
                files.Add(new FileObject(
                        isFile,
                        filename,
                        filesize,
                        new DateTime(ticks)));
            }
            return files;
        }

        // debug info
        public string GetString()
        {
            string res;
            if (!m_isFile)
            {
                res = "dir  ";
            }
            else
            {
                res = "file ";
            }
            int len = 15;
            if (m_name.Length >= len)
                res += m_name.Substring(0, len);
            else
                res += m_name.PadRight(len);
            res += " ";
            res += ((m_size / 1024).ToString() + "KB").PadRight(10);
            res += " ";
            res += m_date.ToString();
            return res;
        }


    }
}
