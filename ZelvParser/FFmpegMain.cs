using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using System.Diagnostics;
using System.IO;
using System.Net;
using static System.Console;


namespace ZelvParser
{
    using static Program;
    using static AppSettings;/*использование настроек из файла AppSettings.cs*/
    public class FFmpegMain
    {
        public class VideoInfo //класс относящийся к информации из видеофайла
        {
            public string Name { get; set; } //Свойство название файла
            public string Title { get; set; } //Свойство заголовок
            public List<VideoPartInfo> Parts { get; set; } = new List<VideoPartInfo>();//список относящийся к частям видео

            public VideoInfo(string name, string title) { Name = name; Title = title; }//конструктор отвечающий за создание одного экземпляра класса информации о видео
        }

        public class VideoTextInfo //класс относящийся к информации из видеофайла
        {
            public string Name { get; set; } //Свойство название файла
            public string Title { get; set; } //Свойство заголовок
            public List<TextPartInfo> Parts { get; set; } = new List<TextPartInfo>();//список относящийся к частям видео

            public VideoTextInfo(string name, string title) { Name = name; Title = title; }//конструктор отвечающий за создание одного экземпляра класса информации о видео
        }

        public class TextPartInfo //класс относящийся к информации к одной единственной части видеофайла
        {
            public Guid Id { get; } = Guid.NewGuid();//свойство guid файла
            public string Audio { get; }//свойство аудио

            public string FileName { get; }
            public string Text { get; }


            public TextPartInfo(string filename, string text, string audio) { FileName = filename; Text = text; Audio = audio; }//конструктор, отвечающий за создание экземпляра класса части информации о видео
        }

        public class VideoPartInfo //класс относящийся к информации к одной единственной части видеофайла
        {
            public Guid Id { get; } = Guid.NewGuid();//свойство guid файла
            public string Image { get; }//свойство картинки
            public string Audio { get; }//свойство аудио

            public VideoPartInfo(string image, string audio) { Image = image; Audio = audio; }//конструктор, отвечающий за создание экземпляра класса части информации о видео
        }

        static string id = string.Empty;

        public static void ImageProcessing(string sourceImageFilePath, string targetImageFilePath)//создание изображения во временной папке
        {
            try
            {
                using (var sourceImage = new MagickImage(sourceImageFilePath))//создаем экземпляр magick image с использованием картинки из папки
                {
                    int width = VideoScaleWidth;//шириной будет ширина видео
                    int heigt = VideoScaleHeight;//высотой будет высота видео
                    byte r = VideoBackgroundColor[0];//получить из настроек байтового массива значений значение red
                    byte g = VideoBackgroundColor[1];//получить из настроек байтового массива значений значение green
                    byte b = VideoBackgroundColor[2];//получить из настроек байтового массива значений значение blue

                    sourceImage.Scale(width, heigt);//растянуть изображение по заданным настройкам ширины и высоты

                    var backgroundImage = new MagickImage(MagickColor.FromRgb(r, g, b), width, heigt);//создать экземпляр для временного файла изображения
                    backgroundImage.Composite(sourceImage, Gravity.Center);//совместить в нем текущее изображение в середину
                    backgroundImage.Write(targetImageFilePath);//записать изображение в файл по требуемому пути к этому файлу.
                }
            }
            catch (Exception e)
            {
                Log.SendAndText("errorWriteErrorIssue.txt", e);
            }
        }

        public static void NotifyAnErrorViaTelegramBot(string message)//объявить об ошибке в телеграм
        {
            if (AppSettings.EnableTelegramBotNotifications)
            {
                using (var webClient = new WebClient())
                    webClient.DownloadString($"https://api.telegram.org/bot{TelegramBotApiToken}/sendMessage?chat_id={TelegramBotUserId}&text=" + message);

            }
        }

        static List<VideoInfo> ReadDBAndCheckForFiles(List<string> Urls)//статическая функция, возвращающая список с информацией о видеофайлах
        {

            var videos = new List<VideoInfo>();//обозначить список информации о файлах

            var readDB = Storage.Database.GetMultiplyValues("Records", new[] { "Link", "Title", "Audio", "PicturesList" }, Urls);

            int idx = 0;

            List<int> nums = new List<int>();
            List<string> s = Directory.GetFiles(System.IO.Path.Combine(Program.outputD)).ToList();
            foreach (string s1 in s) nums.Add(Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(s1)));
            int VideoNum = nums.Count;
            if (VideoNum > 0)
            {
                nums.Sort();
                int numslast = nums.Last();//Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(nums.Last()));
                idx = numslast;
            }
            foreach (var row in readDB)//цикл получения содержимого ячеек
            {
                idx++;//увеличить счетчик idx

                string title = row.Value[0];//получение значения ячейки
                string[] audio = row.Value[1].Split(';');//получить значение аудио
                string[] image = row.Value[2].Split(';');//получить значение картинки


                var video = new VideoInfo(idx + ".mp4", title);//сделать новый экземпляр класса VideoInfo

                for (int count = 0; count < audio.Length; count++) video.Parts.Add(new VideoPartInfo(image[count], audio[count]));//добавить в список частей новый экземпляр класса частей видео

                videos.Add(video);//добавляем получившийся экземпляр VideoPartInfo в список VideoInfo
            }

