using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ZelvParser
{
    using static Tell;
    using static Program;
    class Preferences
    {
        
        public static void CheckFiles()
        {
            //проверяется обязательное наличие указанных файлов.
            string[] files = { "UA.txt", "Config.ini", "Proxy.txt", "chromedriver.exe" };
            foreach (string file in files) FileCheck(file);
        }

        public static void PrepareDatabase()
        {
            //Проверка и создание файла базы данных
            Storage.Database.CheckFile();
            //Если нужно - создание таблицы со ссылками
            Storage.Database.CreateTable();
            
        }

        public static void CheckDirectories()
        {
            string[] Directories = AppSettings.Directories.Split(',');

            audioD = Directories[0] + account;
            imagesD = Directories[1] + account;
            outputD = Directories[2] + account;
            tempD = Directories[3] + account;
            previewD = Directories[4] + account;

            foreach (string x in new[] {Program.imagesD,Program.audioD,Program.tempD,Program.outputD,previewD})
            { 
                string path = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), x);
                DirectoryInfo di = new DirectoryInfo(path);
                if (!di.Exists) di.Create();
            }
        }

        public static void FileCheck(string filename)
        {
            if (!File.Exists(System.IO.Path.Combine(Program.Path, filename)))
            {
                ColoredMessage($"Отсутствует файл {filename}", ConsoleColor.DarkRed); Console.ReadKey(); throw new Exception($"Отсутсвует файл {filename}");
            }
        }
    }
}
