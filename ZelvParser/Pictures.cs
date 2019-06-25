using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ZelvParser
{
    class Picture //класс относящийся к информации из видеофайла
    {
            public string Name { get; set; } //Свойство название файла
            public string Link { get; set; } //Свойство ссылка
            
            public Picture(string name, string link) { Name = name; Link = link; }//конструктор отвечающий за создание одного экземпляра класса информации о картинке
    }

    class Check
    {
        public static void Pictures(List<string> list)
        {
            //получить массив из картинок к статьям
            //картинки сериализованы через json
            var picturesArray = Storage.Database.GetPictures("Records", new[] { "Pictures" }, list);
            
            //перебор по полученному массиву
            foreach (var x in picturesArray)
            {
                //получили массив из отдельных картинок и ссылок на них к каждой отдельной статье
                List<Picture> jsToPicture = Deserialize.JsonToListPicture(x);

                for (int i = 0; i < jsToPicture.Count; i++)
                {
                    int counter = 0;
					download:
                    Picture picture = jsToPicture[i];
                    string path = Path.Combine(Program.imagesD, picture.Name);
                    if(!File.Exists(path))
                    {
                        if (counter > 3) continue;
                        counter++;
                        int result = LoadPicture(picture);
                        if (result == -1)
                        {
                            string s = $@"DELETE FROM Records WHERE Pictures like '%{picture.Name}%'";
                            Storage.Database.ExecuteInsert(s);
                            continue;
                        }
                        if (File.Exists(path)) Console.WriteLine($"Picture {picture.Name} downloaded");
						else goto download;
                    }
                    else Console.WriteLine($"Picture {picture.Name} exists");
                }
            }
        }

        public static int DownloadOnce(string name)
        {
            string pictures = Storage.Database.VoidPictureDownload("Records", "Pictures", "Pictures", name);

            var js = Deserialize.JsonToListPicture(pictures);
            int counter = 0;
            for (int i = 0; i < js.Count; i++)
            {
				download1:
                Picture picture = js[i];
                string path = Path.Combine(Program.imagesD, picture.Name);
                if (!File.Exists(path))
                {
                    int result = LoadPicture(picture);
                    if (File.Exists(path)) Console.WriteLine($"Картинка {picture.Name} загружена");
                    else
                    {
                        if (counter > 3||result == -1) return -1;
                        counter++;
                        goto download1;
                    }
                }
                else Console.WriteLine($"Картинка {picture.Name} существует");
            }
            return 0;
        }  

        static int LoadPicture(Picture picture)
        {
            string result = GetData.Download(url: picture.Link, downloadMode: "File", filename: picture.Name);
            if (result == "404") return -1;
            return 0;
        }

        
    }  
}
