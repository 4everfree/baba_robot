using System.Text;
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

namespace ZelvParser
{
    using static Tell;

    public static class Parsing
    {
        static int i = 1;
        static int a = 1;
        static int b = 1;
        static int full = 0;
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
            List<string> list = Storage.Database.GetLinkTable("Records");
            //сортировка списка
            list.Sort();

            List<string> books = Storage.Database.GetLinkTable("Current");
            books.Sort();

            List<string> temp = new List<string>();
            foreach (string res in books) if (!list.Contains(res)) temp.Add(res);
            return temp;
        }

        public static List<string> LabirintExceptLists()
        {

            //список ссылок из таблицы Current
            List<string> current = Storage.Database.GetLinkTable("Catalog");
            //сортировка списка
            current.Sort();

            //список ссылок из таблицы bad
            List<string> bad = Storage.Database.GetLinkTable("Bad");
            bad.Sort();

            //разница между двумя списками Current и bad
            List<string> difference = current.Except(bad).ToList();

            List<string> records = Storage.Database.GetLinkTable("Records");
            records.Sort();

            //разница между каталогом и тем что уже есть в records.
            //результат - ссылки, которых нет ни в records, ни в bad
            difference = difference.Except(records).ToList();


            #region old exception
            /*
            List<string> temp = new List<string>();
            foreach (string res in books) if (!list.Contains(res)) temp.Add(res);

            List<string> afterbad = new List<string>();
            foreach (string res in temp) if (!bad.Contains(res)) afterbad.Add(res);
            */
            #endregion

            return difference;
        }


        //парсинг статей
        public static void Start(List<string> list)
        {
            Console.WriteLine("\nПарсинг страниц\n");

            switch (AppSettings.Scraping)
                {
                    case "M":
                        Console.WriteLine("Многопоточный парсинг\n");
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
                        Console.WriteLine("Однопоточный парсинг\n");
                        foreach (string j in list)
                        {
                            ParseLink(j);
                        }
                        break;
                }
            
        }
        public static void ZelvParseLink(string url,string data)
        {
            //новый экземпляр книги.
            Issue issue = new Issue();
            string iCategory = string.Empty;
            try
            {
                iCategory = url.Split('/')[3];
            }
            catch
            {
                iCategory = "auto";
            }
            string iTitle = string.Empty;

            string iTags = string.Empty;
            string iDate = DateTime.Now.ToShortDateString();

            //

            //новый экземпляр парсера HTML
            var parser = new AngleSharp.Parser.Html.HtmlParser();

            //парсинг полученных данных через AngleSharp
            AngleSharp.Dom.Html.IHtmlDocument document = parser.Parse(data);


            issue.Category = iCategory;
            //ссылка на товар
            issue.Link = url;
            issue.Date = iDate;

            //парсинг заголовка
            var title = document.GetElementsByTagName("h1");
            if (title == null) return;
            iTitle = title[0].TextContent.ToUpper();
            if (iTitle.Length > AppSettings.TitleMaxLength) return;
            iTitle = iTitle.Replace("'", "");
            string[] textExceptions = { " (ВИДЕО)", ". ВИДЕО", "(ФОТО)", ". ФОТО", "- ФОТО", "- ВИДЕО", "— ВИДЕО", "— ФОТО", "\"ВИДЕО\"", "\"ФОТО\"" };
            for (int i = 0; i < textExceptions.Count(); i++)
            {
                string e = textExceptions[i];
                if (iTitle.Contains(e))
                {
                    iTitle = iTitle.Replace(e, "");
                    issue.Title = iTitle;
                    break;
                }
            }
            issue.Title = iTitle.Trim();

            //парсинг фото и текста.
            var newsitem_text = document.GetElementsByClassName("newsitem_text");
            if (newsitem_text.Length == 0)
            {
                Tell.ColoredMessage($"Текст к статье {iTitle} не найден!!!!", c: ConsoleColor.Yellow);
                return;
            }
            var block = newsitem_text[0];
            var text_blocks = block.GetElementsByClassName("masha_index");

            List<Picture> dict = new List<Picture>();


            //поиск картинок среди текста
            foreach (var tag in block.Children)
            {
                ParsePictures(tag, ref dict);
            }

            //чтобы не вызвать ошибку в дальнейшем -лучше сразу выйти из цикла, если не нашел в статье фотографии
            if (dict.Count == 0)
            {
                Tell.ColoredMessage($"Не нашел картинки к статье {iTitle}!!!!",c:ConsoleColor.Yellow);
                return;
            }

            string text = ParseTextIssue(block.OuterHtml);
            issue.Text = text;
            if (text.Length > AppSettings.TextMaxLength)
            {
                Tell.ColoredMessage($"Длина текста больше {AppSettings.TextMaxLength} - {iTitle}!!!!", c: ConsoleColor.Yellow);
                return;
            }
            else if (text.Length < AppSettings.TextMinLength)
            {
                Tell.ColoredMessage($"Длина текста меньше {AppSettings.TextMinLength} - {iTitle}!!!!", c: ConsoleColor.Yellow);
                return;
            }
            issue.TextLength = text.Length;


            //блок разделения текста на части
            Dictionary<string,string> parts = Text.SplitterDot(text); //(text, dict.Count);
            //сериализовать полученные части
            issue.TextParts = JsonConvert.SerializeObject(parts);


            //блок получения длины частей
            int[] partsLength = new int[parts.Count];
            for (int i = 0; i < parts.Count; i++) partsLength[i] = parts.ElementAt(i).Value.Length;
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

            issue.Tags = Text.Frequency(text).Replace("(", "").Replace(")", "");



            Storage.Database.WriteData(issue);


            ColoredMessage($"{a}/{b} - Added an issue {url}\n", ConsoleColor.DarkRed);
            a++;

            GC.Collect();

        }
        public static void ParseLink(string url)
        {
            Random r = new Random();
            b++;
            if (AppSettings.Sleep) System.Threading.Thread.Sleep(r.Next(AppSettings.SleepFrom, AppSettings.SleepTo));

            ColoredMessage($"{b} - Поток {url}\r", ConsoleColor.Blue);

            string data = string.Empty;
            //получение строки данных
            data = GetData.Download(url: url);

            //если ответ не включает данную фразу, то задейстовать браузер
            if (!data.Contains("<!DOCTYPE html")) data = Toggle.Navigate(url);


            switch (Program.project)
            {
                case "Zelv":
                    ZelvParseLink(url,data);
                    break;
                case "Hollywood":
                    HollywoodParseLink(url,data);
                    break;
                case "Deadline":
                    DeadlineParseLink(url,data);
                    break;
                case "Labirint":
                    LabirintParseLink(url, data);
                    break;            }


        }

