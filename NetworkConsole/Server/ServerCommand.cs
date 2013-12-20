using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkConsole
{
    using Server;
    public partial class ClientConnection
    {
        public abstract class AbsCommand
        {
            private string m_client = ""; // id клиента
			// получение команды
            public static AbsCommand ParseCommand(string _cmd, ref Object _memory, int _clientNum)
            {
                AbsCommand cmd = null;
                if (_cmd.Substring(0, Constants.cmdLs.Length) == Constants.cmdLs) // если это Ls - запрос
                {
                    cmd = new LsCommand(_cmd.Substring(Constants.cmdLs.Length));
                }
                else if (_cmd.Substring(0, Constants.cmdCat.Length) == Constants.cmdCat) // если Cat запрос
                {
                    int len = Constants.cmdCat.Length;
                    bool isFirst = true;
                    if (_cmd[len] == Constants.optCatFirst[0]) // если это запрос первой части файла
                        len += Constants.optCatFirst.Length;
                    else // иначе - запрос следующих частей файла
                    {
                        len += Constants.optCatNext.Length;
                        isFirst = false;
                    }
                    cmd = new CatCommand(ref _memory, _cmd.Substring(len), isFirst); // формирование команды
                }
                else // иначе - неизвестная команда
                {
                    cmd = new UnknownCommand();
                }

                cmd.SetClientNum(_clientNum); // выставляем id клиента(для использования в логах)
                
                return cmd;
            }

            protected void SetClientNum(int _client)
            {
                m_client = " " + _client.ToString();
            }

            protected void ToLog(string _msg)
            {
                Log.Add(_msg);
            }

            protected void LogServerRcvd(string _msg)
            {
                ToLog("Получено от клиента" + m_client + ':' + _msg);
            }

            protected void LogServerSend(string _msg)
            {
                ToLog("Отправлено клиенту" + m_client + ':' + _msg);
            }

            protected string m_parameter;
            public abstract string Run();
        }

        public class AuthCommand : AbsCommand 
		// команда авторизации(вся авторизация прошла в AuthorizationModule)
		// здесь формируется ответ клиенту
        {
            private string m_result;
            public AuthCommand(bool _isAuthorized, string _login, bool _isCloseConnection)
            {
                this.LogServerRcvd("Запрос на авторизацию");
                if (_isCloseConnection) { m_result = Constants.authWrongPasswordCloseConnection; this.LogServerSend("Неправильный пароль. Превышено кол-во попыток авторизации, закрытие соединения."); }
                else if (_isAuthorized) { m_result = Constants.authRightPassword; this.LogServerSend("Авторизация успешна. Клиент зашел в программу под логином: " + _login); }
                else { m_result = Constants.authWrongPassword; this.LogServerSend("Неправильный пароль. Клиент зашел в программу под логином: " + _login); }
            }

            public override string Run()
            {
                return m_result;
            }
        }
		
		// если получена неизвестная команда от клиента
        public class UnknownCommand : AbsCommand
        {
            public UnknownCommand() { }

            public override string Run()
            {
                this.LogServerRcvd("Неизвестный запрос");
                this.LogServerSend("Неизвестный запрос");
                return Constants.ansCmdUnknownHeader;
            }
        }

		// команда просмотра директорий
        public abstract class AbsLsCommand : AbsCommand
        {
            protected abstract List<FileObject> Ls(string _fullpath); // получение списка файлов

            protected abstract List<FileObject> GetDrives(); // получение списка томов(корневая директория)

            public override string Run()
            {
                bool isGood = true; // не возникли ли ошибки при выполнении команды
                string header;
                string package = "";
                List<FileObject> files = null;
 	            try
                {
                    if (m_parameter == @"\") // если параметр - корневая директория
                    {
                        files = GetDrives();
                        this.LogServerRcvd("Запрос на просмотр корневой директории");
                    }
                    else // если параметр - не корневая директория
                    {
                        files = Ls(m_parameter);
                        this.LogServerRcvd("Запрос на просмотр директории " + m_parameter);
                    }
                } catch (Exception ex) { isGood = false; }
                if (files != null) // если список файлов != null (если нету файлов в директории, то получим пустой список, но не null)
                {
                    header = Constants.ansLs; // заголовок
                    foreach (FileObject f in files)
                        package += f.ToString() + '\n'; // упаковка в сообщение файлов
                    this.LogServerSend("Отправлен список файлов директории");
                } else
                {
                    header = Constants.ansLsError; // заголовок ошибки
                    if (isGood) { header += Constants.errNoPath; this.LogServerSend("Неверный путь"); }
                    else { header += Constants.errUnknown; this.LogServerSend("Непредвиденная ошибка"); }
                }

                return header + package;
            }
        }

		// конкретная реализация LsCommand, таких функций как GetDrives и Ls
        public class LsCommand : AbsLsCommand
        {
			// Перевод FileInfo в FileObject
            private List<FileObject> ParseObjects(FileInfo[] _files)
            {
                List<FileObject> result = new List<FileObject>();
                for (int i = 0; i < _files.Count(); i++)
                {
                    result.Add(new FileObject(
                        true,
                        _files[i].Name,
                        _files[i].Length,
                        _files[i].CreationTime
                        ));
                }
                return result;
            }

			// перевод DirectroyInfo в FileObject
            private List<FileObject> ParseObjects(DirectoryInfo[] _dirs)
            {
				
                List<FileObject> result = new List<FileObject>();
                for (int i = 0; i < _dirs.Count(); i++)
                {
                    result.Add(new FileObject(
                        false,
                        _dirs[i].Name,
                        0,
                        _dirs[i].LastWriteTime
                        ));
                }
                return result;
            }

			// получение списка томов
            protected override List<FileObject> GetDrives()
            {
                List<FileObject> result = new List<FileObject>();
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                foreach (DriveInfo d in allDrives)
                {
                    result.Add(new FileObject(false, d.Name, 0, new DateTime(0)));
                }
                return result;
            }

			// получение списка файлов в директории
            protected override List<FileObject> Ls(string _fullpath)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(_fullpath);
                
                if (!dirInfo.Exists)
                {
                    return null;
                }
                DirectoryInfo[] dirs = dirInfo.GetDirectories();
                FileInfo[] files = dirInfo.GetFiles();

                List<FileObject> result = ParseObjects(files);
                result.AddRange(ParseObjects(dirs));

                return result;
            }

            public LsCommand( string _param)
            {
                m_parameter = _param;
            }
        }

		// команда передачи файла
        public class CatCommand : AbsCommand
        {
            public class MemoryModule {

                private bool m_isPathExists;
                private long m_fileLength;
                private long m_bytesRead; // сколько байтов файла прочитано 
                public string filename; //
                private FileStream m_fs; // поток чтения файла
                public byte[] m_buf; // буфер, в который читается файл

                public bool isPathExists { get { return m_isPathExists; } }

				// прочитан ли файл до конца
                public bool isEnd { get { return (m_bytesRead == m_fileLength && m_fileLength != 0); } } 

				// конструктор
                public MemoryModule(int _bufSize)
                {
                    m_buf = new byte[_bufSize];
                    m_fileLength = 0;
                    m_bytesRead = 0;
                }

				// попытаться открыть запрошенный файл
                public bool OpenFile(string _filename)
                {
                    bool result = false;
                    try
                    {
                        if (File.Exists(_filename)) // если файл существуетс
                        {
                            m_fileLength = (new FileInfo(_filename)).Length; // получить размер файла
                            m_fs = File.OpenRead(_filename); // создать поток для чтения
                            m_bytesRead = 0; // сброс прочитанного кол-ва байтов
                            filename = _filename;
                            result = true; // файл открыт для чтения
                        }
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    m_isPathExists = result;
                    return result;
                }

                public bool ReadPortion(ref int _count, int _maxSize) // чтение очередной порции файла
                {
                    //only even _maxSize, because after we shall convert to utf8 string
                    bool result = false;
                    try
                    {
                        _count = m_fs.Read(m_buf, 0, _maxSize);
                        m_bytesRead += _count;
                        result = true;
                        if (this.isEnd) { m_fs.Close(); }
                    }
                    catch (Exception ex) { Debug.WriteLine(ex.Message); }
                    return result;
                }
           }

            private MemoryModule m_memory;
            public CatCommand(ref Object _memory, string _param, bool _isFirst)
            {
                Debug.WriteLine("is first: " + _isFirst.ToString());

                if (_isFirst) // если запрошена первая порция файла, то инициализируем _memory и пытаемся открыть файл
                {
                    this.LogServerRcvd("Запрос на получение первой порции файла " + _param);
                    if (_memory == null) { _memory = new MemoryModule(Constants.filePackageSize); }
                    m_memory = (MemoryModule)_memory;
                    m_memory.OpenFile(_param);
                }
                else // если следующая порция
                {
                    m_memory = (MemoryModule)_memory;
                    this.LogServerRcvd("Запрос на получение следующей порции файла " + m_memory.filename);
                }

            }

			// получение ответного сообщения на запрос
            public override string Run()
            {
                string msg = "";
                if (m_memory.isPathExists)
                {
                    int count = 0;
                    if (m_memory.ReadPortion(ref count, Constants.filePackageSize)) // если успешно прочитан кусок файла
                    {
                        msg = Constants.ansCat; // формируем заголовок
                        Debug.WriteLine("cat read :" + count.ToString() + " bytes");
                        if (m_memory.isEnd)
                            { msg += Constants.ansCatLastEven; }
                        else { msg += Constants.ansCatNotLast; }
                        msg += Encoding.Default.GetString(m_memory.m_buf, 0, count); // тело сообщения
                        if (!m_memory.isEnd)
                            this.LogServerSend("Порция файла");
                        else
                            this.LogServerSend("Последняя порция файла");
                    }
                    else { msg = Constants.ansCatError + Constants.errUnknown; this.LogServerSend("Непредвиденная ошибка"); }
                }
                else { msg = Constants.ansCatError + Constants.errNoPath; this.LogServerSend("Неверный путь до файла"); }
                return msg;
            }
        }
    }
}