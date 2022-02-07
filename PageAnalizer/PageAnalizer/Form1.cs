using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using PageAnalizer;

namespace PageAnalizer
{
    public partial class Form1 : Form
    {
        // строка, необходимая для подключения к бд с помощью MySqlConnection
        string connectionString = "Server=localhost;Database=pageanalizer;Uid=root;pwd=root;";
        // На данный момент база данных хранит в себе лишь одну таблицу - wordstatistics 
        // с полями id, pageAddress, statistic

        // основной объект, работающий с текстом одного сайта.
        TextFounder TF;

        public Form1()
        {
            InitializeComponent();

            TF = new TextFounder("", connectionString);
        }


        private void button2_Click(object sender, EventArgs e)
        {
            // дополнительная кнопка для просмотра всех сохранённых в БД страниц.

            // подключение к БД
            MySqlConnection connect = new MySqlConnection(connectionString);
            connect.Open();

            // Получение всех сохранённых страниц
            string query = "SELECT pageAddress FROM " + connect.Database + ".wordstatistics";
            MySqlCommand command = new MySqlCommand(query, connect);
            MySqlDataReader reader = command.ExecuteReader();

            // вывод в большой textBox
            textBox2.Text = "";
            while (reader.Read())
            {
                textBox2.Text += reader[0] + "\r\n";
            }
            textBox2.Text = textBox2.Text.Substring(0, textBox2.Text.Length - 2);
            connect.Close();
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            // загрузка из БД статистики
            WordFrequencyList statistics = new WordFrequencyList(textBox1.Text, connectionString);
            // вывод в textBox
            textBox2.Text = "";
            if (statistics.Count != 0)
            {
                for (int i = 0; i < statistics.Count - 2; i++)
                {
                    textBox2.Text += statistics[i] + "\r\n";
                }
                textBox2.Text += statistics[statistics.Count - 1];
            }
            else
            {
                textBox2.Text = "Такая стараница не была сохранена ранее.";
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // загрузка текста страницы
            TF.Page = textBox1.Text;
            // загрузка текста из страницы и формирование статистики
            WordFrequencyList statistics = TF.GetUniqueWordStatistics();
            // вывод в textBox
            textBox2.Text = "";
            if (statistics != null)
            {
                for (int i = 0; i < statistics.Count - 2; i++)
                {
                    textBox2.Text += statistics[i] + "\r\n";
                }
                textBox2.Text += statistics[statistics.Count - 1];

                // сохранение в БД статистики
                statistics.SaveToBD();
            }
            else
            {
                textBox2.Text = "Ошибка выполнения. Проверьте логи.";
            }
            
        }
    }
}
