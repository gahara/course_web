using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Server
{
    using NetworkConsole;
    using System.Threading;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public static class Log
    {
        private static RichTextBox m_logBox;
        private delegate void AddStr(string _text);
        private static AddStr m_delegateAppendStr;
        private static object m_sync = new object();

        public static void Init(RichTextBox _logBox)
        {
            m_logBox = _logBox;
            m_delegateAppendStr = new AddStr(AppendString);
        }

        public static void Close()
        {
            lock (m_sync)
            {
                m_logBox = null;
            }
        }

        public static void Add(string _logString)
        {
            lock (m_sync)
            {
                if (m_logBox == null)
                    return;

                m_logBox.Dispatcher.Invoke(
                    m_delegateAppendStr,
                    new object[] { _logString });
            }
        }

        private static void AppendString(string _str)
        {
            m_logBox.AppendText(">" + _str + "\r\n");
        }
    }

    public partial class MainWindow : Window
    {
        private ExplorerServer m_server = null;
        private Thread m_serverThread = null;

        public MainWindow()
        {
            InitializeComponent();
            Log.Init(boxLog);
            m_server = new ExplorerServer();
        }

        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string _pass = m_server.Password;
            SetPasswordWindow passWindow = new SetPasswordWindow(_pass);
            passWindow.ShowDialog();
            if (passWindow.IsPasswordChanged) { m_server.Password = passWindow.Password; Log.Add("Password changed"); }
        }

        private void ___btnStartServer_Click(object sender, RoutedEventArgs e)
        {
            ___btnStartServer.IsEnabled = false;
            m_serverThread = new Thread(m_server.Start) { IsBackground = true };
            m_serverThread.Start();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Log.Close();
        }
    }
}
