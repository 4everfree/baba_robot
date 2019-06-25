using TagLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NReco.VideoInfo;

namespace ZelvParser
{
    using System.Collections.Generic;
    using static AppSettings;
    using static Program;
    static class FFmpeg
    {
        public const string FileName = "ffmpeg";
        public static bool mute = AppSettings.Mute == 1 ? true : false;

        //public static int i = 0;

        /// <summary>
        /// creating video file mpeg-ts using audio and video that was stored in databases
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <param name="audioFilePath"></param>
        /// <param name="outputFilePath"></param>

        public static void CreateVideoFromImageAndAudio(string imageFilePath, string audioFilePath, string outputFilePath, List<string> pList)
        {
            StartFunction:
            //i++;
            TimeSpan audioFileDuration;
            TagLib.File file = TagLib.File.Create(audioFilePath);
            audioFileDuration = file.Properties.Duration;
            audioFileDuration += TimeSpan.FromMilliseconds(FFmpegAudioDurationOffset);

            #region old
            /*
            //-vf \"zoompan = z = 'if(lte(zoom,1.0),1.5,max(1.001,zoom-0.0015))':d = 125\"  перед yuv420p
            startInfo.Arguments = $"-loop 1 -i {imageFilePath} -i {audioFilePath} -c:v libx264 -tune stillimage -c:a aac -b:a 192k -pix_fmt yuv420p -shortest -t {audioFileDuration.ToString(@"hh\:mm\:ss\.fff")} -y {outputFilePath}";
            Process.Start(startInfo).WaitForExit();
            */
            #endregion

            DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(Path, tempD));
            int i = 0;
            switch (AppSettings.Effects)
            {
                default:
                    do
                    {
                        if (i == 1)
                        {
                            var x = di.EnumerateFiles("*.ts");
                            foreach (var y in x) if (y.Exists) y.Delete();
                            goto StartFunction;
                        }
                        OnlyBlack(imageFilePath, audioFilePath, audioFileDuration, outputFilePath);
                        i++;
                    }
                    while (!CheckFile(outputFilePath));
                    Tell.ColoredMessage($"\nFile {outputFilePath} created\n", ConsoleColor.Green);
                    break;

                case "5":
                    do
                    {
                        if (i == 1)
                        {
                            var x = di.EnumerateFiles("*.ts");
                            foreach (var y in x) if (y.Exists) y.Delete();
                            goto StartFunction;
                        }
                        OnlyZoom(pList, audioFilePath, audioFileDuration, outputFilePath);
                        i++;
                    }
                    while (!CheckFile(outputFilePath));
                    Tell.ColoredMessage($"\nFile {outputFilePath} created\n", ConsoleColor.Green);
                    break;
                case "2":
                    do
                    {
                        if (i == 1)
                        {
                            var x = di.EnumerateFiles("*.ts");
                            foreach (var y in x) if (y.Exists) y.Delete();
                            goto StartFunction;
                        }
                        OnlyFadeINOUT(imageFilePath, audioFilePath, audioFileDuration, outputFilePath);
                        i++;
                    }
                    while (!CheckFile(outputFilePath));
                    Tell.ColoredMessage($"\nFile {outputFilePath} created\n", ConsoleColor.Green);
                    break;
                case "3":
                    string temporaryFile = $"{Program.tempD}/temp_{Guid.NewGuid()}.ts";
                    do
                    {
                        if (i == 1)
                        {
                            var x = di.EnumerateFiles("*.ts");
                            foreach (var y in x) if (y.Exists) y.Delete();
                            goto StartFunction;
                        }
                        OnlyBlack(imageFilePath, audioFilePath, audioFileDuration, temporaryFile);
                        i++;
                    }
                    while (!CheckFile(temporaryFile));

                    Tell.ColoredMessage($"\nFile {temporaryFile} created\n", ConsoleColor.Red);

                    i = 0;
                    do
                    {
                        if (i == 1)
                        {
                            var x = di.EnumerateFiles("*.ts");
                            foreach (var y in x) if (y.Exists) y.Delete();
                            goto StartFunction;
                        }
                        System.IO.FileInfo fileblack = new FileInfo(temporaryFile);
                        if (fileblack.Exists & fileblack.Length > 0) OnlyBlur(temporaryFile, outputFilePath);
                        else if (fileblack.Exists & fileblack.Length == 0) goto StartAgain;
                        i++;
                    }
                    while (!CheckFile(outputFilePath));
                    Tell.ColoredMessage($"\nFile {outputFilePath} created\n", ConsoleColor.Red);
                    break;
                case "4":
                    StartAgain:
                    temporaryFile = $"{Program.tempD}/temp_{Guid.NewGuid()}.ts";
                    do
                    {
                        if (i == 1)
                        {
                            var x = di.EnumerateFiles("*.ts");
                            foreach (var y in x) if (y.Exists) y.Delete();
                            goto StartFunction;
                        }
                        OnlyFadeINOUT(imageFilePath, audioFilePath, audioFileDuration, temporaryFile);
                        i++;
                    }
                    while (!CheckFile(temporaryFile));

                    Tell.ColoredMessage($"\nFile {temporaryFile} created\n", ConsoleColor.Red);

                    i = 0;
                    System.IO.FileInfo filefo = new FileInfo(temporaryFile);
                    do
                    {
                        if (i == 1)
                        {
                            var x = di.EnumerateFiles("*.ts");
                            foreach (var y in x) if (y.Exists) y.Delete();
                            goto StartFunction;
                        }
                        if (filefo.Exists & filefo.Length > 0) OnlyBlur(temporaryFile, outputFilePath);
                        else if (filefo.Exists & filefo.Length == 0) goto StartAgain;
                        i++;
                    } while (!CheckFile(outputFilePath));

                    Tell.ColoredMessage($"\nFile {outputFilePath} created\n", ConsoleColor.Red);
                    break;
            }
        }

