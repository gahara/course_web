using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ExplolerViaNetworkConsole
{
    public partial class ServerExplorer
    {

        /// <summary>
        /// this is the wrapper for some
        /// functions for parsing requests
        /// </summary>
        /// todo: make absclass ProtocolModule and inherit 2 realizations: server and client
        public class ServerProtocolModule : ProtocolModule
        {
            public static Command Authorize(ref bool _isSuccess, string _param, string _pass)
            {
                _isSuccess = (_pass == _param) ? true : false;
                return new AuthorizeCommand(_isSuccess);
            }

            public static Command ReturnNullCommand()
            {
                return new NullCommand();
            }
            
            private static bool isCommand(string _msg, string _cmdHeader, ref string _param)
            {
                // returns param of the command through the _param
                bool result = false;
                Regex regex = new Regex("^" + _cmdHeader, RegexOptions.Singleline);
                if (regex.Match(_msg).Success)
                {
                    result = true;
                    _param = _msg.Substring(_cmdHeader.Count());
                }
                return result;
            }

            public static Command ParseCommand(string _msg)
            {
                string param = "";
                if (isCommand(_msg, ProtocolConstants.reqCatHeader,ref param))
                {
                    return new CatCommand(param);
                }
                else if (isCommand(_msg, ProtocolConstants.reqLsHeader, ref param))
                {
                    return new LsCommand(param);
                }
                else
                {
                    return new UnknownCommand();
                }
            }
        }
    }
}