        static string RegexSource(string source, string pattern)
        {
            string fullPattern = $"(?<=<{pattern}>).*?(?=</{pattern}>)";
            Regex r = new Regex(fullPattern);
            bool result = r.IsMatch(source);

            if(result)
            {
                return r.Match(source).Value;
            }
            return string.Empty;
        }

        public static bool ParseTextFromFile(string source)
        {
            string header = string.Empty;
            string body = string.Empty;

            string[] patterns = { @"header", @"body" };
            foreach(string pattern in patterns)
            {
                switch(pattern)
                {
                    case "header" :
                       header = RegexSource(source, pattern);
                       break;
                    case "body":
                        body = RegexSource(source, pattern);
                        break;
                }               
            }

            if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(body)) return false;

            //новая статья
            Issue issue = new Issue();


            if (AppSettings.DirtyWords)
            {
                issue.Text = Text.DirtyWords(body, true);
                issue.Title = Text.DirtyWords(header,false);
            }
            //добавить в нее текст
            else
            {
                issue.Text = body;
                issue.Title = header;
            }
            //блок разделения текста на части
            Dictionary<string, string> parts = Text.SplitterDot(body); //(text, dict.Count);
            //сериализовать полученные части
            issue.TextParts = JsonConvert.SerializeObject(parts);

            issue.Tags = Text.Frequency(body).Replace("(", "").Replace(")", "");

            

            issue.Link = header;

            Storage.Database.WriteData(issue);

