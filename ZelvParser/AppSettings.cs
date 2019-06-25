using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


namespace ZelvParser
{
    /// <summary>
    /// Application settings from Config.ini
    /// </summary>
    static class AppSettings
    {
        
        static string filePath = new FileInfo("Config.ini").FullName;

        // DB
        public static string Database { get; }

        //Days
        public static int Parse { get; }
        public static long Days { get; }
        public static int RecordsCount { get; }
        public static DateTime DateTo { get; }
        public static DateTime DateFrom { get; }

        // Link
        public static string SitemapLink { get; }
        public static string TestUrl { get; }
        public static int SitemapNum {get;}
        public static string PartnerLink { get; }

        // Text
        public static int TextMinLength { get; }
        public static int TextMaxLength { get; }
        public static int TitleMaxLength { get; }
        public static string BlankData { get; }
        public static string StopWords { get; }

        public static int TextMinLimit { get; }
        public static int TextMaxLimit { get; }
        public static bool DirtyWords { get; }

        // Mode
        public static string Mode { get; }
        public static string Scraping { get; set; }
        public static int FromFile { get; set; }
        public static string Do { get; set; }

        public static int WithPictures { get; set; }




        //Threads
        public static int ParallelThreads { get; }
        public static bool Sleep { get; }
        public static int SleepFrom { get; }
        public static int SleepTo { get; }

        //Amazon
        public static string Api_id { get; }
        public static string Api_secret { get; }
        public static string SampleRate { get; }

        
        // Main
        public static bool CloseWindowAfterExecuting { get; }

        
        // Directories
        public static string Directories { get; set; }
        
        // Video
        public static int VideoScaleWidth { get; }
        public static int VideoScaleHeight { get; }
        public static byte[] VideoBackgroundColor { get; }
        public static bool StartVideoIncluding { get; }
        public static string StartVideoFileName { get; }
        public static bool EndVideoIncluding { get; }
        public static string EndVideoFileName { get; set; }
        public static bool UseTS { get; set; }
        public static string Effects { get; set; }
        public static string StartFadeIn { get; set; }
        public static string DurationFadeIn { get; set; }
        public static string DurationFadeOut { get; set; }
        public static string BackgroundMusic { get; set; }
        public static string VoiceVolume { get; set; }
        public static string  CreatePreview { get; set; }
        public static string  PreviewWithText { get; set; }
        public static int FontSize { get; set; }
        public static string FontFamily { get; set; }
        public static int StrokeWidth { get; set; }
        public static int Mute { get; set; }
        public static string FFmpegPreset { get; set; }
        public static string Ads { get; set; }
        public static int AdsPosition { get; set; }

        public static string VoiceoverLang { get; }

        public static string VoiceId { get; }
        public static string PlaybackSpeed { get; }
        public static string VideoTemplateName { get; }
        public static string OverlayX { get; }
        public static string OverlayY { get; }
        public static string OverlayWidth { get; }
        public static string OverlayHeight { get; }
        public static string OverlayColor { get; }
        public static string OverlayTransparency { get; }
        public static string OverlayFont { get; }
        public static string OverlayFontSize { get; }
        public static string OverlayFontColor { get; }
        public static string OverlayTextLeftOffset { get; }
        public static string OverlayTextTopOffset { get; }
        public static int OverlayTextWidth { get; }
        

        // FFmpegF
        public static bool HideFFmpegWidnow { get; }
        public static int FFmpegAudioDurationOffset { get; }

        // Telegram Bot
        public static bool EnableTelegramBotNotifications { get; }
        public static string TelegramBotApiToken { get; }
        public static int TelegramBotUserId { get; }

        //Templates
        public static string Template1 { get; }
        public static int StartAfter { get; }

        //Accounts
        public static int AccsCount { get; }
        static AppSettings()
        {

            Database = ReadValue("DB", "Database");
            
            SitemapLink = ReadValue("Link", "SitemapLink");
            SitemapNum = int.Parse(ReadValue("Link", "SitemapNum"));
            TestUrl = ReadValue("Link", "TestUrl");
            PartnerLink = ReadValue("Link", "PartnerLink");

            RecordsCount = int.Parse(ReadValue("Days", "RecordsCount"));
            Parse = int.Parse(ReadValue("Days", "Parse"));
            Days = long.Parse(ReadValue("Days", "Days"));
            DateTo = DateTime.Parse(ReadValue("Days", "DateTo"));
            DateFrom = DateTime.Parse(ReadValue("Days", "DateFrom"));

            TextMinLength = int.Parse(ReadValue("Text", "TextMinLength"));
            TextMaxLength = int.Parse(ReadValue("Text", "TextMaxLength"));
            TitleMaxLength = int.Parse(ReadValue("Text", "TitleMaxLength"));
            BlankData = ReadValue("Text", "BlankData");
            TextMinLimit = int.Parse(ReadValue("Text", "TextMinLimit"));
            TextMaxLimit = int.Parse(ReadValue("Text", "TextMaxLimit"));
            StopWords = ReadValue("Text", "StopWords");
            DirtyWords = ReadValue("Text", "DirtyWords") == "1";

            Scraping = ReadValue("Mode", "Scraping");
            Mode = ReadValue("Mode", "Mode");
            Do = ReadValue("Mode", "Do");
            FromFile = int.Parse(ReadValue("Mode", "FromFile"));
            WithPictures = int.Parse(ReadValue("Mode", "WithPictures"));

            ParallelThreads = int.Parse(ReadValue("Threads", "ParallelThreads"));
            Sleep = bool.Parse(ReadValue("Threads", "Sleep"));
            SleepFrom = int.Parse(ReadValue("Threads", "SleepFrom"));
            SleepTo = int.Parse(ReadValue("Threads", "SleepTo"));

            Api_id = ReadValue("Amazon", "Api_id");
            Api_secret = ReadValue("Amazon", "Api_secret");
            SampleRate = ReadValue("Amazon", "SampleRate");

            CloseWindowAfterExecuting = ReadValue("Main", nameof(CloseWindowAfterExecuting)) == "1";

            
            Directories = ReadValue("Directories", nameof(Directories));
            
            VideoScaleWidth = int.Parse(ReadValue("Video", "ScaleWidth"));
            VideoScaleHeight = int.Parse(ReadValue("Video", "ScaleHeight"));

            string[] parts = ReadValue("Video", "BackgroundColor").Split(new[] { ',' });
            VideoBackgroundColor = new[] { byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]) };

