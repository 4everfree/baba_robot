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
using NReco.VideoInfo;

namespace ZelvParser
{
    using static Tell;
    class Polly
    {
        static Dictionary<string, string> result = new Dictionary<string, string>();
        static int i = 1;
        

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

        public static int BackToTheFuture(string name)
        {
            
            //получить соответствующий названию строку в базе
            string textPart = Storage.Database.GetForAudioValue(name);

            //взять нужный кусок.

            //отправить на загрузку часть с индексом
            bool result = DownloadMp3(textPart,name);
            //сохранить.
            return 0;
        }

        public static void TextToMp3Creator(KeyValuePair<string,string> textPart)
        {
            //айди
            string key = textPart.Key;

            //текстовые части одним куском в json
            string value = textPart.Value;

            //десериализация частей, через newtonsoft.json
            Dictionary<string,string> parts = Deserialize.JsonToDictionaryString(value);
            List<string> convert_res = new List<string>(parts.Count);
            //перебор полученных частей
            foreach (KeyValuePair<string,string> part in parts)
            {
                Again:
                long timeCheck = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                string audio_file = Path.Combine(Program.audioD, $"{part.Key}.mp3");

                try
                {
                    //скачать mp3
                    bool runtime = DownloadMp3(part.Value, /*key,*/ audio_file);
                    if (!runtime) return;
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
                    string file = "errorDownloadMp3.txt";
                    Log.SendAndText(file, e);
                    
                    Tell.ColoredMessage($"Произошла ошибка - подробности в файле {file}\n\nДля выхода нажмите любую клавишу", ConsoleColor.Red);
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
                //добавить промежуточное значение в список
                convert_res.Add(Path.GetFileName(audio_file));
                ColoredWrite($@"  {i}\r\n", ConsoleColor.Green);
                i++;
            }
            //добавить новое значение в список
            result.Add(key, convert_res.Count > 1 ? string.Join(";", convert_res) : convert_res[0]);
        }

        //отправить данные в epolly
        static bool DownloadMp3(string text, /*string key,*/string audio_file_path)
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

                    switch(AppSettings.VoiceoverLang)
                    {
                        case "cmn-CN":
                            switch(AppSettings.VoiceId)
                            {
                                case "Zhiyu":
                                    req.VoiceId = Amazon.Polly.VoiceId.Zhiyu;
                                    break;
                            }
                            break;
                        case "da-DK":
                            switch (AppSettings.VoiceId)
                            {
                                case "Mads":
                                    req.VoiceId = Amazon.Polly.VoiceId.Mads;
                                    break;
                                case "Naja":
                                    req.VoiceId = Amazon.Polly.VoiceId.Naja;
                                    break;
                            }
                            break;
                        case "en-GB":
                            switch (AppSettings.VoiceId)
                            {
                                case "Brian":
                                    req.VoiceId = Amazon.Polly.VoiceId.Brian;
                                    break;
                                case "Amy":
                                    req.VoiceId = Amazon.Polly.VoiceId.Amy;
                                    break;
                                case "Emma":
                                    req.VoiceId = Amazon.Polly.VoiceId.Emma;
                                    break;
                            }
                            break;

                        case "en-IN":
                            switch (AppSettings.VoiceId)
                            {
                                case "Aditi":
                                    req.VoiceId = Amazon.Polly.VoiceId.Aditi;
                                    break;
                                case "Raveena":
                                    req.VoiceId = Amazon.Polly.VoiceId.Raveena;
                                    break;
                            }
                            break;

                        case "en-US":
                            switch (AppSettings.VoiceId)
                            {
                                case "Joey":
                                    req.VoiceId = Amazon.Polly.VoiceId.Joey;
                                    break;
                                case "Justin":
                                    req.VoiceId = Amazon.Polly.VoiceId.Justin;
                                    break;
                                case "Matthew":
                                    req.VoiceId = Amazon.Polly.VoiceId.Matthew;
                                    break;
                                case "Ivy":
                                    req.VoiceId = Amazon.Polly.VoiceId.Ivy;
                                    break;
                                case "Joanna":
                                    req.VoiceId = Amazon.Polly.VoiceId.Joanna;
                                    break;
                                case "Kendra":
                                    req.VoiceId = Amazon.Polly.VoiceId.Kendra;
                                    break;
                                case "Kimberly":
                                    req.VoiceId = Amazon.Polly.VoiceId.Kimberly;
                                    break;
                                case "Salli":
                                    req.VoiceId = Amazon.Polly.VoiceId.Salli;
                                    break;
                            }
                            break;

                            
                            case "en-GB-WLS":
                            switch (AppSettings.VoiceId)
                            {
                                case "Geraint":
                                    req.VoiceId = Amazon.Polly.VoiceId.Geraint;
                                    break;
                            }
                            break;

                        case "fr-FR":
                            switch (AppSettings.VoiceId)
                            {
                                case "Mathieu":
                                    req.VoiceId = Amazon.Polly.VoiceId.Mathieu;
                                    break;
                                case "Celine":
                                    req.VoiceId = Amazon.Polly.VoiceId.Celine;
                                    break;
                            }
                            break;

                        case "fr-CA":
                            switch (AppSettings.VoiceId)
                            {
                                case "Chantal":
                                    req.VoiceId = Amazon.Polly.VoiceId.Chantal;
                                    break;
                            }
                            break;

                        case "de-DE":
                            switch (AppSettings.VoiceId)
                            {
                                case "Hans":
                                    req.VoiceId = Amazon.Polly.VoiceId.Hans;
                                    break;
                                case "Marlene":
                                    req.VoiceId = Amazon.Polly.VoiceId.Marlene;
                                    break;
                                case "Vicki":
                                    req.VoiceId = Amazon.Polly.VoiceId.Vicki;
                                    break;
                            }
                            break;

                        case "hi-IN":
                            switch (AppSettings.VoiceId)
                            {
                                case "Aditi":
                                    req.VoiceId = Amazon.Polly.VoiceId.Aditi;
                                    break;
                            }
                            break;

                        case "is-IS":
                            switch (AppSettings.VoiceId)
                            {
                                case "Karl":
                                    req.VoiceId = Amazon.Polly.VoiceId.Karl;
                                    break;
                                case "Dora":
                                    req.VoiceId = Amazon.Polly.VoiceId.Dora;
                                    break;
                            }
                            break;

                        case "it-IT":
                            switch (AppSettings.VoiceId)
                            {
                                case "Giorgio":
                                    req.VoiceId = Amazon.Polly.VoiceId.Giorgio;
                                    break;
                                case "Carla":
                                    req.VoiceId = Amazon.Polly.VoiceId.Carla;
                                    break;
                                case "Bianca":
                                    req.VoiceId = Amazon.Polly.VoiceId.Bianca;
                                    break;
                            }
                            break;


                        case "ja-JP":
                            switch (AppSettings.VoiceId)
                            {
                                case "Takumi":
                                    req.VoiceId = Amazon.Polly.VoiceId.Takumi;
                                    break;
                                case "Mizuki":
                                    req.VoiceId = Amazon.Polly.VoiceId.Mizuki;
                                    break;
                            }
                            break;

                        case "ko-KR":
                            switch (AppSettings.VoiceId)
                            {
                                case "Seoyeon":
                                    req.VoiceId = Amazon.Polly.VoiceId.Seoyeon;
                                    break;
                            }
                            break;

                        case "nb-NO":
                            switch (AppSettings.VoiceId)
                            {
                                case "Liv":
                                    req.VoiceId = Amazon.Polly.VoiceId.Liv;
                                    break;
                            }
                            break;

                        case "pl-PL":
                            switch (AppSettings.VoiceId)
                            {
                                case "Jacek":
                                    req.VoiceId = Amazon.Polly.VoiceId.Jacek;
                                    break;
                                case "Jan":
                                    req.VoiceId = Amazon.Polly.VoiceId.Jan;
                                    break;
                                case "Ewa":
                                    req.VoiceId = Amazon.Polly.VoiceId.Ewa;
                                    break;
                                case "Maja":
                                    req.VoiceId = Amazon.Polly.VoiceId.Maja;
                                    break;
                            }
                            break;

                        case "pt-BR":
                            switch (AppSettings.VoiceId)
                            {
                                case "Ricardo":
                                    req.VoiceId = Amazon.Polly.VoiceId.Ricardo;
                                    break;
                                case "Vitoria":
                                    req.VoiceId = Amazon.Polly.VoiceId.Vitoria;
                                    break;
                            }
                            break;

                        case "pt-PT":
                            switch (AppSettings.VoiceId)
                            {
                                case "Cristiano":
                                    req.VoiceId = Amazon.Polly.VoiceId.Cristiano;
                                    break;
                                case "Ines":
                                    req.VoiceId = Amazon.Polly.VoiceId.Ines;
                                    break;
                            }
                            break;

                        case "ro-RO":
                            switch (AppSettings.VoiceId)
                            {
                                case "Carmen":
                                    req.VoiceId = Amazon.Polly.VoiceId.Carmen;
                                    break;
                            }
                            break;

                        case "ru-RU":
                            switch (AppSettings.VoiceId)
                            {
                                case "Maxim":
                                    req.VoiceId = Amazon.Polly.VoiceId.Maxim;
                                    break;
                                case "Tatyana":
                                    req.VoiceId = Amazon.Polly.VoiceId.Tatyana;
                                    break;
                            }
                            break;

                        case "es-ES":
                            switch (AppSettings.VoiceId)
                            {
                                case "Enrique":
                                    req.VoiceId = Amazon.Polly.VoiceId.Enrique;
                                    break;
                                case "Conchita":
                                    req.VoiceId = Amazon.Polly.VoiceId.Conchita;
                                    break;
                                case "Lucia":
                                    req.VoiceId = Amazon.Polly.VoiceId.Lucia;
                                    break;
                            }
                            break;


                        case "es-MX":
                            switch (AppSettings.VoiceId)
                            {
                                case "Mia":
                                    req.VoiceId = Amazon.Polly.VoiceId.Mia;
                                    break;
                            }
                            break;


                        case "es-US":
                            switch (AppSettings.VoiceId)
                            {
                                case "Miguel":
                                    req.VoiceId = Amazon.Polly.VoiceId.Miguel;
                                    break;
                                case "Penelope":
                                    req.VoiceId = Amazon.Polly.VoiceId.Penelope;
                                    break;
                            }
                            break;

                        case "sv-SE":
                            switch (AppSettings.VoiceId)
                            {
                                case "Astrid":
                                    req.VoiceId = Amazon.Polly.VoiceId.Astrid;
                                    break;
                            }
                            break;

                        case "tr-TR":
                            switch (AppSettings.VoiceId)
                            {
                                case "Filiz":
                                    req.VoiceId = Amazon.Polly.VoiceId.Filiz;
                                    break;
                            }
                            break;

                        case "cy-GB":
                            switch (AppSettings.VoiceId)
                            {
                                case "Gwyneth":
                                    req.VoiceId = Amazon.Polly.VoiceId.Gwyneth;
                                    break;
                            }
                            break;


                    }
                    req.OutputFormat = Amazon.Polly.OutputFormat.Mp3;
                    req.SampleRate = AppSettings.SampleRate;
                    req.TextType = Amazon.Polly.TextType.Text;
                    resp = cl.SynthesizeSpeech(req);
                }
                catch (Exception e)
                {
                    Log.WriteError("errorEPolly.txt", e);
                    if (e.Message.Contains("security token included"))
                    {
                        File.AppendText("wrong-token.txt");
                        Tell.ColoredMessage("Нужно поменять токен\n\nДля выхода нажмите любую клавишу", ConsoleColor.Red);
                        Console.ReadKey();
                        Environment.Exit(-1);
                    }
                    return false;
                }

