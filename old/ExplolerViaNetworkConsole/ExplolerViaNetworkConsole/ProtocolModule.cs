using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public class ProtocolConstants
    {
        public static readonly string authorizeAccept = "You_are_chosen_one";
        public static readonly string authorizeDecline = "Wrong_password";
        public static readonly string reqLsHeader = "ls ";
        public static readonly string ansLsErrHeader = "ans ls err ";
        public static readonly string ansLsHeader = "ans ls ";
        public static readonly string reqCatHeader = "cat ";
        public static readonly string ansCatErrHeader = "ans cat err ";
        public static readonly string ansCatHeader = "ans cat ";
        public static readonly int maxBufSize = 1200;
    }

    public class ProtocolModule 
    {
        private static int m_maxMsgLength = 500;

        private static string[] DevideMsg(string _msg)
        {
            /// DEVIDING MESSAGE INTO SUBMESSAGES WITH SOME LENGTH
            ///  
            ///

            int len = m_maxMsgLength;
            int count = (_msg.Count() + len - 1) / len;
            string[] res = new string[count];
            int startPos = 0;

            for (int i = 0; i < count - 1; i++)
            {
                res[i] = _msg.Substring(startPos, len);
                startPos += len;
            }
            len = _msg.Count() - startPos;
            res[count - 1] = _msg.Substring(startPos, len);

            return res;
        }

        private static string WrapPackage(string _msg, int _numberInPackage, int _packagesNum)
        {
            /// SERVER MESSAGE STRUCT =  LENGTH_OF_CURRENT_MESSAGE + SPACE + 
            ///     CURRENT_NUMBER_OF_PACKAGES + SPACE + NUMBER_OF_PACKAGES + SPACE + MSG 
            ///
            /// IMPORTANT: PACKAGE CUR NUMBER START VALUE = 1
            int num = _msg.Count();
            return (
                num.ToString()
                + " "
                + _numberInPackage.ToString()
                + " "
                + _packagesNum.ToString()
                + " "
                + _msg);
        }

        public static string[] WrapMsg(string _msg)
        {
            string[] msgChunks = DevideMsg(_msg);
            int count = msgChunks.Count();
            string[] packages = new string[count];

            for (int i = 0; i < count; i++)
            {
                packages[i] = WrapPackage(msgChunks[i], i + 1, count);
            }

            return packages;
        }

        public static string ExtractMsg(string _msg, ref bool _isLastPackage)
        {
            string packageCount = "";
            string packageNumber = "";
            string msgLength = "0";
            string msg = "";

            Regex regex = new Regex(@"^(\d+) (\d+) (\d+) (.*)", RegexOptions.Singleline);
            Match match = regex.Match(_msg);
            if (match.Success)
            {
                msgLength = match.Groups[1].Value;
                packageNumber = match.Groups[2].Value;
                packageCount = match.Groups[3].Value;
                msg = match.Groups[4].Value;
            }

            if (packageNumber == packageCount) { _isLastPackage = true; }
            Debug.WriteLine("msg length " + _msg.Count().ToString());
            return msg.Substring(0, Convert.ToInt32(msgLength));
        }
    }
}