            return true;
        }

        private static void LabirintParseLink(string url, string data)
        {
            Random r = new Random();
            //новый экземпляр парсера HTML
            var parser = new AngleSharp.Parser.Html.HtmlParser();

            //парсинг полученных данных через AngleSharp
            AngleSharp.Dom.Html.IHtmlDocument document = parser.Parse(data);

            Book issue = new Book();


            string iCategory = string.Empty;
            string iCategoryName = string.Empty;

            //Жанр
            string bGenre = string.Empty;
            try
            {
                var x = document.GetElementById("thermometer-books");
                bGenre = x.Children[1].Children[0].FirstChild.TextContent;
                iCategory = x.Children[1].Children[0].GetAttribute("href").Replace("/genres", "");
                if (iCategory == "/2137/") { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
                if (iCategory == "/965/") { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
                if (IsNull(bGenre, url)) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
            }
            catch
            {
                bGenre = string.Empty;
            }
            finally
            {
                issue.Category = iCategory;
                issue.CategoryName = bGenre;
            }

            //название товара
            var x3 = document.GetElementById("product-title");

            if (document.Title == "Ошибка 404. Интернет-магазин Лабиринт.") { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
            string bTitle = x3.Children[0].InnerHtml;

            if (IsNull(bTitle, url)) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }



            //удаление апострофов
            if (bTitle.Contains("'")) bTitle = bTitle.Replace("'", "");

            string pre = "Обзор книги - ";
            string post = ", отзывы";

            int fulltitle = pre.Length + bTitle.Length + post.Length;
            int pretitle = pre.Length + bTitle.Length;
            int posttitle = bTitle.Length + post.Length;
            if (fulltitle < AppSettings.TitleMaxLength) bTitle = pre + bTitle + post;
            else if (pretitle < AppSettings.TitleMaxLength) bTitle = pre + bTitle;
            else if (posttitle < AppSettings.TitleMaxLength) bTitle = bTitle + post;
            if (bTitle.Length > AppSettings.TitleMaxLength) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
            issue.Title = bTitle;


            //Изображение товара
            var x2 = document.GetElementById("product-image").FirstElementChild;
            string bPhotoLink = x2.GetAttribute("data-src") ?? x2.GetAttribute("src") ?? x2.Children[0].GetAttribute("src");// ?? x2.FirstChild;

            if (IsNull(bPhotoLink, url)) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }

            List<Picture> dict = new List<Picture>(1);
            Picture picture = new Picture(GetHashString(bTitle) + ".jpg", bPhotoLink);
            dict.Add(picture);
            issue.Pictures = JsonConvert.SerializeObject(dict);

            issue.PicturesCount = 1;

            issue.PicturesList = GetHashString(bTitle) + ".jpg";


            // цена товара
            string bPrice = string.Empty;
            try
            {
                bPrice = document.GetElementsByClassName("buying-price-val-number")[0].TextContent;
            }
            catch
            {
                try
                {
                    bPrice = document.GetElementsByClassName("buying-pricenew-val-number")[0].TextContent;
                }
                catch
                {
                    Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return;
                }
            }

            if (IsNull(bPrice, url))
            {
                Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return;
            }

            if (int.Parse(bPrice) < 300) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
            //issue.Price = bPrice;


            //Наличие
            var x1 = document.GetElementsByClassName("prodtitle-availibility")[0];
            string bInstock = x1.Children[0].TextContent.Trim();
            switch (bInstock)
            {
                default: Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; ;
                case "Предзаказ":
                case "В наличии":
                case "На складе (ограниченное количество)":
                case "На складе": break;

            }

            if (IsNull(bInstock, url)) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }

            //issue.InStock = bInstock;

            //оценка товара
            string star = document.GetElementById("product-rating-marks-label").TextContent;
            string stars = Regex.Match(star, @"(?<=:).*?(?=\))").Value.Trim();
            if (IsNull(star, url)) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
            if (int.Parse(stars) == 0) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }


            //аннотация к товару
            string bAnnot = string.Empty;
            var x5 = document.GetElementById("fullannotation");
            if (x5 != null)
            {
                bAnnot = x5.FirstElementChild.TextContent;
            }
            else
            {
                var x4 = document.GetElementById("product-about");
                if (x4.HasChildNodes) bAnnot = x4.LastChild.TextContent;
                if (string.IsNullOrEmpty(bAnnot)) bAnnot = x4.ChildNodes[1].TextContent.Replace("\n", string.Empty).Replace("\t", string.Empty);
            }

            if (IsNull(bAnnot, url)) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }



            if (bAnnot.Length < AppSettings.TextMinLength) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }
            if (bAnnot.Length > AppSettings.TextMaxLength) { Storage.Database.ExecuteInsert($"INSERT OR IGNORE INTO Bad ('link') values('{url}')"); return; }

            bAnnot = ParseTextIssue(bAnnot);

            issue.Text = bAnnot;






            string iTags = string.Empty;
            string iDate = DateTime.Now.ToShortDateString();



            //ссылка на товар
            issue.Link = url + AppSettings.PartnerLink;
            issue.Date = iDate;
            issue.TextLength = bAnnot.Length;


            //блок разделения текста на части
            List<string> parts = Text.Splitter(bAnnot, dict.Count);
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


            issue.Tags = Text.Frequency(bAnnot).Replace("(", "").Replace(")", "");


            /* загрузка реализована отдельным блоком
            foreach (Picture row in dict)
            {
                //получить хэш строку названия картинки.
                string photo = row.Name;

                //загрузка файла
                string result = GetData.Download(url: row.Link, downloadMode: "File", filename: photo);
            }
            */
            //записать в базу, т.к. все необходимые части есть
            Storage.Database.WriteData(issue);


            ColoredMessage($"{a}/{b} - Добавлена книга {bTitle}\n", ConsoleColor.DarkRed);
            a++;

            GC.Collect();
        }

        private static void DeadlineParseLink(string url,string data)
        {

            //новый экземпляр книги.
            Issue issue = new Issue();
            List<Picture> dict = new List<Picture>();

            //

            //новый экземпляр парсера HTML
            var parser = new AngleSharp.Parser.Html.HtmlParser();

            //парсинг полученных данных через AngleSharp
            AngleSharp.Dom.Html.IHtmlDocument document = parser.Parse(data);

            var metaEl = document.GetElementsByTagName("head")[0].GetElementsByTagName("meta");

            string iTitle = string.Empty;

            string iTags = string.Empty;

            string iCategory = string.Empty;

            string iDate = DateTime.Now.ToShortDateString();

            
            //ссылка на товар
            issue.Link = url;
            issue.Date = iDate;

            string text = string.Empty;
            List<string> tagsNames = new List<string>();
            List<string> categoryNames = new List<string>();

            foreach (var x in metaEl)
            {

                if (x.ClassName == "swiftype")
                {
                    if (x.GetAttribute("name") == "title") iTitle = WebUtility.HtmlDecode(x.GetAttribute("content")).Replace("'", "").Replace("’", "").ToUpper();
                    if (x.GetAttribute("name") == "tags")
                    {
                        tagsNames.Add(x.GetAttribute("content"));
                    }
                    if (x.GetAttribute("name") == "image")
                    {
                        string link = Regex.Match(x.GetAttribute("content"), @".*(?=\?)").Value;
                        dict.Add(new Picture(GetHashString(x.GetAttribute("content")) + ".jpg", link));
                    }
                    if (x.GetAttribute("name") == "body") text = WebUtility.HtmlDecode(x.GetAttribute("content")).Replace("\n", "").Replace("'", "").Replace("’", "");
                    if (x.GetAttribute("name") == "topics")
                    {
                        categoryNames.Add(x.GetAttribute("content"));
                    }
                }

                issue.Tags = string.Join(", ", tagsNames).Replace("'", "").Replace("’", "");
            }

            if (iTitle == null) return;
            if (iTitle.Length > AppSettings.TitleMaxLength) return;

            iTitle = Text.DirtyWords(iTitle,false);

            issue.Title = iTitle.Trim();
            if (categoryNames.Count > 0) issue.Category = string.Format("/" + categoryNames[Program.r.Next(categoryNames.Count)].ToLower() + "/");
            else issue.Category = "";
            //получить блок статьи с тегами и изображениями
            /*var newsitem_text = document.GetElementsByClassName("post-content")[0];

            //получить блоки p
            var text_blocks = newsitem_text.GetElementsByTagName("p").Select(x => x.TextContent).ToArray();
            string text = string.Join(" ", text_blocks).Replace("\n\t", "").Trim().Replace("'", "");*/
            if (text.Length == 0) return;


            //записать блоки в текст.
            text = text.Replace("'", "").Replace(" \t   ", "").Replace("  ", "").Replace(" \t", "").Replace("\"", "").Replace("’", "");
            issue.Text = Text.DirtyWords(text,true);
            if (text.Length > AppSettings.TextMaxLength) return;
            else if (text.Length < AppSettings.TextMinLength) return;
            issue.TextLength = text.Length;

            //чтобы не вызвать ошибку в дальнейшем -лучше сразу выйти из цикла, если не нашел в статье фотографии
            if (dict.Count == 0) return;

            //блок разделения текста на части
            Dictionary<string,string> parts = Text.SplitterDot(text);
            //сериализовать полученные части
            issue.TextParts = JsonConvert.SerializeObject(parts);


            //блок получения длины частей
            int[] partsLength = new int[parts.Count];
            for (int i = 0; i < parts.Count; i++) partsLength[i] = parts.ElementAt(i).Value.Length;
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



            //записать в базу, т.к. все необходимые части есть
            Storage.Database.WriteData(issue);


            ColoredMessage($"{a}/{b} - Добавлена статья {iTitle}\n", ConsoleColor.DarkRed);
            a++;
            b++;
            GC.Collect();
        }

        private static void HollywoodParseLink(string url,string data)
        {
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
                var tags = document.GetElementById("post").GetElementsByTagName("small")[1] ?? null;
                if (tags != null)
                {
                    bool cat = false;
                    foreach (var x in tags.Children)

                        if (x.GetAttribute("href").Contains("/categories/"))
                        {
                            if (cat == false)
                            {
                                string textCategory = x.GetAttribute("href").Replace("/categories", "");
                                if (categoriesArray.Contains(textCategory)) l_categories.Add(textCategory.Replace("'", ""));
                                else l_tags.Add(x.TextContent.Replace("'", ""));
                                cat = true;
                            }
                            else l_tags.Add(x.TextContent.Replace("'", ""));
                        }
                        else l_tags.Add(x.TextContent.Replace("'", ""));

                }
                else
                {
                    tags = document.GetElementsByClassName("keywords")[0] ?? null;
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
            iTitle = title[0].TextContent.ToUpper().Replace("'", "");
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
            var text_blocks = newsitem_text.GetElementsByTagName("p").Select(x => x.TextContent).ToArray();
            string text = string.Join(" ", text_blocks).Replace("\n\t", "").Trim().Replace("'", "");
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
                    HollywoodParsePictures(tag, ref dict);
                }
            }

            //чтобы не вызвать ошибку в дальнейшем -лучше сразу выйти из цикла, если не нашел в статье фотографии
            if (dict.Count == 0) return;

            //блок разделения текста на части
            Dictionary<string,string> parts = Text.SplitterDot(text);
            //сериализовать полученные части
            issue.TextParts = JsonConvert.SerializeObject(parts);


            //блок получения длины частей
            int[] partsLength = new int[parts.Count];
            for (int i = 0; i < parts.Count; i++) partsLength[i] = parts.ElementAt(i).Value.Length;
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


        /**/

        private static string ParseTextIssue(string textContent)
        {
            string text = textContent.Replace("\n", "").Replace("\t", "");
            text = WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"<yatag.*?</yatag>", "");
            text = Regex.Replace(text, @"<script.*?</script>", "");
            text = Regex.Replace(text, @"<.*?>", " ");
            //&lt;…&gt;
            text = text.Replace("&lt;…&gt;", "");
            text = text.Replace(".", ". ");
            text = text.Replace("  ", " ");
            text = text.Replace("&nbsp;", "");
            text = text.Replace(",", ", ");
            text = text.Replace("  ", " ");
            text = text.Replace("'", "");
            text = text.Replace("Источник.", "");
            text = text.Replace("%", "\\%");
            text = text.Trim();
            

            return text;
            //string text = iText.Replace("'", "").Replace("Источник.", "").Replace(".", ". ").Replace("  ", " ").Trim();
        }

        #region HollywoodParsePictures
        static void HollywoodParsePictures(AngleSharp.Dom.IElement tag, ref List<Picture> dict)
        {
            if (tag.Children[0].TagName == "A")
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
                string name = GetHashString(link) + ".jpg";


                //отдать ссылку в dict
                dict.Add(new Picture(name, link));
            }

        }
        #endregion
        private static string GetText(string url)
        {
            string data = string.Empty;

            //получение строки данных
            data = GetData.Download(url: url);

            //если ответ не включает данную фразу, то задейстовать браузер
            if (!data.Contains("<!DOCTYPE html")) data = Toggle.Navigate(url);

            return data;
        }
        #region ParsePictures
        static void ParsePictures(AngleSharp.Dom.IElement tag, ref List<Picture> dict)
        {
            if(tag.TagName=="DIV"&&tag.HasChildNodes)
            {
                foreach(var tag1 in tag.Children)
                {
                    if(tag1.TagName=="IMG") dict.Add(new Picture(GetHashString($"https://zelv.ru{tag1.Attributes[0].Value}"), $"https://zelv.ru{tag1.Attributes[0].Value}"));
                }
            }
            else
            { 
            //бывает что картинки находятся внутри тегов p
            if (tag.NodeName == "P")
            {
                if (tag.HasChildNodes)
                {
                    foreach (var x in tag.Children)
                    {
                        if (x.LocalName == "img")
                        {
                            if (x.Attributes.Length > 0)
                            {
                                foreach (var attrib in x.Attributes)
                                {
                                    if (attrib.LocalName == "src") dict.Add(new Picture(GetHashString($"https://zelv.ru{attrib.Value}"), $"https://zelv.ru{attrib.Value}"));
                                }
                            }

                        }
                    }
                }
            }

            //бывает что напрямую вставлены в текст
            if (tag.LocalName == "img")
            {
                if (tag.Attributes.Length > 0)
                {
                    foreach (var attrib in tag.Attributes)
                    {
                        if (attrib.LocalName == "src") dict.Add(new Picture(GetHashString($"https://zelv.ru{attrib.Value}") + ".jpg", $"https://zelv.ru{attrib.Value}"));
                    }
                }

            }
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

        public static string GetHashString(string s)
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

        private static void ZelvFT()
        {
            //получение главного sitemap.xml
            string xml = GetData.Download(url: AppSettings.SitemapLink);
            //если ответ не пуст
            if (xml != string.Empty)
            {
                List<string> nodes = new List<string>();

                //загружаем ответ в XDocument
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(xml);

                XmlNodeList x = xd.GetElementsByTagName("sitemap");

                //перебор нодлиста sitemap
                foreach (XmlNode node in x)
                {
                    if ((node["loc"].InnerText.Contains($"sitemap{AppSettings.SitemapNum}"))) nodes.Add(node["loc"].InnerText);
                }

                //перебор отобранных файлов
                //System.Threading.Tasks.Parallel.ForEach(nodes, url =>
                foreach (string url in nodes)
                {
                    SingleCatalog(url);
                }
                GC.Collect();
            }
            else
            {
                Log.WriteError("error.txt", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "\t" + "в главном sitemap.xml нет нужных нод");
                Console.WriteLine("Невозможно спарсить основной sitemap.xml");
                Console.ReadKey();
            }
        }

        public static void HollywoodFT()
        {
            string url = AppSettings.SitemapLink;
            try
            {
                for (int page = 1; page < AppSettings.Days + 1; page++)
                {
                    string pageurl = url + $"/page-{page}.html";
                    SingleCatalog(pageurl);
                }
            }
            catch
            {
                Log.WriteError("error.txt", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "\t" + "cant parse page from site");
                Console.WriteLine("Cant parse page of site");
                Console.ReadKey();
            }
        }

        public static void DeadlineFT()
        {
            //получение главного sitemap.xml
            string xml = GetData.Download(url: AppSettings.SitemapLink);
            //если ответ не пуст
            if (xml != string.Empty)
            {
                List<string> nodes = new List<string>();

                //загружаем ответ в XDocument
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(xml);

                XmlNodeList x = xd.GetElementsByTagName("sitemap");

                //перебор нодлиста sitemap
                foreach (XmlNode node in x)
                {
                    if ((node["loc"].InnerText.Contains($"sitemap{AppSettings.SitemapNum}"))) nodes.Add(node["loc"].InnerText);
                }

                //перебор отобранных файлов
                //System.Threading.Tasks.Parallel.ForEach(nodes, url =>

                /*foreach (string url in nodes)
                {*/
                string url = nodes[0];
                SingleCatalog(url);
                //}
                GC.Collect();
            }
            else
            {
                Log.WriteError("error.txt", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "\t" + "в главном sitemap.xml нет нужных нод");
                Console.WriteLine("Невозможно спарсить основной sitemap.xml");
                Console.ReadKey();
            }
        }

        //Функция отвечающая за парсинг sitemap.xml - в результате выдающая ссылку на основной xml, в котором хранятся ссылки на статьи
        static void FirstTime()
        {
            switch (Program.project)
            {
                case "Zelv":
                    ZelvFT();
                    break;
                case "Hollywood":
                    HollywoodFT();
                    break;
                case "Deadline":
                    DeadlineFT();
                    break;
                case "Labirint":
                    LabirintFT();
                    break;
            }
        }

        private static void LabirintFT()
        {
            //получение главного sitemap.xml
            string xml = GetData.Download(url: AppSettings.SitemapLink);
            //если ответ не пуст
            if (xml != string.Empty)
            {
                List<string> nodes = new List<string>();

                //загружаем ответ в XDocument
                XmlDocument xd = new XmlDocument();
                xd.LoadXml(xml);

                XmlNodeList x = xd.GetElementsByTagName("sitemap");

                //перебор нодлиста sitemap
                foreach (XmlNode node in x)
                {
                    if ((node["loc"].InnerText.Contains($"smcatalog"))) nodes.Add(node["loc"].InnerText);
                }

                //перебор отобранных файлов
                //System.Threading.Tasks.Parallel.ForEach(nodes, url =>
                foreach (string url in nodes)
                {
                    LabirintSingleCatalog(url);
                }
                GC.Collect();
            }
            else
            {
                Log.WriteError("error.txt", DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "\t" + "в главном sitemap.xml нет нужных нод");
                Console.WriteLine("Невозможно спарсить основной sitemap.xml");
                Console.ReadKey();
            }
        }

        private static void LabirintSingleCatalog(string url)
        {
            if (AppSettings.Parse == 1 & full > AppSettings.RecordsCount) return;
            List<string[]> urls = new List<string[]>();
            //получение одного sm catalog xml

            string xmlbooks = GetData.Download(url: url);

            if (string.IsNullOrEmpty(xmlbooks)) return;

            //загрузка xml в документ
            XDocument doc = XDocument.Parse(xmlbooks);

            DateTime dt = DateTime.Now.Date;

            //список элементов со ссылкой
            var x = doc.Root.Elements();


            //перебор элементов внутри одного файла
            //System.Threading.Tasks.Parallel.ForEach(parallelOptions: new ParallelOptions{MaxDegreeOfParallelism = Environment.ProcessorCount}, source: x, body: delegate (XElement element)
            foreach (XElement element in x)
            {
                //пока пустые строки даты и урла
                string date = string.Empty;
                string loc = string.Empty;

                foreach (var m in element.Elements())
                {
                    //получаем урл в ноде
                    if (m.Name.LocalName == "loc") loc = m.Value;

                    if (!loc.Contains("/books/")) continue;

                    //получаем дату
                    if (m.Name.LocalName == "lastmod")
                    {
                        date = m.Value;

                        switch (AppSettings.Parse)
                        {
                            case 0:
                                Addition(loc, date, ref urls, ref common);
                                break;

                            //просто добавление статьи
                            case 1:
                                if (full < AppSettings.RecordsCount)
                                {
                                    Addition(loc, date, ref urls, ref common);
                                    full++;
                                }

                                break;

                            //получение статьи со сравнением текущей даты
                            case 2:
                                var difference = dt - DateTime.Parse(date);
                                if (difference.TotalDays <= AppSettings.Days) Addition(loc, date, ref urls, ref common);
                                break;
                            //получение нужного количества статей, исходя из настройки
                            case 3:
                                var difference1 = dt - DateTime.Parse(date);
                                if (difference1.TotalDays <= AppSettings.Days)
                                    if (full < AppSettings.RecordsCount)
                                    {
                                        Addition(loc, date, ref urls, ref common);
                                        full++;
                                    }
                                break;
                            //получение статей в пределах указанных дат
                            case 4:
                                if (DateTime.Parse(date) >= AppSettings.DateFrom & DateTime.Parse(date) <= AppSettings.DateTo)
                                {
                                    if (full < AppSettings.RecordsCount)
                                    {
                                        Addition(loc, date, ref urls, ref common);
                                        full++;
                                    }
                                }
                                break;
                        }
                    }
                }
            }//);
            //если trueзаписать в таблицу Catalog
            //    if (add == true) 
            if (urls.Count > 0) Storage.Database.WriteDataFullWithDate(urls, "Catalog");

            GC.Collect();

        }

        //парсинг xml с сайта
        public static void ParseXmls()
        {
            try
            {
                //получить данные из бд
                List<string> checkdb = Storage.Database.GetLinkTable("Catalog");
                checkdb.Sort();

                //если строк больше 0, значит парсинг был и нужно сравнение 2 списков.
                if (checkdb.Count > 0)
                {
                    Console.WriteLine("\nРежим проверки новых строк\n");
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
                    Console.WriteLine("\nРежим записи в новую базу\n");
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

        public static void ZelvSingleCatalog(string xmlbooks)
        {
            List<string[]> urls = new List<string[]>();
            
            //загрузка xml в документ
            XDocument doc = XDocument.Parse(xmlbooks);

            DateTime dt = DateTime.Now.Date;

            //список элементов со ссылкой
            var x = doc.Root.Elements();

            int g = 0;
            //перебор
            foreach (XElement element in x)
            {
                //пока пустые строки даты и урла
                string date = string.Empty;
                string loc = string.Empty;

                foreach (var m in element.Elements())
                {
                    //получаем урл в ноде
                    if (m.Name.LocalName == "loc") loc = m.Value;
                    //получаем дату
                    if (m.Name.LocalName == "lastmod")
                    {
                        date = m.Value;

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
                                var difference = dt - DateTime.Parse(date);
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
                }
            }
            if (urls.Count > 0) Storage.Database.WriteDataFullWithDate(urls, "Current");
            GC.Collect();
        }

        static void SingleCatalog(string url)
        {

            List<string[]> urls = new List<string[]>();
            //получение одного sm catalog xml

            string xmlbooks = GetData.Download(url: url);

            if (string.IsNullOrEmpty(xmlbooks)) return;


            switch (Program.project)
            {
                case "Zelv":
                    ZelvSingleCatalog(xmlbooks);
                    break;
                case "Hollywood":
                    HollywoodSingleCatalog(xmlbooks);
                    break;
                case "Deadline":
                    DeadlineSingleCatalog(xmlbooks);
                    break;
            }

            //если trueзаписать в таблицу Catalog
            //    if (add == true) 
            if (urls.Count > 0) Storage.Database.WriteDataFullWithDate(urls, "Current");
            GC.Collect();
        }

        private static void HollywoodSingleCatalog(string data)
        {
            List<string[]> urls = new List<string[]>();
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
                string loc = AppSettings.SitemapLink + element.GetElementsByTagName("article")[0].GetElementsByTagName("a")[0].GetAttribute("href");
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
            if (urls.Count > 0) Storage.Database.WriteDataFullWithDate(urls, "Current");
            GC.Collect();
        }

        private static void DeadlineSingleCatalog(string xmlbooks)
        {
            List<string[]> urls = new List<string[]>();
            //загрузка xml в документ
            XDocument doc = XDocument.Parse(xmlbooks);

            DateTime dt = DateTime.Now.Date;

            //список элементов со ссылкой
            var x = doc.Root.Elements();

            int g = 0;
            //перебор
            foreach (XElement element in x)
            {
                //пока пустые строки даты и урла
                string date = string.Empty;
                string loc = string.Empty;

                foreach (var m in element.Elements())
                {
                    //получаем урл в ноде
                    if (m.Name.LocalName == "loc") loc = m.Value;
                    //получаем дату
                    if (m.Name.LocalName == "lastmod")
                    {
                        date = m.Value;
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
                                var difference = dt - DateTime.Parse(date.ToString());
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
                }
            }
            if (urls.Count > 0) Storage.Database.WriteDataFullWithDate(urls, "Current");
            GC.Collect();
        }

        static void Addition(string loc, string date, ref List<string[]> urls, ref List<string[]> common)
        {
            if (!loc.Contains("/partnery/"))
            {
                //если false - записать в обычный список, для последующего сравнения. 
                if (add == false) common.Add(new[] { loc, date });
                else urls.Add(new[] { loc, date });
                Console.Write($">> {i} \r");
                i++;
            }
        }
    }
}


