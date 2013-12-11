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

namespace Client
{
    using NetworkConsole;
    using System.Diagnostics;
    using System.IO;
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientNetworkExplorer m_networkExplorer;
        private Stack<string> m_path;

        public class DisplayFile { // отображение файлов в таблице
			// binding'и в xamle настроены напрямую к проперти объектов данного класса
            private string m_fileType = "";
            private string m_name = "";
            private string m_size = "";
            private string m_date = "";

            public string FileType { get { return m_fileType; } }
            public string Name { get { return m_name; } }
            public string Size { get { return m_size; } }
            public string CreationDate { get { return m_date; } }

            private DisplayFile(bool _isFile, string _name, long _size, DateTime _date, bool _isRoot) 
            {
                m_name = _name;
                if (_isRoot)
                {
                    m_fileType = "vol";
                    m_size = ""; m_date = "";
                }
                else {
                    m_fileType = _isFile ? "file" : "dir";
                    if (_isFile) {
                        string opt;
                        if (_size < 10000)
                            opt = " Б";
                        else if (_size < 10000000)
                        {
                            _size /= 1024;
                            opt = " КБ";
                        }
                        else if (_size < 10000000000)
                        {
                            _size /= 1024 * 1024;
                            opt = " МБ";
                        }
                        else
                        {
                            _size /= 1024 * 1024 * 1024;
                            opt = " ГБ";
                        }
                        m_size = _size.ToString() + opt;
                    }
                    m_date = _date.ToString();
                }
            }

            private DisplayFile()
            {
                //parent direcetory
                m_fileType = "dir";
                m_name = "..";
            }

			// перевод FileObject в DisplayFile
            public static DisplayFile[] Parse(List<FileObject> _fileObjects, bool _isRoot)
            {
                DisplayFile[] result;
                if (_isRoot)
                {
                    result = _fileObjects.Select(x => new DisplayFile(x.FileType, x.Name, x.Size, x.CreationDate, _isRoot)).ToArray();
                }
                else
                {
                    List<DisplayFile> files = new List<DisplayFile>();
                    files.Add(new DisplayFile());
                    files.AddRange(_fileObjects.Select(x => new DisplayFile(x.FileType, x.Name, x.Size, x.CreationDate, _isRoot)));
                    result = files.ToArray();
                }
                return result;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            m_networkExplorer = new ClientNetworkExplorer();
        }

		// когда нажимаем на подключиться
        private void itemConnect_Click(object sender, RoutedEventArgs e)
        {
			// если уже к кому-то подключены, то отключаемся
            if (m_networkExplorer.IsConnected)
                this.CloseConnection();
			
			// открываем окошко соединений
            ConnectionWindow c = new ConnectionWindow(m_networkExplorer);
            c.ShowDialog();
			// если мы соединились в том окне с сервером, то...
            if (m_networkExplorer.IsConnected)
            {
                List<FileObject> files = null;
                int err = 0;
				// получаем список файлов корневой директории
                if (m_networkExplorer.Ls("\\", ref files, ref err))
                {
                    m_path = new Stack<string>();
                    m_path.Push("\\");
                    bool isRoot = true;
                    tblFiles.ItemsSource = DisplayFile.Parse(files, isRoot);
                }
            }
        }

		// навигация по директориям
        private DisplayFile[] ChangeDirectory(DisplayFile _d, ref int err)
        {
            DisplayFile[] result = null;
            if (_d.Name == "..") // если поднимаемся на уровень выше
            {
                string curDir = m_path.Pop();
                string parDir = m_path.Peek();
                List<FileObject> rawFiles = null;
                if (m_networkExplorer.Ls(parDir, ref rawFiles, ref err))
                {
                    bool isRoot = m_path.Count == 1 ? true : false;
                    result = DisplayFile.Parse(rawFiles, isRoot);
                }
                else
                {
                    m_path.Push(curDir);
                }
            }
            else
            {  // если опускаемся на уровень ниже
                string curDir = m_path.Peek();
                string nextDir = "";
                if (m_path.Count != 1) { nextDir = curDir +  @"\"; }
                nextDir += _d.Name;
                List<FileObject> rawFiles = null;
                if (m_networkExplorer.Ls(nextDir, ref rawFiles, ref err))
                {
                    result = DisplayFile.Parse(rawFiles, false);
                    m_path.Push(nextDir);
                }
            }
            return result;
        }

		// открываем поток для записи скачанного файла
        private FileStream OpenMyFile(out string filename)
        {
            bool flag = true;
            filename = "__tmp.txt";
            FileStream result = null;
            while(flag){
                try
                {
                    result = File.Open(filename, FileMode.Create);
                    flag = false;
                }
                catch {
                    filename = "_" + filename;
                }
            }
            return result;
            
        }

		// double click по таблице с файлами и папками
        private void tblFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;
            DisplayFile d = (DisplayFile)grid.SelectedItem; // получаем элемент, по которому щелкнули
            if (d != null)
            {
                int err = 0;
                if (d.FileType == "vol" || d.FileType == "dir") // если папка, то это навигация по директориям
                {
                    DisplayFile[] files = ChangeDirectory(d, ref err);
                    if (files != null) { grid.ItemsSource = files; }
                }
                else // иначе это просмотр файла
                {
                    if (d.Size == "0 Б")
                    {
                        MessageBox.Show("Файл пуст", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    byte[] file;
                    string strfile = "";
                    string pathtofile = m_path.Peek() + "\\" + d.Name;
                    string tmpfile;
					// получаем с помощью networkexplorer файл, записываем в txt, открываем в отдельном процессе блокнот для просмотра этого файла
                    if (m_networkExplorer.Cat(pathtofile, ref strfile, ref err))
                    {
                        FileStream fs = OpenMyFile(out tmpfile);
                        file = Encoding.Default.GetBytes(strfile);
                        fs.Write(file, 0, file.Count());
                        fs.Close();
                        Process.Start("notepad", tmpfile);
                    }
                }
                if (err == 100)
                    HandleConnectionError();
                else if (err != 0)
                    HandleOtherError(err);
            }
        }

        private void HandleConnectionError()
        {
            this.CloseConnection();
            MessageBox.Show("Ошибка соединения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void CloseConnection()
        {
            m_networkExplorer.Close();
            tblFiles.ItemsSource = null;
        }

        private void HandleOtherError(int err)
        {
            MessageBox.Show("Возникла ошибка, ну и хрен с ней, продолжаем работать " + err.ToString());
        }
		
		// когда в меню нажимаем на Отключиться
        private void itemDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (m_networkExplorer.IsConnected)
                this.CloseConnection();
        }
		
        private void itemHelp_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("notepad", "readme.txt");
        }
    }
}
