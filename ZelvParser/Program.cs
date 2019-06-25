using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ImageMagick;
using static System.Console;
using syspath = System.IO;


namespace ZelvParser
{
    using static Preferences;
    using static Tell;


    class Program
    {
        //путь к директории
        public static string Path = System.IO.Directory.GetCurrentDirectory();

        public static Random r = new Random();
        public static int c = 0;

        //путь к текущей директории аудио
        public static string audioD = string.Empty;

        //путь к текущей директории картинок
        public static string imagesD = string.Empty;

        //путь к текущей временной директории 
        public static string tempD = string.Empty;

        //путь к текущей директории с созданными видеофайлами
        public static string outputD = string.Empty;

        //путь к текущей директории с превью.
        public static string previewD = string.Empty;

        //какие части выполнять.
        public static string mode = AppSettings.Do;

        //пресет для ffmpeg 
        public static string preset = AppSettings.FFmpegPreset;

        //для какого сайта ведется работа.
        public static string project = System.IO.Path.GetFileNameWithoutExtension(AppSettings.Database);

        public static int account = 1;
        static void Main(string[] args)
        {
            try
            {
                string token = syspath.Path.Combine(Path, "wrong-token.txt");
                if(File.Exists(token))
                {
                    Tell.ColoredMessage("Нужно поменять токен\n\nДля выхода нажмите любую клавишу",ConsoleColor.Red);
                    Console.ReadKey();
                    return;
                }

                //проверка директорий
                CheckDirectories();

                //оповещение в телеграм канал.
                FFmpegMain.NotifyAnErrorViaTelegramBot($"Запущен {project}Parser");

                //проверка наличия нужных файлов
                CheckFiles();

                //подготовка базы данных - если ее нет, то она создается заново.
                PrepareDatabase();

                File.Copy(Program.project + ".sqlite3", $"{project}_copy_last.sqlite3", true);

                if (!string.IsNullOrEmpty(AppSettings.TestUrl))
                {
                    mode = "234567";
                }

                Tell.ColoredMessage($"База данных {project}", ConsoleColor.Cyan);
                switch (project)
                {
                    case "Labirint":
                    case "Hollywood":
                    case "Zelv":

                        Database();
                        break;
                    case "Text":
                        New();
                        break;
                }
            }
            catch (Exception e)
            {
                Log.SendAndText("errorMain.txt", e);
            }
        }