            //check for files
            foreach (var filePath in videos.SelectMany(v => v.Parts.Select(p => imagesD + '/' + p.Image).Concat(v.Parts.Select(p => audioD + '/' + p.Audio))))
                if (!File.Exists(filePath))
                {
                    int result = 0;
                    if (filePath.Contains("jpg")) result = Check.DownloadOnce(System.Text.RegularExpressions.Regex.Replace(filePath, @"^.*?/", ""));
                    if (filePath.Contains("mp3")) result = Polly.BackToTheFuture(filePath);

                    if (result != 0)
                    {
                        int index = 0;
                        bool found = false;
                        foreach (VideoInfo x in videos)
                        {
                            foreach (VideoPartInfo y in x.Parts)
                            {
                                if (filePath.Contains("jpg")) if (y.Image.Contains(System.IO.Path.GetFileName(filePath)))
                                    {
                                        index = videos.IndexOf(x);
                                        found = true;
                                    }
                                if (filePath.Contains("mp3")) if (y.Image.Contains(System.IO.Path.GetFileName(filePath)))
                                    {
                                        index = videos.IndexOf(x);
                                        found = true;
                                    }
                            }
                        }

                        if (found) videos.RemoveAt(index);
                    }
                }

            return videos;
        }

        public static List<string> ViewCheck(bool check, string name, List<string> partList)
        {
            string filePath = System.IO.Path.Combine("TS", System.IO.Path.GetFileNameWithoutExtension(name) + ".ts").Replace("\\", "/");
            //если useTS стоит true, а таких файлов нет в папке TS то сначала их нужно создать,
            if (UseTS && !File.Exists(filePath)) UseTS = false;
            if (UseTS)
                //вставить путь к файлу в начало списка 
                partList.Insert(name.Contains("start") ? 0 : partList.Count, filePath);

            else
            {
                if (check)
                {
                    //скопировать начальную заставку mp4 во временную директорию
                    File.Copy(name, Program.tempD + '/' + name, true);
                    //Сконвертировать начальную заставку в ts и оставить в папке 
                    FFmpeg.ConvertVideoToTS(Program.tempD + '/' + name, out string result1);
                    //Скопировать получившийся файл ts в папку TS
                    File.Copy(result1, filePath, true);
                    partList.Insert(name.Contains("start") ? 0 : partList.Count, filePath);
                }
            }
            return partList;
        }

