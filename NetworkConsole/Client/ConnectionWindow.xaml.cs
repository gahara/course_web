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
using System.Windows.Shapes;

namespace Client
{
    using NetworkConsole;
    using System.Net;
    /// <summary>
    /// Interaction logic for ConnectionWindow.xaml
    /// </summary>
	// окно поиска серверов и коннекта
    public partial class ConnectionWindow : Window
    {
        ClientNetworkExplorer m_explorer = null; // explorer -через который мы делаем запросы к серверу
        public ConnectionWindow(ClientNetworkExplorer _explorer)
        {
            InitializeComponent();
            m_explorer = _explorer;
        }

        private void btnBroadcastSearch_Click(object sender, RoutedEventArgs e)
        {
            IPEndPoint[] servers = m_explorer.BroadcastSearch();
            //ServerAddr[] newserv = ServerAddr.ParseIPEndPoint(servers);
            tblServers.ItemsSource = servers;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            IPAddress ip;
            if (!IPAddress.TryParse(boxIPAddress.Text, out ip))
            {
                MessageBox.Show("Неверный формат IP Адреса", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                boxIPAddress.Focus();
                return;
            }

            int port;
            if (!int.TryParse(boxPort.Text, out port))
            {
                MessageBox.Show("Неверный формат порта", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                boxPort.Focus();
                return;
            }

            if (!m_explorer.Connect(boxIPAddress.Text, port))
            {
                MessageBox.Show("Невозможно подключиться", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

			// окно ввода пароля
            PasswordWindow passWindow = new PasswordWindow(m_explorer);
            passWindow.ShowDialog();
            if (m_explorer.IsConnected)
            {
                this.Close();
            }

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

		// обработка нажатия левой клавиши мыши по таблице(перекидываем инфу о серверах в textbox'ы)
        private void tblServers_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            IPEndPoint endPoint = (IPEndPoint)grid.SelectedItem;
            if (endPoint != null)
            {
                boxIPAddress.Text = endPoint.Address.ToString();
                boxPort.Text = endPoint.Port.ToString();
            }
        }
    }
}
