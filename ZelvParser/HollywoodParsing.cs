﻿using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace HollywoodParser
{
    using static Tell;

    public static class Parsing
    {
        static int i = 1;
        static int a = 1;
        static int b = 1;
        static bool add = true;

        //список нод sitemap из файла sitemap.xml

        //список полученных из базы данных ссылок
        //список полученных ссылок из smcatalog<number>.xml для сверки с checkdb
        static List<string[]> common = new List<string[]>();

        //загрузка из базы данных пустых строк заданной категории
        public static void Upload(string data)
        {
            List<string> list = Storage.Database.GetBlank("Records", data, string.Empty);
            File.WriteAllLines(Path.Combine(Program.Path, "null-parsed.txt"), list);
        }

        //исключить из списка всех ссылок те, которые уже спаршены
        public static List<string> ExceptLists()
        {

            //список ссылок из таблицы Catalog
            List<string> list = Storage.Database.GetOnlyLinkTable("Records");
            //сортировка списка
            

            List<string> books = Storage.Database.GetOnlyLinkTable("Current");
            
            List<string> temp = new List<string>();
            foreach (string res in books) if (!list.Contains(res)) temp.Add(res);
            return temp;
        }

        //парсинг статей
        public static void Start(List<string> list)
        {
            Console.WriteLine("\nPage parsing\n");

            switch (AppSettings.Scraping)
                {
                    case "M":
                        Console.WriteLine("Multithreaded parsing\n");
                        try
                        {
                            Parallel.ForEach(list, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, j =>
                            {
                                ParseLink(j);
                            });
                        }
                        catch (Exception e)
                        {
                            Log.SendAndText("errorParseLink.txt", e);
                        }
                        break;
                    case "O":
                        Console.WriteLine("One thread parsing\n");
                        foreach(string j in list)
                        {
                            ParseLink(j);
                        }
                        break;
                }
            
        }

        public static void ParseLink(string url)
        {

            Random r = new Random();

            if (AppSettings.Sleep) System.Threading.Thread.Sleep(r.Next(AppSettings.SleepFrom, AppSettings.SleepTo));

            ColoredMessage($"{b} - Поток {url}\r", ConsoleColor.Blue);
            
            string data = GetText(url);

            //новый экземпляр книги.
            Issue issue = new Issue();
            

            //

            //новый экземпляр парсера HTML
            var parser = new AngleSharp.Parser.Html.HtmlParser();

            //парсинг полученных данных через AngleSharp
            AngleSharp.Dom.Html.IHtmlDocument document = parser.Parse(data);

            string iTags = string.Empty;

            string iCategory = string.Empty;
            try
            {
                List<string> l_tags = new List<string>();
                List<string> l_categories = new List<string>();
                List<string> categoriesArray = new List<string>() { "/celebrity-gossip/", "/celebrity-fashion/", "/celebrity-weddings/", "/celebrity-weddings/", "/celebrity-babies/", "/celebrity-hairstyles/", "/pets/", "/news/", "/sports/", "/movies/", "/tv/" };
                var tags = document.GetElementById("post").GetElementsByTagName("small")[1]??null;
                if(tags!=null)
                { 
                bool cat = false;
                foreach (var x in tags.Children)

                    if (x.GetAttribute("href").Contains("/categories/"))
                    {
                        if (cat == false)
                        {
                            string textCategory = x.GetAttribute("href").Replace("/categories", "");
                            if (categoriesArray.Contains(textCategory)) l_categories.Add(textCategory.Replace("'",""));
                            else l_tags.Add(x.TextContent.Replace("'", ""));
                            cat = true;
                        }
                        else l_tags.Add(x.TextContent.Replace("'", ""));
                    }
                    else l_tags.Add(x.TextContent.Replace("'", ""));
                
                }
                else
                {
                    tags = document.GetElementsByClassName("keywords")[0]??null;
                    if (tags != null)
                    {
                        foreach (var x in tags.Children)
                        { 
                            if (x.GetAttribute("href").Contains("/categories/")) l_categories.Add(x.TextContent.Replace("'", ""));
                            else l_tags.Add(x.TextContent.Replace("'", ""));
                        }
                    }
                    else
                    {
                        tags = document.GetElementsByClassName("dl-horizontal")[0];
                        foreach (var x in tags.Children) if (x.TextContent.Contains("Tags")) l_tags.Add(x.Children[1].TextContent.Replace("'", ""));
                    }
                }
                if (l_categories.Count > 0) iCategory = string.Join(", ", l_categories); else iCategory = "";
                if (l_tags.Count > 0) iTags = string.Join(", ", l_tags); else iTags = "";
               
            }
            catch
            {
                
            }


            string iTitle = string.Empty;

            
            string iDate = DateTime.Now.ToShortDateString();

            issue.Category = iCategory;
            //ссылка на товар
            issue.Link = url;
            issue.Date = iDate;

            //парсинг заголовка
            var title = document.GetElementsByTagName("h1");
            if (title == null) return;
            iTitle = title[0].TextContent.ToUpper().Replace("'","");
            if (iTitle.Length > AppSettings.TitleMaxLength) return;
            /*string[] textExceptions = { "'", " (ВИДЕО)", ". ВИДЕО", "(ФОТО)", ". ФОТО", "- ФОТО", "- ВИДЕО","— ВИДЕО", "— ФОТО","\"ВИДЕО\"", "\"ФОТО\"" };
            foreach (string e in textExceptions)
            {
                Regex reg2 = new Regex(e);
                string str = reg2.Match(iTitle).Value;// Regex.Replace(iTitle, e);//iTitle.Replace(e, "");
                issue.Title = str;
            }*/
            issue.Title = iTitle.Trim();

            //получить блок статьи с тегами и изображениями
            var newsitem_text = document.GetElementsByClassName("body")[0];

            //получить блоки p
            var text_blocks = newsitem_text.GetElementsByTagName("p").Select(x=>x.TextContent).ToArray();
            string text = string.Join(" ", text_blocks).Replace("\n\t","").Trim().Replace("'", "");
            if (text.Length == 0) return;


            //записать блоки в текст.
            text = text.Replace("'", "");
            issue.Text = text;
            if (text.Length > AppSettings.TextMaxLength) return;
            else if (text.Length < AppSettings.TextMinLength) return;
            issue.TextLength = text.Length;

            //список для картинок
            List<Picture> dict = new List<Picture>();

            //поиск картинок среди текста
            var blocks = newsitem_text.GetElementsByTagName("div");
            foreach (var tag in blocks)
            {
                if (tag.ClassName == "thumbnail")
                {
                    ParsePictures(tag, ref dict);
                }
            }

            //чтобы не вызвать ошибку в дальнейшем -лучше сразу выйти из цикла, если не нашел в статье фотографии
            if (dict.Count == 0) return;

            //блок разделения текста на части
            List<string> parts = Text.SplitterDot(text);
            //сериализовать полученные части
            issue.TextParts = JsonConvert.SerializeObject(parts);


            //блок получения длины частей
            int[] partsLength = new int[parts.Count];
            for (int i = 0; i < parts.Count; i++) partsLength[i] = parts[i].Length;
            issue.TextPartsLength = JsonConvert.SerializeObject(partsLength);

            Program.c += parts.Count;

            //блок получения количества частей
            issue.TextPartsCount = parts.Count;

            //список фотографий, согласно тз
            List<string> photos = new List<string>();
            for (int i = 0; i < dict.Count; i++) photos.Add(dict[i].Name);

            //получить количество фото
            List<string> partsPhotos = Text.PicturesCount(parts.Count, photos);

            //сериализовать полученные значения
            issue.Pictures = JsonConvert.SerializeObject(dict);

            //список с названия фото для монстра 
            issue.PicturesList = string.Join(";", partsPhotos);

            //получение количества фотографий для данной статьи
            issue.PicturesCount = dict.Count;

            issue.Tags = iTags;


            /* загрузка картинок реализована отдельным блоком
            foreach (Picture row in dict)
            {
                //получить хэш строку названия картинки.
                string photo = row.Name;

                //загрузка файла
                string result = GetData.Download(url: row.Link, downloadMode: "File", filename: photo);
            }*/

            //записать в базу, т.к. все необходимые части есть
            Storage.Database.WriteData(issue);


            ColoredMessage($"{a}/{b} - Добавлена статья {iTitle}\n", ConsoleColor.DarkRed);
            a++;
            b++;
            GC.Collect();

        }


        private static string ParseTextIssue(string textContent)
        {
            string text = textContent.Replace("\n", "").Replace("\t", "");
            text = WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"<script.*?</script>", "");
            text = Regex.Replace(text, @"<.*?>", "");
            //&lt;…&gt;
            text = text.Replace("&lt;…&gt;", "");
            text = text.Replace(".", ". ");
            text = text.Replace("&nbsp;", "");
            text = text.Replace(",", ", ");
            text = text.Replace("  ", " ");
            text = text.Replace("'", "");
            //text = text.Replace("Источник.", "");
            text = text.Trim();
            

            return text;
            //string text = iText.Replace("'", "").Replace("Источник.", "").Replace(".", ". ").Replace("  ", " ").Trim();
        }
        #region ParsePictures
        static void ParsePictures(AngleSharp.Dom.IElement tag, ref List<Picture> dict)
        {   
                if(tag.Children[0].TagName=="A")
                {
                    var aTag = tag.GetElementsByTagName("a")[0].GetAttribute("href");
                    string fullLink = AppSettings.SitemapLink + aTag;
                if (!fullLink.Contains("/gallery/")) return;
                    string data = GetText(fullLink);

                    //новый экземпляр парсера HTML
                    var parser = new AngleSharp.Parser.Html.HtmlParser();

                    //парсинг полученных данных через AngleSharp
                    AngleSharp.Dom.Html.IHtmlDocument document = parser.Parse(data);


                    string link = document.GetElementsByClassName("thumbnail")[0].Children[0].Children[0].GetElementsByTagName("img")[0].GetAttribute("src");
                    string name = GetHashString(link)+".jpg";


                    //отдать ссылку в dict
                    dict.Add(new Picture(name, link));
                }
            
        }
        #endregion
        static bool IsNull(string parsed, string url)
        {
            if (string.IsNullOrEmpty(parsed))
            {
                System.IO.File.AppendAllText("null-parsed.txt", url + "\n");
                return true;
            }
            else return false;
        }

        private static string GetText(string url)
        {
            string data = string.Empty;

            //получение строки данных
            data = GetData.Download(url: url);

            //если ответ не включает данную фразу, то задейстовать браузер
            if (!data.Contains("<!DOCTYPE html")) data = Toggle.Navigate(url);

            return data;
        }

        static string GetHashString(string s)
        {
            //переводим строку в байт-массим  
            byte[] bytes = Encoding.Unicode.GetBytes(s);

            //создаем объект для получения средст шифрования  
            MD5CryptoServiceProvider CSP =
                new MD5CryptoServiceProvider();

            //вычисляем хеш-представление в байтах  
            byte[] byteHash = CSP.ComputeHash(bytes);

            string hash = string.Empty;

            //формируем одну цельную строку из массива  
            foreach (byte b in byteHash)
                hash += string.Format($"{b:x2}");

            return hash;
        }

        //Функция отвечающая за парсинг sitemap.xml - в результате выдающая ссылку на основной xml, в котором хранятся ссылки на статьи
        static void FirstTime()
        {
            
                
                string url = AppSettings.SitemapLink;
                try
                {
                    for(int page = 1;page<AppSettings.Days+1;page++)
                    {
                        string pageurl = url + $"/page-{page}.html";
                        SingleCatalog(pageurl);
                    }
                }
                catch(Exception e)
                {
                    Log.WriteError("error.txt", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "\t" + "cant parse page from site");
                    Console.WriteLine("Cant parse page of site");
                    Console.ReadKey();
                }
            
        }

        //парсинг xml с сайта
        public static void parseXmls()
        {
            try
            {
                //получить данные из бд
                List<string> checkdb = Storage.Database.GetLinkTable("Catalog");
                
                //если строк больше 0, значит парсинг был и нужно сравнение 2 списков.
                if (checkdb.Count > 0)
                {
                    Console.WriteLine("\n New issues check \n");
                    //не добавлять в базу
                    add = false;

                    FirstTime();

                    //сравнение двух списков
                    List<string> result = ExceptArrays(checkdb, common);
                    //добавить в базу, если будет совпадение
                    if (result.Count > 0) Storage.Database.WriteDataFull(result, "Current");
                }
                else
                {
                    Console.WriteLine("\nFirst time parsing \n");
                    //заполнение с нуля базы данных
                    FirstTime();
                }

            }
            catch (Exception e)
            {
                Log.WriteError("error.txt", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "\t" + e.Message + "\n\t" + e.StackTrace + "\n\t" + e.Source + "\n");
            }
        }

        static List<string> ExceptArrays(List<string> one, List<string[]> another)
        {
            List<string> result = new List<string>();
            List<string> val = new List<string>();
            var tempAnother = another.Select(x => x[0]);
            foreach (var x in tempAnother)
            {
                val.Add(x);
            }

            foreach (string res in val) if (!one.Contains(res)) result.Add(res);
            if (result.Count == 0) result = val;
            return result;
        }

        static void SingleCatalog(string url)
        {
            List<string[]> urls = new List<string[]>();
            //получение одного sm catalog xml

            if (string.IsNullOrEmpty(url)) return;

            

            string data = GetData.Download(url);

            //новый экземпляр парсера HTML
            var parser = new AngleSharp.Parser.Html.HtmlParser();

            //парсинг полученных данных через AngleSharp
            AngleSharp.Dom.Html.IHtmlDocument document = parser.Parse(data);

            var x = document.GetElementById("infinite").GetElementsByClassName("col-sm-6");

            DateTime dt = DateTime.Now;

            int g = 0;
            //перебор элементов списка статей.
            foreach (var element in x)
            {
                //пока пустые строки даты и урла
                string loc = AppSettings.SitemapLink+element.GetElementsByTagName("article")[0].GetElementsByTagName("a")[0].GetAttribute("href");
                string tempDate = element.GetElementsByTagName("article")[0].GetElementsByClassName("details clearfix")[0].GetElementsByTagName("time")[0].GetAttribute("datetime");
                string date = DateTime.Parse(tempDate).ToShortDateString();
                switch (AppSettings.Parse)
                        {
                            case 0:
                                    Addition(loc, date, ref urls, ref common);
                                    g++;
                                
                                break;

                            //просто добавление статьи
                            case 1:
                                if (g < AppSettings.RecordsCount)
                                {
                                    Addition(loc, date, ref urls, ref common);
                                    g++;
                                }

                                break;

                            //получение статьи со сравнением текущей даты
                            case 2:
                                var difference = dt - DateTime.Parse(tempDate);
                                if (difference.TotalDays <= AppSettings.Days) Addition(loc, date, ref urls, ref common);
                                break;
                            //получение нужного количества статей, исходя из настройки
                            case 3:
                                var difference1 = dt - DateTime.Parse(date);
                                if (difference1.TotalDays <= AppSettings.Days)
                                    if (g < AppSettings.RecordsCount)
                                    {
                                        Addition(loc, date, ref urls, ref common);
                                        g++;
                                    }
                                break;
                            //получение статей в пределах указанных дат
                            case 4:
                                if (DateTime.Parse(date) >= AppSettings.DateFrom && DateTime.Parse(date) <= AppSettings.DateTo)
                                {
                                    if (g < AppSettings.RecordsCount)
                                    {
                                        Addition(loc, date, ref urls, ref common);
                                        g++;
                                    }
                                }
                                break;
                        }
                   
            }
            //если trueзаписать в таблицу Catalog
            //    if (add == true) 
            if (urls.Count > 0) Storage.Database.WriteDataFullWithDate(urls, "Current");
            GC.Collect();
        }



        /*//перебор нод в одном xml файле
                foreach (XElement element in x)
                {

                    string date = string.Empty;
                    string loc = string.Empty;

                    foreach (var m in element.Elements())
                    {

                        if (m.Name.LocalName == "loc") loc = m.Value;
                        if (m.Name.LocalName == "lastmod")
                        {
                            date = m.Value;

                            Addition(loc, date, ref urls, ref common);

                        }
                    }
                }*/
        static void Addition(string loc, string date, ref List<string[]> urls, ref List<string[]> common)
        {
            if (!loc.Contains("/partnery/"))
            {
                //если false - записать в обычный список, для последующего сравнения. 
                if (add == false) common.Add(new[] { loc, date });
                else urls.Add(new[] { loc, date });
                Console.WriteLine($">> {i} \r");
                i++;
            }
        }
    }
}


