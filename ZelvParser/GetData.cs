using System;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace ZelvParser
{
    class GetData
    {
        static string[] ua = File.ReadAllLines(Path.Combine(Program.Path, "UA.txt"));
        static string UserAgent()
        {
            return ua[Program.r.Next(ua.Length)];
        }
        public static string Download(string url, string downloadMode = "Text", string filename = "",bool proxy = false)
        {          
            string ua = string.Empty;
            string res = string.Empty;

            //клиент WebClient
            WebClient wc = new WebClient();
            wc.Encoding = System.Text.Encoding.UTF8;

            ServicePointManager.ServerCertificateValidationCallback = delegate (object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            int pop = 0;
            //цикл чтобы не застрять в while
            for (int counter = 1;counter < 5;counter++)
            {
                try
                {
                    ua = UserAgent();
                    if (proxy == true)
                    {
                        string[] proxies = File.ReadAllLines(Path.Combine(Program.Path, "Proxy.txt"));
                        string[] row = proxies[Program.r.Next(proxies.Length)].Split(new char[] { ':' }, StringSplitOptions.None);
                        wc.Proxy = new WebProxy(row[0], int.Parse(row[1]));
                    }
                }

                catch (Exception e)
                {
                    Log.WriteError("proxyGetError.txt", e);
                }


                //заполнение хедеров
                try
                {
                    wc.Headers.Add(HttpRequestHeader.UserAgent,ua);
                    wc.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                    wc.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                    wc.Headers.Add(HttpRequestHeader.KeepAlive, "true");
                }
                catch(Exception e)
                {
                    Log.WriteError("errorUA.txt", ua + "\n");
                    Log.SendAndText("errorAddHeaders.txt", e);
                }
                
                //режим загрузки
                switch (downloadMode)
                {
                    case "Text":
                        try
                        {//скачать строку
                            res = wc.DownloadString(url);
                            if(Program.project.Contains("Zelv")) MakeRequests();
                            //если ответ пришел - выйти из цикла
                            if (url.Contains("smcatalog")) if (!string.IsNullOrEmpty(res)) counter = 10;
                            if (!url.Contains("sitemap"))
                            {
                                if (!res.Contains("<!DOCTYPE html")) break;
                            }
                            if(!string.IsNullOrEmpty(res)) counter = 10;
                        }
                        catch (Exception e)
                        {//в любом случае, при исключении переключится сюда
                            if (proxy == false) proxy = true; 
                            Log.SendAndText("textDownloadError.txt", e);
                            if(pop==10)
                            { 
                            res = Toggle.Navigate(url);
                            if (!string.IsNullOrEmpty(res)) counter = 10;
                            }
                            pop++;
                        }
                        break;

                    case "File":
                        //ссылка на папку изображений
                        string ImagesPath = Path.Combine(Program.Path, Program.imagesD);
                        //путь к текущему изображению
                        string path = Path.Combine(ImagesPath, filename);


                        try
                        {
                            byte[] response = wc.DownloadData(url);
                            if (response != null)
                            {
                                //запись в файл потока полученных байт
                                File.WriteAllBytes(path, response);
                                //выход из цикла
                                counter = 10;
                            }
                            else proxy = true;
                        }
                        catch (Exception e)
                        {
                            if (e.Message.Contains("(404)"))
                            {
                                res = "404";
                                counter = 5;
                                break;
                            }
                            if (counter == 4)
                            {
                                counter = 5;
                                break;

                            }
                            Console.WriteLine($"Try to download file: {counter}");
                            proxy = true;
                            Log.WriteError("fileDownloadError.txt", e);
                        }
                        break;
                }
            }
            return res;
        }
        private static void MakeRequests()
        {
            HttpWebResponse response;

            if (Request_zelv_ru(out response))
            {
                response.Close();
            }
        }

        private static bool Request_zelv_ru(out HttpWebResponse response)
        {
            response = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://zelv.ru/engine/modules/antibot/antibot.php");

                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; rv:45.0) Gecko/20100101 Firefox/45.0";
                request.Accept = "image/png,image/*;q=0.8,*/*;q=0.5";
                request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                request.Referer = "https://zelv.ru/auto/79572-stali-izvestny-samye-prodavaemye-v-rf-poderzhannye-vnedorozhniki.html";
                request.KeepAlive = true;

                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError) response = (HttpWebResponse)e.Response;
                else return false;
            }
            catch (Exception)
            {
                if (response != null) response.Close();
                return false;
            }

            return true;
        }
    }
}

