using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HtmlAgilityPack;
using MySql.Data.MySqlClient;
using PageAnalizer;

namespace PageAnalizer
{
    /// <summary>
    /// Класс - прародитель всех классов.
    /// </summary>
    abstract class PageAnalizer
    {
        protected string page; // адрес страницы
        protected string connectionString; // строка, необходимая для подключени к базе данных
        /// <summary>
        /// Записывает в лог запись об ошибке в виде JSON
        /// </summary>
        /// <param name="errorObject"></param>
        protected void WriteLog(string page, int code = 0, Exception errorObject = null)
        {

            string path = Directory.GetCurrentDirectory();
            if (!Directory.Exists(path + "/logs"))
            {
                Directory.CreateDirectory(path + "/logs");
            }
            string coutnOfLogs = Directory.GetFiles(path + "/logs").Length.ToString();

            // имя файла с ошибкой содержит номер записи и дату ошибки, внутри содержится JSON с адресом страницы, типом ошибки и сообщением
            StreamWriter errorWriter = new StreamWriter(path + "\\logs\\" + coutnOfLogs + "_log_" + DateTime.Today.ToShortDateString().Substring(3) + ".txt");
            if (errorObject is System.Net.WebException || code == 1)
            {
                errorWriter.WriteLine("{\"error\":{\"code\":1,\"message\":\"Ошибка получения текста со страницы:" + (errorObject as System.Net.WebException).Status + "\"},\"page\":\"" + page + "\"}");
            }
            else if (code == 2)
            {
                errorWriter.WriteLine("{\"error\":{\"code\":2,\"message\":\"Ошибка формирования статистики: Текст не был получен со страницы\"},\"page\":\"" + page + "\"}");
            }
            else if (errorObject is System.UriFormatException || code == 3)
            {
                errorWriter.WriteLine("{\"error\":{\"code\":3,\"message\":\"Ошибка формата URI" + (errorObject.Message) + "\"},\"page\":\"" + page + "\"}");
            }
            else if (code == 4)
            {
                errorWriter.WriteLine("{\"error\":{\"code\":4,\"message\":\"Ошибка записи в БД:" + (errorObject.Message) +  "\"},\"page\":\"" + page + "\"}");
            }
            else if (code == 5)
            {
                errorWriter.WriteLine("{\"error\":{\"code\":4,\"message\":\"Ошибка чтения из БД:" + (errorObject.Message) + "\"},\"page\":\"" + page + "\"}");
            }
            errorWriter.Close();
        }
    }

    /// <summary>
    /// Класс содержит функции обработки текста html страницы, полученной по адресу
    /// </summary>
    class TextFounder: PageAnalizer
    {
        string pageText; // текст со страницы
        bool TextIsTaked = false; // готов ли объект к обработке
        public string ConnectionString  // строка подключения к БД
        {
            get { return connectionString; }
            set { connectionString = value; }
        }
        public string Page  // адрес страницы
        {
            get { return page; }
            set
            {
                if (value != page)
                {
                    page = value;
                    TextIsTaked = false;
                    pageText = null;
                }
            }
        }

        /// <summary>
        /// Обабатывает текст, формируя статистику по уникальным словам в тексте.
        /// </summary>
        public WordFrequencyList GetUniqueWordStatistics()
        {
            if (!TextIsTaked)
            {
                this.Initialize();
            }
            try
            {
                WordFrequencyList statistic = new WordFrequencyList(pageText, this);
                statistic.SortListsReverse();
                return statistic;
            }
            catch
            {
                WriteLog(Page, 2);
                return null;
            }
        }

