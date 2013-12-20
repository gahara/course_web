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
    /// <summary>
    /// Interaction logic for StartAuthWindow.xaml
    /// </summary>
    public partial class StartAuthWindow : Window
    {
        public bool IsOk = false;
        public string Login;
        private Dictionary<string, String> m_logPass = new Dictionary<string,string>();
        public StartAuthWindow()
        {
            m_logPass.Add("Bob", "Marley");
            m_logPass.Add("Пользователь", "password");
            m_logPass.Add("User123", "qwerty");
            InitializeComponent();
        }

        private void btnEnter_Click(object sender, RoutedEventArgs e)
        {
            string login = boxLogin.Text;
            string pass = boxPassword.Password;
            if (m_logPass.Keys.Contains(login))
            {
                if (m_logPass[login] == pass)
                {
                    Login = login;
                    IsOk = true;
                }             
            }
            if (!IsOk) {MessageBox.Show("Неправильный логин или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);}
            this.Close();
        }
    }
}