        static void New()
        {
            Tell.ColoredMessage($"Настройка FromFile: {AppSettings.FromFile}", ConsoleColor.Cyan);
            switch (AppSettings.FromFile)
            {
                default:
                    Database();
                    break;
                case 1:
                    TxtFile();
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        static void TxtFile()
        {

            DirectoryInfo di = new DirectoryInfo(syspath.Path.Combine(Path, "Text"));

            if (mode.Contains("1"))
            {
                if (!string.IsNullOrEmpty(AppSettings.TestUrl))
                {
                    String text = syspath.File.ReadAllText(syspath.Path.Combine(di.FullName, AppSettings.TestUrl));
                    text = Regex.Replace(text, @"\r\n", " ");
                    bool result = Parsing.ParseTextFromFile(text);
                    if (result == false) return;
                    MoveFile(syspath.Path.Combine(di.FullName, AppSettings.TestUrl), "TextDone");
                }
                else
                {
                    foreach (string textFilePath in syspath.Directory.GetFiles(di.FullName))
                    {
                        //получение текста из папки текст
                        string text = GetFileFromFolder(textFilePath);
                        if (string.IsNullOrEmpty(text))
                        {
                            File.AppendAllText(System.IO.Path.Combine(Path, $"errorText {DateTime.Now.ToShortDateString()}"), "нет входящих файлов в папке Text");
                            return;
                        }

                        text = Regex.Replace(text, @"\r\n", " ");

                        //парсинг текста и сохранение его в базу данных
                        bool result = Parsing.ParseTextFromFile(text);
                        if (result == false) break;
                        MoveFile(syspath.Path.Combine(di.FullName, textFilePath), "TextDone");
                    }
                }
            }
            CreateTextVideo();

        }

        private static void MoveFile(string From, string To)
        {
            string ToDirectory = syspath.Path.Combine(Program.Path, To);
            if (!Directory.Exists(ToDirectory)) Directory.CreateDirectory(ToDirectory);
            string ToFile = syspath.Path.Combine(ToDirectory, syspath.Path.GetFileName(From));
            File.Move(From, ToFile);
        }

        static void CreateTextVideo()
        {
            //создание mp3 из текста.
            //нюанс - сейчас нет уже ссылки на статью, надо как-то по другому.
            string message = string.Empty;
            Dictionary<string, string> list = Storage.Database.GetAllTable("Records", "Audio");
            if (list.Count > 0)
            {
                if (mode.Contains("5"))
                {
                    //Часть 5
                    message = "Режим проверки и создания Mp3 файлов";
                    ChangePart(message, ConsoleColor.DarkYellow);
                    //Получение Mp3 из спаршенного текста
                    Polly.CreateMp3(list);
                }
            }

            //создание видеоролика
            list = Storage.Database.GetAllTable("Records", "VidName");
            if (list.Count > 0)
            {
                List<string> Urls = list.Select(x => x.Key).ToList();

                if (mode.Contains("6"))
                {
                    //Часть 6
                    message = "Режим создания видео из mp3 и картинок";
                    ChangePart(message, ConsoleColor.Magenta);
                    //Запуск FFmpeg монстра
                    FFmpegMain.NoPictures(Urls);
                }

                if (mode.Contains("7"))
                {
                    //Часть 7
                    message = "Режим проверки созданного видео";
                    ChangePart(message, ConsoleColor.DarkGreen);
                    Storage.Database.CheckVideos("Records", Urls);
                }
                if (mode.Contains("8"))
                {
                    FFmpegMain.NotifyAnErrorViaTelegramBot($"Запущен шаблон постинга {project}");
                    //Часть 8
                    message = "Запуск шаблона Zenno Poster";
                    Tell.ChangePart(message, ConsoleColor.Magenta);

                    if (AppSettings.Template1 != null)
                    {
                        string name = AppSettings.Template1 + account + ".bat";
                        Program.Poster(name, Urls.Count);
                    }
                    mode = mode.Replace("8", "");
                }
            }
            else
            {
                Tell.ColoredMessage("В базе нет ничего для создания видео\nНажмите любую клавишу...", c: ConsoleColor.Blue);
                Console.ReadKey();
            }
        }

        static string GetFileFromFolder(string path)
        {
            DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(Path, "Text"));

            if (!di.Exists) return "";

            List<string> files = System.IO.Directory.GetFiles(di.FullName).ToList();
            if (files.Count == 0) return "";

            string text = System.IO.File.ReadAllText(path);
            return text;
        }

        /// <summary>
        /// Стандартный алгооритм парсинга с сайта и прочее.
        /// </summary>
        static void Database()
        {
            switch (AppSettings.Mode)
            {
                case "1":
                    Tell.ColoredMessage($"Настройка Mode: {AppSettings.Mode}", ConsoleColor.Cyan);
                    ParseOnlyBase();
                    break;
                case "2":
                    Tell.ColoredMessage($"Настройка Mode: {AppSettings.Mode}", ConsoleColor.Cyan);
                    int RecordsCount = int.Parse(Storage.Database.CountRows("Records", "VidName"));
                    if (RecordsCount < 100) ParseOnlyBase();
                    while (account < AppSettings.AccsCount + 1)
                    {
                        switch (AppSettings.WithPictures)
                        {
                            default:
                                Tell.ColoredMessage($"Настройка WithPictures: {AppSettings.WithPictures}", ConsoleColor.Cyan);
                                CreateVideo();
                                break;
                            case 0:
                                Tell.ColoredMessage($"Настройка WithPictures: {AppSettings.WithPictures}", ConsoleColor.Cyan);
                                CreateTextVideo();
                                break;
                        }
                        account++;
                    }
                    break;
            }
        }

        //сбор ссылок из XML sitemap, общий для всех
        static void ParseOnlyBase()
        {
            string message = string.Empty;
            if (mode.Contains("1"))
            {
                //Часть 1
                message = "Режим сбора данных с xml и запись в базу данных";
                ChangePart(message, ConsoleColor.DarkGray);
                //получить голый sitemap.xml и записать ссылки в базу данных
                Parsing.ParseXmls();
            }
            if (mode.Contains("2"))
            {
                List<string> list = new List<string>();
                //разница между таблицами Current и Records

                switch (Program.project)
                {
                    case "Hollywood":
                    case "Deadline":
                    case "Zelv":
                        list = Parsing.ExceptLists();
                        break;
                    case "Labirint":
                        list = Parsing.LabirintExceptLists();
                        break;
                }



                message = string.Empty;
                //Часть 2
                message = "Режим сбора данных с сайта и запись в базу данных";
                ChangePart(message, ConsoleColor.DarkGreen);
                if (!string.IsNullOrEmpty(AppSettings.TestUrl)) list.Add(AppSettings.TestUrl);
                //парсинг с помощью AngleSharp
                Parsing.Start(list);
            }
            if (mode.Contains("3"))
            {
                //Часть 3
                message = "Копирование значений из таблицы Current в Catalog";
                //копирование из таблицы current в каталог
                Storage.Database.SendToCatalog();
            }

        }

        static void CreateVideo()
        {
            CheckDirectories();
            try
            {
                //проверка наличия директорий для этой части цикла аккаунта
                string message = string.Empty;
                Dictionary<string, string> list = Storage.Database.GetAllTable("Records", "Audio");
                if (list.Count > 0)
                {
                    if (mode.Contains("5"))
                    {
                        //Часть 5
                        message = "Режим проверки и создания Mp3 файлов";
                        ChangePart(message, ConsoleColor.DarkYellow);
                        //Получение Mp3 из спаршенного текста
                        Polly.CreateMp3(list);
                    }
                }
                list = Storage.Database.GetAllTable("Records", "VidName");
                if (list.Count > 0)
                {
                    List<string> Urls = list.Select(x => x.Key).ToList();



                    if (mode.Contains("4"))
                    {
                        //Часть 4
                        message = "Проверка картинок";
                        ChangePart(message, ConsoleColor.Blue);
                        //проверка наличия фотографий в папке Images
                        Check.Pictures(Urls);
                    }

                    if (mode.Contains("6"))
                    {
                        //Часть 6
                        message = "Режим создания видео из mp3 и картинок";
                        ChangePart(message, ConsoleColor.Magenta);
                        //Запуск FFmpeg монстра
                        FFmpegMain.Start(Urls);
                    }

                    if (mode.Contains("7"))
                    {
                        //Часть 7
                        message = "Режим проверки созданного видео";
                        ChangePart(message, ConsoleColor.DarkGreen);
                        Storage.Database.CheckVideos("Records", Urls);
                    }
                    if (mode.Contains("8"))
                    {
                        FFmpegMain.NotifyAnErrorViaTelegramBot($"Запущен шаблон постинга {project}");
                        //Часть 8
                        message = "Запуск шаблона Zenno Poster";
                        Tell.ChangePart(message, ConsoleColor.Magenta);

                        if (AppSettings.Template1 != null)
                        {
                            string name = AppSettings.Template1 + account + ".bat";
                            Program.Poster(name, Urls.Count);
                        }
                        mode = mode.Replace("8", "");
                    }
                }
            }
            catch (Exception e)
            {
                Log.SendAndText("errorCreateVideo.txt", e);
            }
        }

        public static void Poster(string name, int count)
        {
            if (!string.IsNullOrEmpty(name))
            {
                if (!File.Exists(name)) return;

                int tries = Directory.GetFiles(outputD).Length;

                //сколько раз запускать шаблон
                if (tries > 100) tries = 100;

                string bat = File.ReadAllText(name);

                bat = Regex.Replace(bat, @"SetTries\s\d{1,3}", $"SetTries {count}");

                File.WriteAllText(name, bat);

                var startInfo = new ProcessStartInfo(name);

                Process.Start(startInfo);
            }
        }
    }
}