using Amazon;
using Amazon.Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HollywoodParser
{
    using static Tell;
    class EPolly
    {
        static Dictionary<string, string> result = new Dictionary<string, string>();
        static int i = 1;
        static bool voice = false;


        public static bool CheckMp3(string audio)
        {
            //если файл существует, вернуть true
            if (File.Exists(Path.Combine(Program.Path, Program.audioD, audio))) return true;
            //если не существует то вернуть false
            else return false;
        }

        public static void CreateMp3(Dictionary<string,string> texts)
        {         
            //foreach (var textPart in texts)
            System.Threading.Tasks.Parallel.ForEach(texts,new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },textPart =>
            {
                TextToMp3Creator(textPart);
            });
            
            //отметить в базе получение файла аудио
            Storage.Database.WriteData("Records", "Audio", result);
            
        }

        public static void TextToMp3Creator(KeyValuePair<string,string> textPart)
        {
            //айди
            string key = textPart.Key;

            //текстовые части одним куском в json
            string value = textPart.Value;

            //десериализация частей, через newtonsoft.json
            List<string> parts = Deserialize.JsonToListString(value);
            List<string> convert_res = new List<string>(parts.Count);
            //перебор полученных частей
            foreach (string part in parts)
            {
                Again:
                long timeCheck = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string audio_file = Path.Combine(Program.audioD, $"{Guid.NewGuid()}.mp3");

                try
                {
                    //скачать mp3
                    DownloadMp3(part, key, audio_file);
                    bool exists = File.Exists(audio_file);
                    if (!exists)
                    {
                        System.Threading.Thread.Sleep(100);
                        goto Again;
                    }
                    System.Threading.Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    Log.SendAndText("errorDownloadMp3.txt", e);
                }
                //добавить промежуточное значение в список
                convert_res.Add(Path.GetFileName(audio_file));
                ColoredWrite($" {i}/{Program.c}", ConsoleColor.Green);
                i++;
            }
            //добавить новое значение в список
            result.Add(key, convert_res.Count > 1 ? string.Join(";", convert_res) : convert_res[0]);
        }

        //отправить данные в epolly
        static void DownloadMp3(string text, string key,string audio_file_path)
        {
            DateTime dtx = DateTime.Now;
            Random r = new Random();
            DateTime dtn = DateTime.Now;
            if (!string.IsNullOrEmpty(text))
            {
                Amazon.Polly.AmazonPollyClient cl = null;
                Amazon.Polly.Model.SynthesizeSpeechResponse resp = null;
                try
                {
                    cl = new Amazon.Polly.AmazonPollyClient(AppSettings.Api_id, AppSettings.Api_secret, RegionEndpoint.EUWest1);
                    Amazon.Polly.Model.SynthesizeSpeechRequest req = new Amazon.Polly.Model.SynthesizeSpeechRequest();
                    req.Text = text;
                    if (voice)
                    {
                        req.VoiceId = Amazon.Polly.VoiceId.Kendra;
                        voice = false;
                    }
                    else
                    {
                        req.VoiceId = Amazon.Polly.VoiceId.Matthew;
                        voice = true;
                    }
                    req.OutputFormat = Amazon.Polly.OutputFormat.Mp3;
                    req.SampleRate = AppSettings.SampleRate;
                    req.TextType = Amazon.Polly.TextType.Text;
                    resp = cl.SynthesizeSpeech(req);
                }
                catch (Exception e)
                {
                    Log.WriteError("errorEPolly.txt", e);
                }
                
                FileStream fs = null;
                try
                {
                    fs = new FileStream(audio_file_path, FileMode.CreateNew);
                    resp.AudioStream.CopyTo(fs);
                    fs.Flush();
                }
                catch (Exception e)
                {
                    Log.WriteError("errorEPolly.txt", e);
                    audio_file_path = System.IO.Path.GetFullPath(audio_file_path).Replace(".mp3", "_1.mp3");
                    fs = new FileStream(audio_file_path, FileMode.CreateNew);
                    resp.AudioStream.CopyTo(fs);
                    fs.Flush();
                }
                finally
                {
                    fs.Close();
                }
                var dtmi = DateTime.Now.Subtract(dtn);
            }
            TimeSpan ts = DateTime.Now.Subtract(dtx);
            Console.WriteLine($"\nРабота сделана за {ts.Milliseconds} миллисекунд\n");
        }
    }
}