            StartVideoIncluding = ReadValue("Video", nameof(StartVideoIncluding)) == "1";
            StartVideoFileName = ReadValue("Video", nameof(StartVideoFileName));
            EndVideoIncluding = ReadValue("Video", nameof(EndVideoIncluding)) == "1";
            EndVideoFileName = ReadValue("Video", nameof(EndVideoFileName));
            UseTS = ReadValue("Video", nameof(UseTS)) == "1";
            Effects = ReadValue("Video", nameof(Effects));
            StartFadeIn = ReadValue("Video", nameof(StartFadeIn));
            DurationFadeIn = ReadValue("Video", nameof(DurationFadeIn));
            DurationFadeOut = ReadValue("Video", nameof(DurationFadeOut));
            VoiceVolume = ReadValue("Video", nameof(VoiceVolume));
            BackgroundMusic = ReadValue("Video", nameof(BackgroundMusic));
            CreatePreview = ReadValue("Video", nameof(CreatePreview));
            FontSize = int.Parse(ReadValue("Video", nameof(FontSize)));
            FontFamily = ReadValue("Video", nameof(FontFamily));
            FFmpegPreset = ReadValue("Video", nameof(FFmpegPreset));
            StrokeWidth = int.Parse(ReadValue("Video", nameof(StrokeWidth)));
            Mute = int.Parse(ReadValue("Video", nameof(Mute)));
            PreviewWithText = ReadValue("Video", nameof(PreviewWithText));
            Ads = ReadValue("Video", nameof(Ads));
            AdsPosition = int.Parse(ReadValue("Video", nameof(AdsPosition)));
            VoiceoverLang = ReadValue("Video", nameof(VoiceoverLang));
            VoiceId = ReadValue("Video", nameof(VoiceId));
            PlaybackSpeed = ReadValue("Video", nameof(PlaybackSpeed));
            VideoTemplateName = ReadValue("Video",nameof(VideoTemplateName));
            OverlayX = ReadValue("Video",nameof(OverlayX));
            OverlayY = ReadValue("Video",nameof(OverlayY));
            OverlayWidth = ReadValue("Video",nameof(OverlayWidth));
            OverlayHeight = ReadValue("Video",nameof(OverlayHeight));
            OverlayColor = ReadValue("Video",nameof(OverlayColor));
            OverlayTransparency = ReadValue("Video",nameof(OverlayTransparency));
            OverlayFont = ReadValue("Video",nameof(OverlayFont));
            OverlayFontSize = ReadValue("Video",nameof(OverlayFontSize));
            OverlayFontColor = ReadValue("Video",nameof(OverlayFontColor));
            OverlayTextLeftOffset = ReadValue("Video",nameof(OverlayTextLeftOffset));
            OverlayTextTopOffset = ReadValue("Video",nameof(OverlayTextTopOffset));
            OverlayTextWidth = int.Parse(ReadValue("Video",nameof(OverlayTextWidth)));
            

            HideFFmpegWidnow = ReadValue("FFmpeg", "HideWindow") == "1";
            FFmpegAudioDurationOffset = int.Parse(ReadValue("FFmpeg", "AudioDurationOffset"));

            EnableTelegramBotNotifications = ReadValue("TelegramBot", "EnableNotifications") == "1";
            TelegramBotApiToken = ReadValue("TelegramBot", "ApiToken");
            TelegramBotUserId = int.Parse(ReadValue("TelegramBot", "UserId"));

            Template1 = ReadValue("Templates", "Template1");
            StartAfter = int.Parse(ReadValue("Templates", "StartAfter"));

            AccsCount = int.Parse(ReadValue("Accounts", "AccsCount"));
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string section, string key, string defaultValue, StringBuilder value, int size, string filePath);

        static string ReadValue(string section, string key)
        {
            var value = new StringBuilder(byte.MaxValue);
            GetPrivateProfileString(section, key, "", value, value.Capacity, filePath);
            return value.ToString();
        }
    }
}