        static void CheckWidth(IMagickImage image, string path)
        {
            //var x = File.GetAccessControl(sourceImageFilePath);
            //TagLib.File file = TagLib.File.Create(sourceImageFilePath);

            using (var sourceImage = new MagickImage(image))//создаем экземпляр magick image с использованием картинки из папки
            {
                byte r = VideoBackgroundColor[0];//получить из настроек байтового массива значений значение red
                byte g = VideoBackgroundColor[1];//получить из настроек байтового массива значений значение green
                byte b = VideoBackgroundColor[2];//получить из настроек байтового массива значений значение blue

                int width = sourceImage.Width;//шириной будет ширина видео
                int heigt = sourceImage.Height;//высотой будет высота видео

                if (sourceImage.Width < 1920)
                {
                    if (sourceImage.Width % 2 != 0)
                    {
                        using (var backgroundImage = new MagickImage(MagickColor.FromRgb(r, g, b), width + 1, heigt))
                        {
                            backgroundImage.Composite(sourceImage, Gravity.Center);//совместить в нем текущее изображение в середину
                            backgroundImage.Format = MagickFormat.Jpeg;
                            backgroundImage.Write(path);//записать изображение в файл по требуемому пути к этому файлу.

                        }//создать экземпляр для временного файла изображения

                    }
                }
                else if (sourceImage.Width > 1920)
                {

                    sourceImage.Crop(1900, VideoScaleHeight); //ChopHorizontal(sourceImage.Width - 1920, 1920);//  Resize(VideoScaleWidth, VideoScaleHeight);


                    using (var backgroundImage = new MagickImage(MagickColor.FromRgb(r, g, b), 1600, VideoScaleHeight))
                    {
                        //создать экземпляр для временного файла изображения

                        backgroundImage.Composite(sourceImage, Gravity.Center);//совместить в нем текущее изображение в середину
                        backgroundImage.Format = MagickFormat.Jpeg;
                        backgroundImage.Write(path);//записать изображение в файл по требуемому пути к этому файлу.
                    }
                }
            }
        }
        /// <summary>
        /// запуск генерации видео
        /// </summary>
        /// <param name="Urls"></param>
        public static void NoPictures(List<string> Urls)
        {
            NotifyAnErrorViaTelegramBot($"Запущен {Program.project} FFmpegmonster / Режим NoPictures");
            Title = $"{Program.project} FFmpeg Monster";//название программы
            ForegroundColor = ConsoleColor.Red;//цвет текста консоли
            WriteLine($"=== {Title} ===\n");//написать название программы

            List<VideoTextInfo> videos = new List<VideoTextInfo>();

            try
            {
                videos = ReadDB(Urls);//прочитать базу данных
            }
            catch (Exception ex)
            {
                Tell.ColoredMessage($"{ex.StackTrace}", ConsoleColor.Red);
                Console.ReadKey();
            }

            //создать видео
            if (!Directory.Exists(Program.outputD)) Directory.CreateDirectory(Program.outputD);//если директории Output нет - то создать
            DateTime ts = DateTime.Now;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            foreach (var video in videos)//перебор списка VideoInfo
            {
                Stopwatch ts_all = new Stopwatch();
                ts_all.Start();
                string overlayPath = string.Empty;
                if (string.IsNullOrEmpty(AppSettings.VideoTemplateName))
                {
                    string overlay = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "VideoTemplate");
                    DirectoryInfo di = new DirectoryInfo(overlay);

                    if (!di.Exists) return;

                    string[] files = System.IO.Directory.GetFiles(di.FullName);
                    if (files.Count() != 0)
                    {
                        Random r = new Random();
                        string fn = System.IO.Path.GetFileName(files[r.Next(files.Count())]);
                        overlayPath = $"VideoTemplate/{fn}";

                    }
                }
                else overlayPath = $"VideoTemplate/{AppSettings.VideoTemplateName}";

                string result = MakeTextVideo(video, overlayPath);
                if (result == "") continue;

                Tell.ColoredMessage($"Elapsed {ts_all.Elapsed.Hours}:{ts_all.Elapsed.Minutes}:{ts_all.Elapsed.Seconds}", ConsoleColor.Blue);
                TimeSpan ts1 = DateTime.Now - ts;
                if (ts1.TotalSeconds > AppSettings.StartAfter)
                {
                    if (mode.Contains("8"))
                    {
                        FFmpegMain.NotifyAnErrorViaTelegramBot("Запущен шаблон постинга");
                        //Часть 8
                        string message = "Запуск шаблона Zenno Poster";
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
            Tell.ColoredMessage($"Elapsed total {sw.Elapsed.Hours}:{sw.Elapsed.Minutes}:{sw.Elapsed.Seconds}", ConsoleColor.Blue);
            System.Threading.Thread.Sleep(new TimeSpan(0, 0, 30));
        }

        private static string MakeTextVideo(VideoTextInfo video, string overlayPath)
        {
            if (string.IsNullOrEmpty(video.Parts[0].Audio)) return $"Отсутствует аудио {video.Name}";
            ForegroundColor = ConsoleColor.Green;//Сделать цвет консоли зеленым
            Write($"[{DateTime.Now.ToLongTimeString()}] File ");//написать в консоли текущую дату

            ForegroundColor = ConsoleColor.Red;//Сделать цвет консоли зеленым
            Write($"{video.Name} ");//написать в консоли название видео

            ForegroundColor = ConsoleColor.DarkGray;//Сделать цвет консоли темно-серым
            Write($"({video.Title}) ");//написать в консоли заголовок видео

            ForegroundColor = ConsoleColor.Magenta;//Сделать цвет консоли  пурпурным
            WriteLine($"- start processing in {Environment.ProcessorCount} threads:");//написать количество ядер процессора в качестве количества потоков

            var stopwatch = Stopwatch.StartNew();//запустить таймер

            var tempDir = new DirectoryInfo(Program.tempD);//временная директория
            tempDir.Create();//создать ее

            //tempDir.Attributes = FileAttributes.Hidden;//сделать ее скрытой


            List<string> pList = new List<string>();



            ////////////////////////////
            //Создание отдельных частей/
            ////////////////////////////

            switch (AppSettings.Scraping)
            {
                case "M":
                    var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };//установить количество потоков равным количеству ядер процессора
                    Parallel.ForEach(
                    source: video.Parts/*количество частей для работы*/,
                    parallelOptions: parallelOptions/*установка параллельных потоков*/,
                    body: delegate (TextPartInfo videoPart)/*как называется название из списка частей для работы*/

                    {
                        MakeTextPart(videoPart, overlayPath);
                    });
                    break;
                case "O":
                    foreach (TextPartInfo videoPart in video.Parts) MakeTextPart(videoPart, overlayPath);
                    break;
            }


            ////////////////////////////
            //Объединение частей////////
            ////////////////////////////

            //Объединение названий частей-файлов видео в один список
            var partList = video.Parts.Select(x => x.Id + ".ts").ToList();
            for (int i = 0; i < partList.Count; i++) partList[i] = $"{Program.tempD}/{partList[i]}";

            for (int i = 0; i < partList.Count; i++) if (!FFmpeg.CheckFile(partList[i])) return $"Отсутствует часть {partList[i]}";

            string filePath = outputD + '/' + video.Name;

            //if (FFmpeg.mute) FFmpeg.JoinAudio(video);


            //Конвертация выбранных частей в ts файл
            FFmpeg.ConvertTSToMp4v2(filePath, partList.ToArray());


            //if (AppSettings.Ads != "N") Advertisement(partList,filePath); 
            //////////////////////////////////////////
            //Окончание работы с одной статьей////////
            //////////////////////////////////////////


            stopwatch.Stop();//остановить таймер
            ForegroundColor = ConsoleColor.Green;//сделать текст консоли зеленым
            Write($"[{DateTime.Now.ToLongTimeString()}] File ");//написать в консоли дату и время

            ForegroundColor = ConsoleColor.Red;//сделать текст консоли красным
            Write($"{video.Name} ");//написать название видеофайла

            ForegroundColor = ConsoleColor.DarkGray;//сделать текст консоли темно-серым
            Write($"({video.Title}) ");//написать заголовок видеофайла

            ForegroundColor = ConsoleColor.Green;//сделать текст консоли зеленым
            WriteLine($"- processing done in {stopwatch.Elapsed.ToString(@"mm\:ss")}\n");//написать в консоли сколько минут и секунд заняло достижение результата




            if (File.Exists(filePath))
            {
                try
                {
                    TagLib.File f = TagLib.File.Create(filePath);
                    if (f.Properties.Duration.TotalSeconds > 10)
                    {
                        //if (AppSettings.CreatePreview == "1") CreatePreview(video);

                        //обновить базу данных с отметкой о создании видео
                        Storage.Database.UpdateData("Records", "VidName", video.Name, video.Title);
                        Storage.Database.UpdateData("Records", "Created", $"{Program.account}", video.Title);
                        
                    }
                }
                catch
                {
                    return "";
                }
            }


            foreach (var v in video.Parts)
            {

                var path = new FileInfo(System.IO.Path.Combine(audioD, v.Audio));
                if (path.Exists) path.Delete();
            }
            try
            {
                foreach (var file in tempDir.EnumerateFiles()) file.Delete();//удалить временную директорию
            }
            catch (Exception ex)
            {
                Tell.ColoredMessage($"{ex.StackTrace}", ConsoleColor.Red);
                Console.ReadKey();
            }
            return "OK";
        }

        private static List<VideoTextInfo> ReadDB(List<string> Urls)
        {
            var videos = new List<VideoTextInfo>();//обозначить список информации о файлах

            var readDB = Storage.Database.GetMultiplyValues("Records", new[] { "Link", "Title", "Audio", "TextParts" }, Urls);

            int idx = 0;

            List<int> nums = new List<int>();
            List<string> s = Directory.GetFiles(System.IO.Path.Combine(Program.outputD)).ToList();
            foreach (string s1 in s) nums.Add(Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(s1)));
            int VideoNum = nums.Count;
            if (VideoNum > 0)
            {
                nums.Sort();
                int numslast = nums.Last();//Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(nums.Last()));
                idx = numslast;
            }
            foreach (var row in readDB)//цикл получения содержимого ячеек
            {
                idx++;//увеличить счетчик idx

                string title = row.Value[0];//получение значения ячейки
                string[] audio = row.Value[1].Split(';');//получить значение аудио
                Dictionary<string, string> text = Deserialize.JsonToDictionaryString(row.Value[2]);


                var video = new VideoTextInfo(idx + ".mp4", title);//сделать новый экземпляр класса VideoInfo

                for (int count = 0; count < audio.Length; count++)
                {
                    var dic = text.ElementAt(count);
                    video.Parts.Add(new TextPartInfo(dic.Key, dic.Value, audio[count]));//добавить в список частей новый экземпляр класса частей видео
                }
                videos.Add(video);//добавляем получившийся экземпляр VideoPartInfo в список VideoInfo
            }
            /*
            //check for files
            foreach (var filePath in videos.SelectMany(v => v.Parts.Select(p => audioD + '/' + p.Audio)))
                if (!File.Exists(filePath))
                {
                    int result = 0;
                    if (filePath.Contains("jpg")) result = Check.DownloadOnce(System.Text.RegularExpressions.Regex.Replace(filePath, @"^.*?/", ""));
                    if (filePath.Contains("mp3")) result = Polly.BackToTheFuture(filePath);

                    if (result != 0)
                    {
                        int index = 0;
                        bool found = false;
                        foreach (VideoTextInfo x in videos)
                        {
                            foreach (TextPartInfo y in x.Parts)
                            {
                                if (filePath.Contains("mp3")) if (y.Image.Contains(System.IO.Path.GetFileName(filePath)))
                                    {
                                        index = videos.IndexOf(x);
                                        found = true;
                                    }
                            }
                        }

                        if (found) videos.RemoveAt(index);
                    }
                }
                */
            return videos;
        }

