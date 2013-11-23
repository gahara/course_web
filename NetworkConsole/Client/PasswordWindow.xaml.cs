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
    /// <summary>
    /// Interaction logic for PasswordWindow.xaml
    /// </summary>
    public partial class PasswordWindow : Window
    {
        ClientNetworkExplorer m_explorer;
        bool m_closeConnection = true;

        public PasswordWindow(ClientNetworkExplorer _explorer)
        {
            InitializeComponent();
            m_explorer = _explorer;
        }

        private void btnAuthorize_Click(object sender, RoutedEventArgs e)
        {
            int err = 0;
            if (m_explorer.Authorize(boxPassword.Password, ref err))
            {
                MessageBox.Show("Авторизация успешна", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                m_closeConnection = false;
                Close();
            }
            else
            {
                if (err == Constants.codeErrBadAuthorization)
                {
                    MessageBox.Show("Неверный пароль", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (err == Constants.codeErrBadConnection)
                {
                    MessageBox.Show("Ошибка соединения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
                else if (err == Constants.codeErrVeryBadAuthorization)
                {
                    MessageBox.Show("Неверный пароль. Превышен лимит попыток авторизации", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
                else
                {
                    throw new Exception("Авторизация. Неизвестный код ошибки");
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (m_closeConnection)
            {
                m_explorer.Close();
            }
        }
    }
}