        /// <summary>
        /// creates video with zoom effect
        /// if audio duration less than 15 seconds - creates only fade in out preview, needed for Zelv advertising working
        /// </summary>
        /// <param name="pList">pictures list from video</param>
        /// <param name="audioFilePath">filepath to audio</param>
        /// <param name="audioFileDuration">duration of audio</param>
        /// <param name="outputFilePath">path to output mpeg-ts file</param>
        private static void OnlyZoom(List<string> pList, string audioFilePath, TimeSpan audioFileDuration, string outputFilePath)
        {
            double totalTime = audioFileDuration.TotalSeconds;
            if (audioFileDuration > TimeSpan.Parse("00:00:15"))
            {
                string inputFiles = "";
                string blocks = "";

                string overlayOutput = string.Empty;
                double timePart = totalTime / pList.Count;
                double part = default(double);
                string zoom = (0.3 / (timePart * 60)).ToString().Replace(",", ".");
                int pictureListCount = pList.Count;
                for (int i = 0; i < pictureListCount; i++)
                {
                    inputFiles += $" -i {pList[i]}";
                    blocks += $"[{i}:v]format=pix_fmts=yuva420p,scale=8000x4000,zoompan=z='if(gte(zoom,1.3)+eq(ld(1),1)*gt(zoom,1),zoom-{zoom}*st(1,1),zoom+{zoom}+0*st(1,0))':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)':fps=60:d=60*{timePart.ToString().Replace(",", ".")}:s=1920x1080,fade=t=in:st=0:d=1:alpha=0,fade=t=out:st={(timePart).ToString().Replace(",", ".")}:d=1:alpha=1,setpts=PTS-STARTPTS{((i == 0) ? string.Empty : $"+{i}*{pList.Count}/TB")}[v{i}];";


                    if (i == 0) overlayOutput += $"[black][v{i}]overlay[ov{i}];";
                    else if (i + 1 == pictureListCount) overlayOutput += $"[ov{i - 1}][v{i}]overlay=format=yuv420";
                    else overlayOutput += $"[ov{i - 1}][v{i}]overlay[ov{i}];";
                    part += timePart;
                }

                string command = $"-y{inputFiles} -i {audioFilePath} -filter_complex " +
                    $"\"color=c=black:r=60:size=1920x1080:d={(totalTime).ToString().Replace(",", ".")}[black];{blocks}{overlayOutput}\" -c:v libx264 -preset:v ultrafast {outputFilePath}";

                ToFFmpeg(command);
            }
            else
            {
                OnlyFadeINOUT(pList[0], audioFilePath, audioFileDuration, outputFilePath);
            }
        }

