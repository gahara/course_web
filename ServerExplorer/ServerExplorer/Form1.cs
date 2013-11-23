using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ServerExplorer
{
    using NetworkConsole;
    using System.Threading;
    public static class LogBox
    {
        private static RichTextBox m_logBox;
        public static void SetLogBox(RichTextBox _box)
        {
            m_logBox = _box;
        }

        public static void AddString(string _str)
        {
            m_logBox.AppendText(_str + "\r\n");
        }
    }

    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();

            logTextBox.WordWrap = true;
            logTextBox.Multiline = true;
            logTextBox.Enabled = false;
            LogBox.SetLogBox(logTextBox);
        }

        ExplorerServer m_server;

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            m_server = new ExplorerServer();
            Thread t = new Thread(m_server.Start);
            t.Start();
        }
    }
}
