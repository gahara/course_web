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

namespace Server
{
    /// <summary>
    /// Interaction logic for SetPasswordWindow.xaml
    /// </summary>
    public partial class SetPasswordWindow : Window
    {
        private string m_password;
        private bool m_isPassowordChanged = false;

        public string Password { get { return m_password; } }
        public bool IsPasswordChanged { get { return m_isPassowordChanged; } }

        public SetPasswordWindow(string _pass)
        {
            InitializeComponent();
            m_password = _pass;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (boxOldPassword.Password != m_password)
            {
                boxOldPassword.Clear();
                boxNewPassword.Clear();
                boxConfirmPassword.Clear();
                MessageBox.Show("Неверный текущий пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (boxNewPassword.Password != boxConfirmPassword.Password)
            {
                boxNewPassword.Clear();
                boxConfirmPassword.Clear();
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (boxNewPassword.Password.Length < 8)
            {
                boxNewPassword.Clear();
                boxConfirmPassword.Clear();
                MessageBox.Show("Пароль должен содержать не менее 8 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return; 
            }

            m_password = boxNewPassword.Password;
            m_isPassowordChanged = true;
            MessageBox.Show("Пароль успешно изменен", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}