        /// <summary>
        /// send command to FFMpeg 
        /// </summary>
        /// <param name="command">string of commands that FFMpeg uses</param>
        public static void ToFFmpeg(string command)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(FileName);
            startInfo.Arguments = command;
            Tell.ColoredMessage("ffmpeg " + command+"\r\n", ConsoleColor.Blue);
            if (HideFFmpegWidnow) { startInfo.CreateNoWindow = true; startInfo.UseShellExecute = false; };
            Process.Start(startInfo).WaitForExit();
        }
        /// <summary>
        /// check existing of file, path contained to source
        /// </summary>
        /// <param name="source">filepath</param>
        /// <returns>bool - existense of file</returns>
        public static bool CheckFile(string source)
        {
            if (System.IO.File.Exists(source))
            {
                Tell.ColoredMessage($"\nFile {source} exists\n", ConsoleColor.White);
                return true;
            }
            return false;
        }

        public static void OnlyBlack(string imageFilePath, string audioFilePath, TimeSpan audioFileDuration, string outputFilePath)
        {
            string command = string.Empty;
            if (mute) command = $"-loop 1 -i {imageFilePath} -c:v libx264 -tune stillimage -pix_fmt yuv420p -shortest -t {audioFileDuration.ToString(@"hh\:mm\:ss\.fff")} -y {preset}{outputFilePath}";
            else command = $"-loop 1 -i {imageFilePath} -i {audioFilePath} -c:v libx264 -tune stillimage -c:a aac -b:a 192k ar 48000 -pix_fmt yuv420p -shortest -t {audioFileDuration.ToString(@"hh\:mm\:ss\.fff")} -y {preset}{outputFilePath}";

            ToFFmpeg(command);
        }
        public static void OnlyFadeINOUT(string imageFilePath, string audioFilePath, TimeSpan audioFileDuration, string outputFilePath)
        {
            string command = string.Empty;
            string startFadeOut = Convert.ToString(audioFileDuration.TotalSeconds - double.Parse(AppSettings.DurationFadeOut)).Replace(",", ".");

            if (mute) command = $"-loop 1 -i {imageFilePath} -c:v libx264 -vf \"fade=t=in:st={AppSettings.StartFadeIn.Replace(",", ".")}:d={AppSettings.DurationFadeIn.Replace(",", ".")},fade=t=out:st={startFadeOut}:d={AppSettings.DurationFadeOut.Replace(",", ".")}\" -tune stillimage -pix_fmt yuv420p -shortest -t {audioFileDuration.ToString(@"hh\:mm\:ss\.fff")} -y {preset}{outputFilePath}";
            else command = $"-loop 1 -i {imageFilePath} -i {audioFilePath} -c:v libx264 -vf \"fade=t=in:st={AppSettings.StartFadeIn.Replace(",", ".")}:d={AppSettings.DurationFadeIn.Replace(",", ".")},fade=t=out:st={startFadeOut}:d={AppSettings.DurationFadeOut.Replace(",", ".")}\" -tune stillimage -c:a aac -b:a 192k -ar 48000 -pix_fmt yuv420p -shortest -t {audioFileDuration.ToString(@"hh\:mm\:ss\.fff")} -y {preset}{outputFilePath}";

            ToFFmpeg(command);
        }
        public static void OnlyBlur(string inputVideo, string outputFilePath)
        {
            string command = $"-i {inputVideo} -filter_complex \"[0:v]scale=ih*16/9:-1,boxblur=luma_radius=min(h\\,w)/20:luma_power=1:chroma_radius=min(cw\\,ch)/20:chroma_power=1[bg];[bg][0:v]overlay=(W-w)/2:(H-h)/2,crop=h=iw*9/16\" -y {preset}{outputFilePath}";
            ToFFmpeg(command);

            if (!CheckFile(outputFilePath)) return;
        }

        public static void JoinAudio(FFmpegMain.VideoInfo video)
        {
            var audios = video.Parts.Select(x => x.Audio);
            string audioRes = string.Empty;
            foreach (string x in audios)
            {
                if (audios.First() == x) audioRes += $"{Program.audioD}/" + x;
                else audioRes += $"|{ Program.audioD}/" + x;
            }
            string command = $"-i \"concat:{audioRes}\" {preset}-y {Program.tempD}/noend.mp3";
            ToFFmpeg(command);

            if (System.IO.File.Exists(System.IO.Path.Combine(tempD, "noend.mp3"))) Tell.ColoredMessage($"\nFile {Program.tempD}/noend.mp3 created\n", ConsoleColor.White);
        }

        public static void JoinVideoFiles(string outputFilePath, string[] inputFilePaths)
        {
            string listFileName = Program.tempD + "/list.txt";
            System.IO.File.WriteAllLines(listFileName, inputFilePaths.Select(x => $"file '{System.IO.Path.GetFileName(x)}'"));

            string command = $"-f concat -i {listFileName} -c copy -y {outputFilePath}";
            ToFFmpeg(command);
        }

        public static void ConvertVideoToTS(string outputFilePath, out string result)
        {
            string resultPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outputFilePath), System.IO.Path.GetFileNameWithoutExtension(outputFilePath) + ".ts");

            string command = $"-i {outputFilePath} -acodec copy -vcodec copy -vbsf h264_mp4toannexb -f mpegts {preset}{resultPath}";//$"-f concat -i {listFileName} -c copy -y {outputFilePath}";
            ToFFmpeg(command);
            result = resultPath;
        }

        public static void ConvertVideoToTS(string outputFilePath)
        {
            string resultPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(outputFilePath), System.IO.Path.GetFileNameWithoutExtension(outputFilePath) + ".ts");

            string command = $"-i {outputFilePath} -acodec copy -vcodec copy -vbsf h264_mp4toannexb -f mpegts {preset}{resultPath}";//$"-f concat -i {listFileName} -c copy -y {outputFilePath}";
            ToFFmpeg(command);
        }

        public static void ConvertTSToMp4v2(string outputFilePath, string[] inputFilePath)
        {
            //new list
            List<string> list = new List<string>();


            string command = string.Empty;


            //determine do we need advertisement here
            if (AppSettings.Ads != "N")
            {
                //if yes, choose what ads we need to choose
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



                //where to put advertising
                int index = AdsPosition;
                if (AdsPosition > inputFilePath.Count()) index = 1;
                #region old code
                /*TimeSpan ts = new TimeSpan();
                


                for (int x = 0; x < index; x++)
                {
                    var ffProbe = new FFProbe();
                    var videoInfo = ffProbe.GetMediaInfo(inputFilePath[x]);
                    var time = videoInfo.Duration;
                    ts += time;
                }*/
                #endregion
                if (!string.IsNullOrEmpty(AppSettings.BackgroundMusic))
                {
                    //take names of videos before ads and merge with background music on it
                    //background function needs to be written
                    string[] array1 = new string[index];
                    Array.Copy(inputFilePath, 0, array1, 0, index);


                    //добавить рекламный ролик
                    list.Add(AddMusic(1, array1));

                    list.Add("Ads/" + fi.Name);

                    //take names of videos after ads and merge with background music
                    //merge start and end and advertisement
                    string[] array2 = new string[inputFilePath.Count() - array1.Count()];
                    Array.Copy(inputFilePath, AdsPosition, array2, 0, inputFilePath.Count() - array1.Count());
                    list.Add(AddMusic(2, array2));
                }
                //advertising, no music
                else
                {
                    list = inputFilePath.ToList();
                    list.Insert(index, "Ads/" + fi.Name);
                }
            }
            else
            {
                //copy input files from temp to main list where endings and start videos will be concatenated

                //no advertising, with music
                if (!string.IsNullOrEmpty(AppSettings.BackgroundMusic)) list.Add(AddMusic(0, inputFilePath));
                //no advertising, no music
                else list = inputFilePath.ToList();

            }

            string unitedinput = string.Empty;
            string channels = string.Empty;

            for (int i = 0; i < list.Count; i++)
            {
                unitedinput += "-i " + list.ElementAt(i) + " ";
                channels += $"[{i}:v][{i}:a]";
            }


            
                //add endings to video
                if (StartVideoIncluding) list = FFmpegMain.ViewCheck(StartVideoIncluding, Program.project.ToLower() + "_" + StartVideoFileName, list);
                if (EndVideoIncluding) list = FFmpegMain.ViewCheck(EndVideoIncluding, Program.project.ToLower() + "_" + EndVideoFileName, list);

                

                string speed = string.Empty;
                if (AppSettings.PlaybackSpeed != "1")
                {
                    string videospeed = Convert.ToString(1 / Double.Parse(AppSettings.PlaybackSpeed)).Replace(',', '.');
                    string audiospeed = Convert.ToString(1 * Double.Parse(AppSettings.PlaybackSpeed)).Replace(',', '.');
                    speed = $"[v]setpts={videospeed}*PTS[v1];[a]atempo={audiospeed}[a1]";
                    command = $"-y {unitedinput}-filter_complex \"{channels}concat=n={list.Count}:v=1:a=1[v][a];{speed}\" -map \"[v1]\" -map \"[a1]\" {AppSettings.FFmpegPreset}{outputFilePath}";
                }
                else command = $"-y {unitedinput}-filter_complex \"{channels}concat=n={list.Count}:v=1:a=1[v][a]\" -map \"[v]\" -map \"[a]\" {AppSettings.FFmpegPreset}{outputFilePath}";

            

            ToFFmpeg(command);

        }


        /// <summary>
        /// add backgroung music to parts of code
        /// </summary>
        /// <param name="inputFiles"></param>
        public static string AddMusic(int i, params string[] inputFiles)
        {
            string filePathToCurrentTemporaryFile = $"{tempD}/temp_ads{i}.ts";
            string filePathToCurrentMusicFile = $"{tempD}/ads{i}.ts";


            string command = string.Empty;
            DirectoryInfo di = new DirectoryInfo("Background");
            //if directory exists
            if (di.Exists & !string.IsNullOrEmpty(BackgroundMusic))
            {
                //collect music files
                var files = di.GetFiles().Select(x => x.Name);
                //files count
                int backgroundFilesCount = files.Count();
                //if files more than 0
                if (backgroundFilesCount > 0)
                {
                    //choose music file 
                    int numRandomBFile = Program.r.Next(backgroundFilesCount);
                    var file_name = files.ElementAt(numRandomBFile);


                    //if file more than one - combine it with copy files
                    if (inputFiles.Count() > 1)
                    {
                        command = $"-i \"concat:{string.Join("|", inputFiles)}\" -vcodec copy -acodec copy -ar 48000 -y {filePathToCurrentTemporaryFile}";
                        ToFFmpeg(command);
                    }


                    TimeSpan durationMusicFile = Polly.Duration(System.IO.Path.Combine(di.FullName, file_name));

                    string file = inputFiles.Count() > 1 ? filePathToCurrentTemporaryFile : inputFiles[0];
                    //take duration of current files
                    TimeSpan durationCurrentFile = Polly.Duration(file);


                    if (durationCurrentFile > durationMusicFile)
                    {
                        int equation = (int)(durationCurrentFile.TotalSeconds / durationMusicFile.TotalSeconds);
                        //тогда нужно делать loop на весь видеофайл

                        string temp = $"Background/{file_name}";
                        System.Collections.Generic.List<string> srt = new System.Collections.Generic.List<string>();
                        for (int f = 0; f < equation + 1; f++)
                        {
                            srt.Add(temp);
                        }
                        temp = string.Join("|", srt);
                        command = $"-i \"concat:{temp}\" -af \"volume={BackgroundMusic}dB\" -y -ar 48000 {Program.tempD}/merged.mp3";
                        ToFFmpeg(command);
                        // -b:a 192k
                        command = $"-i {file} -i {Program.tempD}/merged.mp3 -codec:v copy -codec:a mp3 -ar 48000 -strict experimental -filter_complex \"amerge\" -shortest -y {preset}{filePathToCurrentMusicFile}";//-i \"concat:{concat}\" -vcodec copy -acodec copy {outputFilePath}";

                    }
                    else
                    {

                        command = $"-i Background/{file_name} -af \"volume={BackgroundMusic}dB\" -y {Program.tempD}/yy.mp3";//-i \"concat:{concat}\" -vcodec copy -acodec copy {outputFilePath}";
                        ToFFmpeg(command);
                        //-b:a 192k 
                        command = $"-i {file} -i {Program.tempD}/yy.mp3 -codec:v copy -codec:a mp3 -ar 48000 -strict experimental -filter_complex \"amerge\" -shortest -y {preset}{filePathToCurrentMusicFile}";//-i \"concat:{concat}\" -vcodec copy -acodec copy {outputFilePath}";

                    }


                    ToFFmpeg(command);
                }
            }

            return filePathToCurrentMusicFile;
        }

        public static void ConvertTSToMp4(string outputFilePath, string[] inputFilePath)
        {

            if (AppSettings.Ads != "N") outputFilePath = $"{Program.tempD}/tempResult.mp4";

            string temp2 = $"{Program.tempD}/Temp2.mp4";
            string concat = string.Empty;
            string command = string.Empty;
            if (mute)
            {
                for (int i = 0; i < inputFilePath.Length; i++) inputFilePath[i] = inputFilePath[i].Replace("\\", "/");

                concat = string.Join("|", inputFilePath);
                command = $"-i \"concat:{concat}\" -i {Program.tempD}/noend.mp3 -vcodec copy -acodec mp3 -y -shortest {preset}{Program.tempD}/yy.mp4";
            }
            else
            {

                for (int i = 0; i < inputFilePath.Length; i++) inputFilePath[i] = inputFilePath[i].Replace("\\", "/");

                concat = string.Join("|", inputFilePath);
                command = $"-i \"concat:{concat}\" -vcodec copy -acodec mp3 {preset}-y -ar 48000 {Program.tempD}/yy.mp4";//

            }

            ToFFmpeg(command);


            DirectoryInfo di = new DirectoryInfo("Background");
            if (di.Exists & !string.IsNullOrEmpty(BackgroundMusic))
            {


                if (CheckFile(System.IO.Path.Combine(Program.tempD, "yy.mp4")))
                {
                    var files = di.GetFiles().Select(x => x.Name);
                    int backgroundFilesCount = files.Count();
                    if (backgroundFilesCount > 0)
                    {
                        int numRandomBFile = Program.r.Next(backgroundFilesCount);
                        var file_name = files.ElementAt(numRandomBFile);


                        TimeSpan durationCurrentFile = TagLib.File.Create($"{Program.tempD}/yy.mp4").Properties.Duration;
                        TimeSpan durationMusicFile = TagLib.File.Create(System.IO.Path.Combine(di.FullName, file_name)).Properties.Duration;

                        if (durationCurrentFile > durationMusicFile)
                        {
                            int equation = (int)(durationCurrentFile.TotalSeconds / durationMusicFile.TotalSeconds);
                            //тогда нужно делать loop на весь видеофайл

                            string temp = $"Background/{file_name}";
                            System.Collections.Generic.List<string> srt = new System.Collections.Generic.List<string>();
                            for (int f = 0; f < equation + 1; f++)
                            {
                                srt.Add(temp);
                            }
                            temp = string.Join("|", srt);
                            command = $"-i \"concat:{temp}\" -af \"volume={BackgroundMusic}dB\" -y ar 48000 {Program.tempD}/merged.mp3";
                            ToFFmpeg(command);
                            // -b:a 192k
                            command = $"-i {Program.tempD}/yy.mp4 -i {Program.tempD}/merged.mp3 -codec:v copy -codec:a mp3 -ar 48000 -strict experimental -filter_complex \"amerge\" -shortest -y {preset}{temp2}";//-i \"concat:{concat}\" -vcodec copy -acodec copy {outputFilePath}";

                        }
                        else
                        {

                            command = $"-i Background/{file_name} -af \"volume={BackgroundMusic}dB\" -y {Program.tempD}/yy.mp3";//-i \"concat:{concat}\" -vcodec copy -acodec copy {outputFilePath}";
                            ToFFmpeg(command);
                            //-b:a 192k 
                            command = $"-i {Program.tempD}/yy.mp4 -i {Program.tempD}/yy.mp3 -codec:v copy -codec:a mp3 -ar 48000 -strict experimental -filter_complex \"amerge\" -shortest -y {preset}{temp2}";//-i \"concat:{concat}\" -vcodec copy -acodec copy {outputFilePath}";

                        }
                        ToFFmpeg(command);

                    }
                }
                else return;

                if (!CheckFile(temp2)) return;

                string temp3 = $"{Program.tempD}/Temp2.ts";
                command = $"-i {temp2} -y {temp3}";
                ToFFmpeg(command);

                List<string> list = new List<string>();
                list.Add(temp3);

                if (StartVideoIncluding) list = FFmpegMain.ViewCheck(StartVideoIncluding, Program.project.ToLower() + "_" + StartVideoFileName, list);
                if (EndVideoIncluding) list = FFmpegMain.ViewCheck(EndVideoIncluding, Program.project.ToLower() + "_" + EndVideoFileName, list);

                string result = string.Join("|", list);
                command = $"-i \"concat:{result}\" -y {preset}{outputFilePath}";

                ToFFmpeg(command);
            }
            else
            {
                command = $"-i \"concat:{concat}\" -y {preset}{outputFilePath}";//
                ToFFmpeg(command);
            }

            if (!CheckFile(outputFilePath)) return;
        }

        internal static void CreateVideoFromText(string filename, string text, string audioFilePath, string tempFilePath,string overlayPath)
        {
            StartFunction:
            //i++;
            TimeSpan audioFileDuration;
            TagLib.File file = TagLib.File.Create(audioFilePath);
            audioFileDuration = file.Properties.Duration;
            audioFileDuration += TimeSpan.FromMilliseconds(FFmpegAudioDurationOffset);

            #region old
            /*
            //-vf \"zoompan = z = 'if(lte(zoom,1.0),1.5,max(1.001,zoom-0.0015))':d = 125\"  перед yuv420p
            startInfo.Arguments = $"-loop 1 -i {imageFilePath} -i {audioFilePath} -c:v libx264 -tune stillimage -c:a aac -b:a 192k -pix_fmt yuv420p -shortest -t {audioFileDuration.ToString(@"hh\:mm\:ss\.fff")} -y {outputFilePath}";
            Process.Start(startInfo).WaitForExit();
            */
            #endregion

            DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(Path, tempD));
            int i = 0;

            //склейка видео
            TimeSpan overlayDuration = Polly.Duration($"{overlayPath}");

            //целый остаток от деления времени
            double total = (double)(audioFileDuration.TotalSeconds / overlayDuration.TotalSeconds);

            
            string overlayCopies = string.Empty;

            if (total < 1) overlayCopies = overlayPath;
            else
            {
                int overlay = (int)total + 1;
                for(int g=0;g<overlay;g++)
                {
                    if (g == overlay - 1)
                    {
                        overlayCopies = "concat:\"" + overlayCopies + overlayPath + "\"";
                    }
                    else overlayCopies += overlayPath + "|";
                }
            }

            //скорость текста на видео

            do
            {
                if (i == 1)
                {
                    var x = di.EnumerateFiles("*.ts");
                    foreach (var y in x) if (y.Exists) y.Delete();
                    goto StartFunction;
                }
                string TempTextFilepath = System.IO.Path.Combine(tempD,filename);
                int lines = Divide(text, filename);
                string textTopOffset = AppSettings.OverlayTextTopOffset;
                if(string.IsNullOrEmpty(textTopOffset))
                {
                    string equation = "h-#*t";
                    /*double*/
                    string linesMultiply = $"(text_h/{audioFileDuration.TotalSeconds})";//(lines + 30) * 0.85;
                    textTopOffset = equation.Replace("#", linesMultiply.ToString()).Replace(",", ".");
                }                
                string command = $"-i {overlayCopies} -i {audioFilePath} -loop 1 -filter_complex \"[0:v]drawbox={AppSettings.OverlayX}:{AppSettings.OverlayY}:{OverlayWidth}:{AppSettings.OverlayHeight}:{AppSettings.OverlayColor}@{AppSettings.OverlayTransparency}:t=fill[box];[box]drawtext=fontfile={AppSettings.OverlayFont}:textfile={TempTextFilepath.Replace("\\","/")}:fontcolor={AppSettings.OverlayFontColor}:fontsize={AppSettings.OverlayFontSize}:x={AppSettings.OverlayTextLeftOffset}:y={textTopOffset}[box1]\" -map \"[box1]\" -map 1:a:0 -acodec copy -y -t {audioFileDuration.ToString(@"hh\:mm\:ss\.fff")} {AppSettings.FFmpegPreset}{tempFilePath}";
                ToFFmpeg(command);
                i++;
            }
            while (!CheckFile(tempFilePath));
            Tell.ColoredMessage($"\r\nFile {tempFilePath} created\r\n", ConsoleColor.Green);

        }

        public static int Divide(string text,string filename)
        {
            string resultFilePath = $"{Program.tempD}/{filename}";
            
            List<string> result = new List<string>();

            string[] divided = text.Split(' ');

            int lines = 1;
            string temp = string.Empty;
            for(int i=0;i<divided.Count();i++)
            {
                //получить текущий
                string current = divided[i] + " ";
                //string next = i != divided.Count() - 1 ? divided[i + 1] : string.Empty;

                string temp1 = i != divided.Count() - 1 ? temp + current: temp + current;

                int f = 0;
                if (i  == divided.Count() - 1) f = 0;
                else f = divided[i + 1].Length;
                if (temp1.Length + f > AppSettings.OverlayTextWidth)
                {
                    result.Add(temp1.Replace(",", "\\,") + "\n");
                    temp = string.Empty;
                    temp1 = string.Empty;
                    lines++;
                }
                else temp = temp1;
                if (i == divided.Count() - 1)
                {
                    result.Add(temp.Replace(",", "\\,") + "\n");
                    lines++;
                }
            }
            
            System.IO.File.AppendAllLines(resultFilePath, result);
            return lines;
        }
    }
}