                if (!string.IsNullOrEmpty(AppSettings.VoiceVolume))
                {
                    //сохранение временного файла со озвучкой
                    string tempFile = System.IO.Path.Combine(Program.Path,System.IO.Path.ChangeExtension(System.IO.Path.GetRandomFileName(),"mp3"));

                    SaveAudioFile(resp, tempFile);
                    //сохранение файла с другой озвучкой
                    string command = $"-i \"{tempFile}\" -af \"volume={AppSettings.VoiceVolume}dB\" -y -ar 48000 {audio_file_path}";
                    FFmpeg.ToFFmpeg(command);
                    File.Delete(tempFile);
                }
                else SaveAudioFile(resp, audio_file_path);
                
                var dtmi = DateTime.Now.Subtract(dtn);
            }
            TimeSpan ts = DateTime.Now.Subtract(dtx);
            Console.WriteLine($"Работа сделана за {ts.Milliseconds} миллисекунд");
            return true;
        }

        private static void SaveAudioFile(Amazon.Polly.Model.SynthesizeSpeechResponse resp, string audio_file_path)
        {
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
        }

        public static TimeSpan Duration(string file)
        {
            var ffProbe = new FFProbe();
            var videoInfo = ffProbe.GetMediaInfo(file);
            var time = videoInfo.Duration;
            return time;
        }
    }
}