        public static void Start(List<string> Urls)
        {
            NotifyAnErrorViaTelegramBot($"Запущен {Program.project} FFmpegmonster");
            Title = $"{Program.project} FFmpeg Monster";//название программы
            ForegroundColor = ConsoleColor.Red;//цвет текста консоли
            WriteLine($"=== {Title} ===\n");//написать название программы

            List<VideoInfo> videos = new List<VideoInfo>();
            try
            {
                videos = ReadDBAndCheckForFiles(Urls);//прочитать базу данных
            }
            catch (Exception ex)
            {
                Tell.ColoredMessage($"{ex.StackTrace}", ConsoleColor.Red);
                Console.ReadKey();
            }


            if (!Directory.Exists(Program.outputD)) Directory.CreateDirectory(Program.outputD);//если директории Output нет - то создать
            DateTime ts = DateTime.Now;
            foreach (var video in videos)//перебор списка VideoInfo
            {
                Stopwatch ts_all = new Stopwatch();
                ts_all.Start();
                string result = MakeVideo(video);
                if (result == "") continue;

                Tell.ColoredWrite($"Elapsed {ts_all.Elapsed.TotalSeconds} seconds", ConsoleColor.Blue);
                TimeSpan ts1 = DateTime.Now - ts;
                if (ts1.TotalSeconds > AppSettings.StartAfter)
                {
                    if (mode.Contains("8"))
                    {
                        FFmpegMain.NotifyAnErrorViaTelegramBot("Запущен шаблон постинга");
                        //Часть 8
                        string message = "Запуск шаблона Zenno Poster";
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


            if (!CloseWindowAfterExecuting)//если закрытие окна не установлено
            {
                ForegroundColor = ConsoleColor.Red;//сделать цвет красным
                WriteLine();//отделить строкой
                WriteLine("Press any key to exit . . .");//написать ...
                ReadKey();//ждать нажатия
            }
        }

        public static string MakeVideo(VideoInfo video)
        {
            //if (File.Exists(Program.outputD + '/' + video.Name)) return;//если файл с таким названием существует, то перейти к следующей итерации цикла.
            if (string.IsNullOrEmpty(video.Parts[0].Audio)) return $"Отсутствует аудио {video.Name}";
            ForegroundColor = ConsoleColor.Green;//Сделать цвет консоли зеленым
            Write($"[{DateTime.Now.ToLongTimeString()}] File ");//написать в консоли текущую дату

            ForegroundColor = ConsoleColor.Red;//Сделать цвет консоли зеленым
            Write($"{video.Name} ");//написать в консоли название видео

            ForegroundColor = ConsoleColor.DarkGray;//Сделать цвет консоли темно-серым
            Write($"({video.Title}) ");//написать в консоли заголовок видео

            ForegroundColor = ConsoleColor.Magenta;//Сделать цвет консоли  пурпурным
            WriteLine($"- start processing in {Environment.ProcessorCount} threads:");//написать количество ядер процессора в качестве количества потоков

            var stopwatch = Stopwatch.StartNew();//запустить таймер

            var tempDir = new DirectoryInfo(Program.tempD);//временная директория
            tempDir.Create();//создать ее

            //tempDir.Attributes = FileAttributes.Hidden;//сделать ее скрытой


            List<string> pList = new List<string>();

            if (AppSettings.Effects == "5")
            {
                foreach (VideoPartInfo file in video.Parts)
                {
                    string pathToOriginalPicture = System.IO.Path.Combine(imagesD, file.Image);
                    string temp = CreateBigPicture(pathToOriginalPicture);
                    pList.Add(System.IO.Path.Combine(temp));
                }
            }

            ////////////////////////////
            //Создание отдельных частей/
            ////////////////////////////

            switch (AppSettings.Scraping)
            {
                case "M":
                    var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount };//установить количество потоков равным количеству ядер процессора
                    Parallel.ForEach(
                    source: video.Parts/*количество частей для работы*/,
                    parallelOptions: parallelOptions/*установка параллельных потоков*/,
                    body: delegate (VideoPartInfo videoPart)/*как называется название из списка частей для работы*/

                    {
                        MakeVideoPart(videoPart, pList);
                    });
                    break;
                case "O":
                    foreach (VideoPartInfo videoPart in video.Parts) MakeVideoPart(videoPart, pList);
                    break;
            }


            ////////////////////////////
            //Объединение частей////////
            ////////////////////////////

            //Объединение названий частей-файлов видео в один список
            var partList = video.Parts.Select(x => x.Id + ".ts").ToList();
            for (int i = 0; i < partList.Count; i++) partList[i] = $"{Program.tempD}/{partList[i]}";

            for (int i = 0; i < partList.Count; i++) if (!FFmpeg.CheckFile(partList[i])) return $"Отсутствует часть {partList[i]}";

            string filePath = outputD + '/' + video.Name;

            //if (FFmpeg.mute) FFmpeg.JoinAudio(video);


            //Конвертация выбранных частей в ts файл
            FFmpeg.ConvertTSToMp4v2(filePath, partList.ToArray());


            //if (AppSettings.Ads != "N") Advertisement(partList,filePath); 
            //////////////////////////////////////////
            //Окончание работы с одной статьей////////
            //////////////////////////////////////////


            stopwatch.Stop();//остановить таймер
            ForegroundColor = ConsoleColor.Green;//сделать текст консоли зеленым
            Write($"[{DateTime.Now.ToLongTimeString()}] File ");//написать в консоли дату и время

            ForegroundColor = ConsoleColor.Red;//сделать текст консоли красным
            Write($"{video.Name} ");//написать название видеофайла

            ForegroundColor = ConsoleColor.DarkGray;//сделать текст консоли темно-серым
            Write($"({video.Title}) ");//написать заголовок видеофайла

            ForegroundColor = ConsoleColor.Green;//сделать текст консоли зеленым
            WriteLine($"- processing done in {stopwatch.Elapsed.ToString(@"mm\:ss")}\n");//написать в консоли сколько минут и секунд заняло достижение результата




            if (File.Exists(filePath))
            {
                try
                {
                    TagLib.File f = TagLib.File.Create(filePath);
                    if (f.Properties.Duration.TotalSeconds > 10)
                    {
                        if (AppSettings.CreatePreview == "1") CreatePreview(video);

                        //обновить базу данных с отметкой о создании видео
                        Storage.Database.UpdateData("Records", "VidName", video.Name, video.Title);
                        Storage.Database.UpdateData("Records", "Created", $"{Program.account}", video.Title);
                    }
                }
                catch
                {
                    return "";
                }
            }


            foreach (var v in video.Parts)
            {

                var path = new FileInfo(System.IO.Path.Combine(audioD, v.Audio));
                if (path.Exists) path.Delete();

                path = new FileInfo(System.IO.Path.Combine(imagesD, v.Image));
                if (path.Exists) path.Delete();

            }
            try
            {
                foreach (var file in tempDir.EnumerateFiles()) file.Delete();//удалить временную директорию
            }
            catch (Exception ex)
            {
                Tell.ColoredMessage($"{ex.StackTrace}", ConsoleColor.Red);
                Console.ReadKey();
            }
            return "OK";
        }

        private static void Advertisement(List<string> partList, string outputFilePath)
        {
            DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(Program.Path, "Ads"));
            var files = di.EnumerateFiles().ToList();
            FileInfo fi = null;


            switch (Ads)
            {
                case "F":
                    fi = files.ElementAt(0);
                    break;
                case "R":
                    fi = files.ElementAt(Program.r.Next(files.Count));
                    break;

            }

            if (!FFmpeg.CheckFile(fi.FullName)) return;

            //подсчет времени
            int index = AdsPosition;
            if (AdsPosition > partList.Count) index = 1;

            TimeSpan ts = new TimeSpan();
            if (StartVideoIncluding)
            {
                TagLib.File f = TagLib.File.Create($"TS/{Program.project.ToLower()}_start.ts");
                ts += f.Properties.Duration;
            }


            for (int x = 0; x < index; x++)
            {
                ts += Polly.Duration(partList[x]);
            }

            int i = 0;


            //команда для формирования первого отрезка
            string start = $"{tempD}/ads1.ts";
            string command = $"-i {Program.tempD}/tempResult.mp4 -ss 0 -t {ts.TotalSeconds - 1} -y {start}".Replace(",", ".");
            do
            {
                if (i == 1)
                {
                    Log.WriteError("adsStart_creation_error.txt", $"{start}\n{ts.ToString()}\n\n");
                    return;
                }
                FFmpeg.ToFFmpeg(command);
                i++;
            }
            while (!FFmpeg.CheckFile(start));


            i = 0;
            string end = $"{tempD}/ads2.ts";
            command = $"-i { Program.tempD}/tempResult.mp4 -ss {ts.TotalSeconds - 1} -y {end}".Replace(",", ".");
            do
            {
                if (i == 1)
                {
                    Log.WriteError("adsEnd_creation_error.txt", $"{start}\n{end}\n{ts.ToString()}\n\n");
                    return;
                }
                FFmpeg.ToFFmpeg(command);
                i++;
            }
            while (!FFmpeg.CheckFile(end));

            i = 0;
            command = $"-i \"concat:{start}|Ads/{fi.Name}|{end}\" -vcodec copy -acodec copy -y {outputFilePath}";
            //ffmpeg -i "concat:Ads/temp1.ts|Ads/biglion.ts|Ads/temp3.ts" -vcodec copy -acodec copy -y Ads/out.mp4
            do
            {
                if (i == 1)
                {
                    Log.WriteError("ads_concatenation_error.txt", $"{start}\n{end}\n{ts.ToString()}\n\n");
                    return;
                }
                FFmpeg.ToFFmpeg(command);
                i++;
            }
            while (!FFmpeg.CheckFile($"{outputFilePath}"));

        }



        public static void CreatePreview(VideoInfo vi)
        {
            /*imagesD = "Images1";
            outputD = "Output1";
            previewD = "Preview1";*/
            var arrayTitle = vi.Title.Split(' ');
            string title = string.Empty;
            if (arrayTitle.Length >= 5) title = arrayTitle[0] + " " + arrayTitle[1] + " " + arrayTitle[2] + " " + arrayTitle[3] + " " + arrayTitle[4];
            else if (arrayTitle.Length >= 3) title = arrayTitle[0] + " " + arrayTitle[1] + " " + arrayTitle[2];
            else if (arrayTitle.Length == 2) title = arrayTitle[0] + " " + arrayTitle[1];
            else title = arrayTitle[0];
            title = System.Text.RegularExpressions.Regex.Replace(title, " ", "\n");

            string pictureName = vi.Parts[0].Image.Split(';')[0];
            string sourceImageFilePath = imagesD + "/" + pictureName;
            string tempImageFilePath = previewD + "/" + $"preview_{System.IO.Path.GetFileNameWithoutExtension(vi.Name)}.jpg";

        Again:
            if (AppSettings.PreviewWithText == "0") ImageProcessing(sourceImageFilePath, tempImageFilePath);
            else
            {

                string overlay = "noborder";
                switch (overlay)
                {
                    case "border":
                        var img = new MagickImage(tempImageFilePath);

                        using (var imgText = new MagickImage())
                        {

                            imgText.BackgroundColor = new MagickColor(System.Drawing.Color.Yellow);
                            imgText.Settings.FillColor = new MagickColor(System.Drawing.Color.Red);
                            imgText.Settings.FontPointsize = AppSettings.FontSize;
                            imgText.Settings.FontFamily = "Arial";
                            imgText.Settings.Font = "Arial";
                            imgText.Settings.TextGravity = Gravity.Center;
                            imgText.Settings.FontWeight = FontWeight.Bold;
                            imgText.Read("label:" + title);
                            img.Composite(imgText, Gravity.Center);
                        }
                        img.Write(tempImageFilePath);
                        break;
                    case "noborder":
                        using (var sourceImage = new MagickImage(tempImageFilePath))//создаем экземпляр magick image с использованием картинки из папки
                        {
                            var back = new MagickImage(sourceImage);
                            new Drawables()
                                .FillColor(MagickColor.FromRgb(255, 255, 00))
                                .FontPointSize(AppSettings.FontSize)
                                .Font(AppSettings.FontFamily, FontStyleType.Normal, FontWeight.Bold, FontStretch.ExtraExpanded)
                                .Text(0, 0, title)
                                .StrokeColor(System.Drawing.Color.Black)
                                .StrokeWidth(AppSettings.StrokeWidth)
                                .Gravity(Gravity.Center)
                                .Draw(back);

                            back.Write(tempImageFilePath);
                        }
                        break;
                }
            }
            if (!FFmpeg.CheckFile(tempImageFilePath)) goto Again;
            /*
            */
        }

        public static void MakeTextPart(TextPartInfo videoPart, string overlayPath)
        {
            ForegroundColor = ConsoleColor.DarkMagenta;//поставить цвет  консоли темно-пурпурным

            WriteLine($"[{DateTime.Now.ToLongTimeString()}] --> Part '{videoPart.Id}' - start processing ...");//написать текущее время в длинном формате и название части

            //нажать чтобы предовратить зависание
            Keyboard.PushTheTempo();

            string audioFilePath = audioD + '/' + videoPart.Audio;//путь к аудио

            string tempFilePath = Program.tempD + '/' + videoPart.Id + ".ts";//путь к временному видеофайлу

            try
            {
                FFmpeg.CreateVideoFromText(videoPart.FileName, videoPart.Text, audioFilePath, tempFilePath, overlayPath);//функция создать видео из аудио и картинки
            }
            catch (Exception e)
            {
                if (e.Message.Contains(" не найден"))
                {
                    Storage.Database.Delete("Records", "Audio", videoPart.Audio);
                    return;
                }
            }

        }

        public static void MakeVideoPart(VideoPartInfo videoPart, List<string> pList)
        {
            string videoPartName = System.IO.Path.ChangeExtension(videoPart.Image, null);//изменить расширение видео на пустое

            ForegroundColor = ConsoleColor.DarkMagenta;//поставить цвет  консоли темно-пурпурным

            WriteLine($"[{DateTime.Now.ToLongTimeString()}] --> Part '{videoPartName}' - start processing ...");//написать текущее время в длинном формате и название части

            //нажать чтобы предовратить зависание
            Keyboard.PushTheTempo();

            string sourceImageFilePath = Program.imagesD + '/' + videoPart.Image;//путь к начальному файлу изображения

            //creates picture with AppSettings.Width Height dimensions
            string tempImageFilePath = CreateBigPicture(sourceImageFilePath);
            if (string.IsNullOrEmpty(tempImageFilePath)) return;



            string audioFilePath = audioD + '/' + videoPart.Audio;//путь к аудио

            string tempFilePath = Program.tempD + '/' + videoPart.Id + ".ts";//путь к временному видеофайлу

            try
            {
                FFmpeg.CreateVideoFromImageAndAudio(tempImageFilePath, audioFilePath, tempFilePath, pList);//функция создать видео из аудио и картинки
            }
            catch (Exception e)
            {
                if (e.Message.Contains(" не найден"))
                {
                    Storage.Database.Delete("Records", e.Message.Contains(".mp3") ? "Audio" : "PicturesList", e.Message.Contains(".mp3") ? videoPart.Audio : videoPart.Image);
                    return;
                }
            }

            if (!FFmpeg.CheckFile(tempFilePath)) return;

            ForegroundColor = ConsoleColor.DarkGreen;//сделать текст консоли темно-зеленым

            WriteLine($"[{DateTime.Now.ToLongTimeString()}] --> Part '{videoPartName}' - processing is done!");//написать в консоль дату и время, и название файла с текстом оповещения о готовности

            //нажать чтобы предовратить зависание
            Keyboard.PushTheTempo();
        }

        private static string CreateBigPicture(string sourceImageFilePath)
        {
            string tempImageFilePath = string.Empty;
            if (AppSettings.Effects == "3" || AppSettings.Effects == "4")
            {
                int counter = 0;
                string path = tempD + "/" + System.IO.Path.GetFileName(sourceImageFilePath);
                if (!File.Exists(path))
                {
                Again:
                    MagickImage image = null;
                    try
                    {
                        image = new MagickImage(sourceImageFilePath);
                    }
                    catch (Exception e)
                    {
                        int result;
                        if (e.Message.Contains("Not a JPEG file"))
                        {
                            counter++;
                            File.Delete(sourceImageFilePath);
                            result = Check.DownloadOnce(System.IO.Path.GetFileName(sourceImageFilePath));
                            if (counter > 5) return string.Empty;
                            goto Again;
                        }
                        if (e.Message.Contains("unable to open")) return string.Empty;

                        result = Check.DownloadOnce(System.IO.Path.GetFileName(sourceImageFilePath));
                        if (counter > 5) return string.Empty;
                        goto Again;
                    }
                    try
                    {
                        if (image == null) goto Again;
                        image.Resize(0, AppSettings.VideoScaleHeight);
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Contains("Ссылка на объект")) goto Again;
                    }
                    CheckWidth(image, path);
                    if (!FFmpeg.CheckFile(path)) image.Write(path);
                    image.Dispose();
                    tempImageFilePath = path;
                }
                else tempImageFilePath = path;
            }
            else if (AppSettings.Effects == "1" || AppSettings.Effects == "2" || AppSettings.Effects == "5")
            {
                tempImageFilePath = tempD + '/' + System.IO.Path.GetFileName(sourceImageFilePath);//путь к временному файлу изображения
                if (!File.Exists(tempImageFilePath))//если временного файла изображения нет, то
                    ImageProcessing(sourceImageFilePath, tempImageFilePath);//создать его там
            }

            return tempImageFilePath;
        }
    }
}