        /// <summary>
        /// Извлекает со страницы весь текст, разрешая обрабатывать этот текст
        /// </summary>
        private void Initialize() 
        {
            // Для этого использованы классы библиотеки HtmlAgilityPack
            HtmlWeb web = new HtmlWeb();
            try 
            {
                HtmlAgilityPack.HtmlDocument document = web.Load(Page);
                pageText = document.DocumentNode.InnerText;
                TextIsTaked = true;
            }
            catch (System.Net.WebException error)
            {
                WriteLog(page, 1, error);
                TextIsTaked = false;
                return;
            }
            catch (System.UriFormatException error)
            {
                WriteLog(page, 3, error);
                TextIsTaked = false;
                return;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_page">Адрес страницы, которую необходимо обработать</param>
        /// <param name="con">Строка подключения для подключения к БД</param>
        public TextFounder(string _page = null, string _connnectionString = null)
        {
            Page = _page;
            connectionString = _connnectionString;
        }
    }


    /// <summary>
    /// Класс, хранящий статистику по уникальным словам страницы.
    /// </summary>
    class WordFrequencyList: PageAnalizer
    {
        /// <summary>
        /// Класс для хранения пар: слово и его количество повторений.
        /// </summary>
        class PairSI
        {
            public string first;
            public int second;

            public override string ToString()
            {
                return first +' '+ second.ToString();
            }

            public static bool operator < (PairSI first, PairSI second)
            {
                if (first.second < second.second)
                    return true;
                else if (first.second > second.second)
                    return false;
                else if (first.first.CompareTo(second.first) > 0)
                    return true;
                else
                    return false;
            }
            public static bool operator > (PairSI first, PairSI second)
            {
                return !(first < second);
            }
            public PairSI(string f, int s)
            {
                first = f;
                second = s;
            }
        }

        List<PairSI> collection = new List<PairSI>(); // список со словами и количеством их повторений
        int size; // размер списка
        public string Page  // адрес страницы
        {
            get { return page; }
        }

        public int Count
        {
            get { return size; }
        }
        public string this[int index]
        {
            get => collection[index].ToString();
        }

        /// <summary>
        /// Сортировка по числам по возрастанию.
        /// </summary>
        public void SortLists() 
        {
            if (collection.Count() == 0)
                return;
            quickSort(0, collection.Count() - 1, false);
        }
        /// <summary>
        /// Сортировка по числам по убыванию.
        /// </summary>
        public void SortListsReverse()
        {
            if (collection.Count() == 0)
                return;
            quickSort(0, collection.Count() - 1, true);
        }
        /// <summary>
        /// Записывает статистику в базу данных
        /// </summary>
        public void SaveToBD()
        {
            try
            {
                // подключение к БД
                MySqlConnection connect = new MySqlConnection(connectionString);
                connect.Open();

                // перевод в JSON формат статистики длязаписи в БД
                string JSONstatistic = "[";
                for (int i = 0; i < this.Count - 1; i++)
                {
                    JSONstatistic += "[\"" + collection[i].first + "\", " + collection[i].second + "], ";
                }
                JSONstatistic += "[\"" + collection[this.Count - 1].first + "\", " + collection[this.Count - 1].second + "]]";

                // проверка, находится ли статистика по этой странице в БД
                string query;
                query = "SELECT * FROM " + connect.Database + ".wordstatistics WHERE pageAddress = @parent.Page ";
                MySqlCommand command = new MySqlCommand(query, connect);
                command.Parameters.AddWithValue("@parent.Page", page);
                
                if (command.ExecuteScalar() == null)
                {
                    // вставляем новую строку, если такой страницы нет
                    query = "INSERT INTO " + connect.Database + ".wordstatistics (pageAddress, statistic) values (";
                    query += "\'" + page + "\'," + " \'" + JSONstatistic + "\'";
                    query += " )";

                    command = new MySqlCommand(query, connect);
                    command.ExecuteNonQuery();
                }
                else
                {
                    // иначе обновляем строку с этой страницей новой статистикой
                    query = "UPDATE " + connect.Database + ".wordstatistics SET statistic = @JSONstatistic";
                    query += " WHERE pageAddress = @parent.Page "; ;

                    command = new MySqlCommand(query, connect);
                    command.Parameters.AddWithValue("@parent.Page", page);
                    command.Parameters.AddWithValue("@JSONstatistic", JSONstatistic);
                    command.ExecuteNonQuery();
                }

                connect.Close();
            }
            catch (Exception e)
            {
                WriteLog(page, 4, e);
                return;
            }
        }

        private void quickSort(int start, int end, bool reverse)
        {
            if (start >= end)
            {
                return;
            }
            PairSI temp;
            int marker = start;
            for (int i = start; i < end; i++)
            {
                if ((!reverse && collection[i] < collection[end]) || (reverse && collection[i] > collection[end]))
                {
                    temp = collection[marker];
                    collection[marker] = collection[i];
                    collection[i] = temp;
                    marker += 1;
                }
            }
            temp = collection[marker];
            collection[marker] = collection[end];
            collection[end] = temp;

            quickSort(start, marker - 1, reverse);
            quickSort(marker + 1, end, reverse);
        }

        /// <summary>
        /// Формирует список уникальных слов в данном тексте и количество их повторений.
        /// </summary>
        /// <param name="p">Обработчик текста страницы, текст которого обрабатывается</param>
        /// <param name="text">Текст, по которому необходимо получить статистику</param>
        public WordFrequencyList(string text, TextFounder p)
        {
            // Список разделителей
            // char[] trimArray = new char[]{ ' ', ',', '.', '!', '?','"', '\'', ';', ':', '[', ']', '(', ')', '\n', '\r', '\t'};

            page = p.Page;
            connectionString = p.ConnectionString;

            Regex reg = new Regex(@"[^\s\,\.\!\?" + "\"" + @"\;\:\[\]\(\)\n\r\t\']+");
            MatchCollection wordMatches = reg.Matches(text);

            // для того, чтобы посчитать количество повторов слов, воспользуемся словарём
            Dictionary<string, int> wordDict = new Dictionary<string, int>();
            foreach (Match m in wordMatches)
            {
                if (wordDict.ContainsKey(m.Value.ToLower()))
                    wordDict[m.Value.ToLower()]++;
                else
                    wordDict.Add(m.Value.ToLower(), 1);
            }
            // перенесём в список, чтобы была возможность сортировки
            foreach (var d in wordDict)
            {
                this.collection.Add(new PairSI(d.Key, d.Value));
            }
            size = collection.Count();
        }

        /// <summary>
        /// Загружает сохранённую статистику из БД
        /// </summary>
        /// <param name="_page">Страница, статистикупо которой нужно загрузить</param>
        /// <param name="_connectionString">Строка для подключения к БД</param>
        public WordFrequencyList(string _page, string _connectionString)
        {
            page = _page;
            connectionString = _connectionString;
            try
            {
                // подключение к БД
                MySqlConnection connect = new MySqlConnection(connectionString);
                connect.Open();

                // поиск статистики по этой странице в БД
                string query;
                query = "SELECT statistic FROM " + connect.Database + ".wordstatistics WHERE pageAddress = @parent.Page ";
                MySqlCommand command = new MySqlCommand(query, connect);
                command.Parameters.AddWithValue("@parent.Page", page);

                // проверка, сохранена ли страница в БД
                if (command.ExecuteScalar() == null)
                {
                    page = null;
                    return;
                }

                //получение JSON статистики и последующее преобразование в список пар.
                string JSONstatistics = command.ExecuteScalar().ToString();

                Regex reg = new Regex("(\""+ @"[^"+"\"" + "]*" + "\")");
                MatchCollection wordMatches = reg.Matches(JSONstatistics);
                reg = new Regex(@"(, \d+)");
                MatchCollection countMatches = reg.Matches(JSONstatistics);

                size = wordMatches.Count;
                for (int i = 0; i < size; i++)
                {
                    collection.Add(new PairSI(
                        wordMatches[i].Value.Substring(1, wordMatches[i].Value.Length-2), 
                        int.Parse(countMatches[i].Value.Substring(2))));
                }
                connect.Close();
            }
            catch (Exception e)
            {
                page = null;
                WriteLog(page, 5, e);
                return;
            }
        }
    }



    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
