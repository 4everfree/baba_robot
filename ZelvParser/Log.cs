using System;
using System.IO;
using System.Text;

namespace ZelvParser
{
    class Log
    {

        public static void WriteError(string errorFileName, string text)
        {
            string path = Path.Combine(Program.Path, errorFileName);

            for (int q = 0; q < 5; q++)
            {
                try
                {
                    using (FileStream f = new FileStream(path, FileMode.Append, FileAccess.Write))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(text);
                        f.Write(info, 0, info.Length);
                        q = 6;
                    }
                }
                catch (Exception error)
                {
                    error = error.GetBaseException();
                    SendAndText("errorWriteErrorText", error);
                }
            }
        }

        public static void WriteError(string errorFileName, Exception e)
        {
            string error = e.GetBaseException().ToString();
            string text = "\r\n"+DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "\r\nError Message: " + e.Message + "\r\nError StackTrace: " + e.StackTrace + "\r\nError Source: " + e.Source + "\r\nError Base Exception: "+ error + "\r\n";
            string path = Path.Combine(Program.Path, errorFileName);
            for (int q = 0; q < 5; q++)
            {
                try
                {
                    using (FileStream f = new FileStream(path, FileMode.Append, FileAccess.Write))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(text);
                        f.Write(info, 0, info.Length);
                        q = 6;
                    }
                }
                catch (Exception error1)
                {
                    error1 = error1.GetBaseException();
                    SendAndText("errorWriteErrorIssue.txt", error1);
                }
            }
        }

        public static void WriteError(string errorFileName, Exception e, Issue issue)
        {
            e =e.GetBaseException();
            string i = issue.ToString();
            string text = DateTime.Now.ToLongDateString()
                + " "
                + DateTime.Now.ToLongTimeString()
                + "\n" + e.Message
                + "\n" + e.StackTrace
                + "\n" + e.Source
                + i;
            string path = Path.Combine(Program.Path, errorFileName);
            for (int q = 0; q < 5; q++)
            {
                try
                {
                    using (FileStream f = new FileStream(path, FileMode.Append, FileAccess.Write))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(text);
                        f.Write(info, 0, info.Length);
                        q = 6;
                    }
                }
                catch (Exception error)
                {
                    error = error.GetBaseException();
                    SendAndText("errorWriteErrorIssue.txt", error);
                }


            }
        }

        public static void WriteError(string errorFileName, Exception e, Book issue)
        {
            e = e.GetBaseException();
            string i = issue.ToString();
            string text = DateTime.Now.ToLongDateString()
                + " "
                + DateTime.Now.ToLongTimeString()
                + "\n" + e.Message
                + "\n" + e.StackTrace
                + "\n" + e.Source
                + i;
            string path = Path.Combine(Program.Path, errorFileName);
            for (int q = 0; q < 5; q++)
            {
                try
                {
                    using (FileStream f = new FileStream(path, FileMode.Append, FileAccess.Write))
                    {
                        byte[] info = new UTF8Encoding(true).GetBytes(text);
                        f.Write(info, 0, info.Length);
                        q = 6;
                    }
                }
                catch (Exception error)
                {
                    error = error.GetBaseException();
                    SendAndText("errorWriteErrorIssue.txt", error);
                }


            }
        }

        public static void SendAndText(string name, Exception e)
        {
            Exception r = e.GetBaseException();
                Log.WriteError(name, r);
                FFmpegMain.NotifyAnErrorViaTelegramBot(r.Message + r.Source);
        }
    }
}
