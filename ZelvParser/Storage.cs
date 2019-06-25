using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ZelvParser
{
    public static class Storage
    {
        public static class Database
        {
            static String dbFileName = Program.project;
            static string dbpath = Path.Combine(Program.Path, dbFileName+".sqlite3");
            

            //проверка наличия таблицы бд
            //создаие таблицы бд
            public static void CheckFile()
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                { 
                    if (!File.Exists(dbpath))
                    {
                        SQLiteConnection.CreateFile(dbpath);
                    }
                }
            }


            public static void SendToCatalog()
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

                    ExecuteInsert("INSERT OR REPLACE INTO Catalog ('link','date') SELECT link,date FROM Current");
                    ExecuteInsert("DELETE FROM Current");

                }
            }


            public static Dictionary<string, string> GetForAudioValues(string table, string column, string link)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

                    Dictionary<string, string> dt = new Dictionary<string, string>();

                    string query = $"SELECT {column} FROM {table} where Link='{link}'";
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {
                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                try
                                {
                                    //если в аудио находится null то отправляем его в словарь
                                    if (string.IsNullOrEmpty(dtr["Audio"].ToString())) dt.Add(dtr["Link"].ToString(), dtr["TextParts"].ToString());
                                    else
                                    {
                                        //но если он не пуст, то разделяем и проверяем, есть ли такой файл
                                        string[] mp3s = dtr["Audio"].ToString().Split(';');

                                        foreach (string file in mp3s)
                                        {
                                            bool check = Polly.CheckMp3(file);
                                            if (!check && !dt.Keys.Contains(file))
                                            {
                                                try
                                                {
                                                    dt.Add(dtr["Link"].ToString(), dtr["TextParts"].ToString());
                                                }
                                                catch (Exception e)
                                                {
                                                    if (e.HResult == -2146232969)
                                                    {
                                                        Tell.ColoredMessage(dtr["Link"].ToString() + " уже добавлен", ConsoleColor.Blue);
                                                    }
                                                }
                                            }
                                        }

                                    }
                                }
                                catch (Exception e)
                                {
                                    Log.SendAndText("errorGetForAudioValues.txt", e);
                                }

                            }
                        }
                    }
                    Connection.Close();
                    return dt;
                }
            }

            public static string GetForAudioValue(string name)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    name = System.IO.Path.GetFileNameWithoutExtension(name);
                    string query = $"select textparts from records where textparts like '%{name}%'";
                    string result = string.Empty;
                    string temp = string.Empty;
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

                    using (SQLiteCommand com = new SQLiteCommand(query, Connection))
                    {
                        using (SQLiteDataReader dtr = com.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                temp = dtr["TextParts"].ToString();
                            }
                        }
                    }

                    Dictionary<string, string> dict = Deserialize.JsonToDictionaryString(temp);

                    foreach (var part in dict)
                    {
                        if (part.Key == name)
                        {
                            result = part.Value;
                            break;
                        }
                    }

                    return result;
                }
            }

            public static Dictionary<string, List<string>> GetMultiplyValues(string table, string[] rows)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    string column = string.Join(",", rows);


                    Dictionary<string, List<string>> dt = new Dictionary<string, List<string>>();

                    string query = $"SELECT {column} FROM {table}";
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {
                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                List<string> val = new List<string>();
                                string key = dtr[0].ToString();
                                val.Add(dtr[1].ToString());
                                val.Add(dtr[2].ToString());
                                val.Add(dtr[3].ToString());
                                dt.Add(key, val);
                            }
                        }
                    }
                
                    return dt;
                }
            }

            public static Dictionary<string, List<string>> GetMultiplyValues(string table, string[] rows, List<string> Links)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    string column = string.Join(",", rows);


                    Dictionary<string, List<string>> dt = new Dictionary<string, List<string>>();

                    foreach (string link in Links)
                    {
                        string query = $"SELECT {column} FROM {table} where Link='{link}'";
                        using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                        {
                            using (SQLiteDataReader dtr = Command.ExecuteReader())
                            {
                                while (dtr.Read())
                                {
                                    List<string> val = new List<string>();
                                    string key = dtr[0].ToString();
                                    val.Add(dtr[1].ToString());
                                    val.Add(dtr[2].ToString());
                                    val.Add(dtr[3].ToString());
                                    dt.Add(key, val);
                                }
                            }
                        }
                    }

                    return dt;
                }
            }

            public static List<string> GetPictures(string table, string[] rows, List<string> list)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    string column = string.Join(",", rows);
                    List<string> val = new List<string>();

                    string query = string.Empty;
                    foreach (string issue in list)
                    {
                        query = $"SELECT {column} FROM {table} Where Link='{issue}'";

                        using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                        {
                            using (SQLiteDataReader dtr = Command.ExecuteReader())
                            {

                                while (dtr.Read())
                                {
                                    val.Add(dtr[0].ToString());
                                }
                            }
                        }
                    }

                    return val;
                }
            }

            public static string CountRows(string table, string column = "", string value = "")
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    string add = "";
                    if (column != "") add = $" Where {column} is null";
                    string query = $"SELECT count(*) FROM {table}{add}";
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {

                        object result = Command.ExecuteScalar();
                        Connection.Close();
                        return (result == null ? "" : result.ToString());
                    }
                }
            }

            //есть ли это значение в базе
            public static bool HasValue(string table, string column, string value)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    string query = $"SELECT '{column}' FROM {table} WHERE {column}='{value}' LIMIT 1";
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {
                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                Connection.Close();
                                return true;
                            }
                            Connection.Close();
                            return false;
                        }
                    }
                }
            }

            //получить таблицу с указанной пустой строкой
            public static List<string> GetBlank(string table, string column, string value)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    List<string> list = new List<string>();

                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    string query = $"SELECT * FROM {table} WHERE {column}='{value}'";
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {
                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                list.Add(dtr["link"].ToString());
                            }
                        }
                    }
                    Connection.Close();
                    return list;
                }
            }

            public static void CreateTable()
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    string issuesT = string.Empty;
                    string badT = string.Empty;
                    if (AppSettings.Database.Contains("Labirint"))
                    {
                        issuesT = "CREATE TABLE IF NOT EXISTS Records (Date TEXT,Title TEXT,Text TEXT,TextLength TEXT,TextParts TEXT,TextPartsLength TEXT,TextPartsCount TEXT,Audio TEXT,Pictures TEXT,PicturesCount TEXT,PicturesList TEXT,Tags TEXT,VidName TEXT,Link TEXT UNIQUE,Created TEXT,CategoryName TEXT,Category TEXT)";
                        badT = "CREATE TABLE IF NOT EXISTS Bad (Link TEXT UNIQUE,Date TEXT)";
                    }
                    else issuesT = "CREATE TABLE IF NOT EXISTS Records (Date TEXT,Category TEXT,Title TEXT,Text TEXT,TextLength TEXT,TextParts TEXT,TextPartsLength TEXT,TextPartsCount TEXT,Audio TEXT,Pictures TEXT,PicturesCount TEXT,PicturesList TEXT,Tags TEXT,VidName TEXT,Link TEXT UNIQUE,Created TEXT)";
                    string currentT = "CREATE TABLE IF NOT EXISTS Current (Link TEXT UNIQUE,Date TEXT)";
                    string catalogT = "CREATE TABLE IF NOT EXISTS Catalog (Link TEXT UNIQUE,Date TEXT)";
                    string publishedT = "CREATE TABLE IF NOT EXISTS Published (Title TEXT UNIQUE)";

                    string[] queries = null;
                    if (AppSettings.Database.Contains("Labirint")) queries = new[] { issuesT, currentT, catalogT, publishedT, badT };
                    else queries = new[] { issuesT, currentT, catalogT, publishedT };
                    foreach (string q in queries)
                    {
                        if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                        using (SQLiteCommand Command = new SQLiteCommand(q, Connection))
                        {
                            Command.ExecuteNonQuery();
                        }
                    }
                }
            }


            public static List<string> GetLinkTable(string table, string column = "", string value = "")
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    List<string> result = new List<string>();
                    string add = "";
                    if (column != "") add = $" Where {column} is null Limit 100";

                    string query = $"SELECT link FROM {table}{add}";
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {

                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                result.Add(dtr["link"].ToString());
                            }
                        }
                        Command.Dispose();
                    }
                    return result;
                }
            }

            public static Dictionary<string, string> GetAllTable(string table, string column = "", string value = "")
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    string add = "";
                    if (column != "") add = $" Where {column} is null Limit {AppSettings.RecordsCount}";
                    else add = $" Limit {AppSettings.RecordsCount}";

                    string query = $"SELECT * FROM {table}{add}";
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {

                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                string tp = dtr["TextParts"].ToString();
                                result.Add(dtr["link"].ToString(), tp);
                            }
                        }
                        Command.Dispose();
                    }
                    return result;
                }
            }

            public static List<string> GetVidNameTable(string table)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    List<string> result = new List<string>();

                    string query = $"SELECT VidName FROM {table}";
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {

                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                result.Add(dtr["VidName"].ToString());
                            }
                        }
                        Command.Dispose();
                    }
                    return result;
                }
            }

            public static string VoidPictureDownload(string table,string column,string clause,string name)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    string query = $"select {column} from {table} where {clause} like '%{name}%'";
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    string result = string.Empty;
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {

                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                result = dtr[$"{column}"].ToString();
                            }
                        }
                        Command.Dispose();
                    }
                    return result;
                }
            }

            public static void WriteDataFull(List<string[]> urls)
            {
                List<string> result = new List<string>(urls.Count);
                foreach (string[] part in urls)
                {
                    string s = $"INSERT OR REPLACE INTO IF NOT EXISTS Catalog ('link','date') values ('{part[0]}','{part[1]}')";
                    result.Add(s);
                }
               ExecuteManyInserts(result);
            }


            public static void WriteDataFull(List<string> urls, string catalog)
            {
                List<string> result = new List<string>(urls.Count);
                foreach (string part in urls)
                {
                    string s = $"INSERT OR REPLACE INTO {catalog} ('link','date') values ('{part}','{DateTime.Now.ToShortDateString()}')";
                    result.Add(s);
                }
                ExecuteManyInserts(result);
            }

            public static List<string> DateListDB(string table)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    List<string> result = new List<string>();

                    string query = $"SELECT Date FROM {table}";
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {

                        using (SQLiteDataReader dtr = Command.ExecuteReader())
                        {
                            while (dtr.Read())
                            {
                                foreach (var x in dtr) result.Add(x.ToString());//dtr["Date"].ToString());
                            }
                        }
                        Command.Dispose();
                    }
                    result = result.Distinct().ToList();
                    return result;
                }
            }

            public static string RecordsDateCheck(string table, string date)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    string query = $"SELECT count(*) FROM {table} Where Date='{date}'";
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                    {

                        object result = Command.ExecuteScalar();
                        return (result == null ? "" : result.ToString());
                    }
                }
            }

            public static void WriteDataFullWithDate(List<string[]> urls, string table)
            {
                List<string> result = new List<string>();

                foreach (string[] part in urls)
                {
                    string s = $"INSERT INTO {table} ('link','date') values ('{part[0]}','{part[1]}')";
                    result.Add(s);
                }

                ExecuteManyInserts(result);
            }

            public static void CheckVideos(string table,List<string> Links)
            {
                #region old deleterow
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    List<string> result = new List<string>();
                    foreach (string link in Links)
                    {
                        string query = $"SELECT VidName FROM {table} where Link='{link}'";
                        if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                        using (SQLiteCommand Command = new SQLiteCommand(query, Connection))
                        {
                            using (SQLiteDataReader dtr = Command.ExecuteReader())
                            {
                                while (dtr.Read())
                                {
                                    string vidname = dtr["VidName"].ToString();
                                    if (string.IsNullOrEmpty(vidname))
                                    {
                                        try { result.Add(link); }
                                        catch { }

                                    }
                                }
                            }
                        }
                    }

                    List<string> commands = new List<string>(result.Count);
                    foreach (var x in result)
                    {
                        string s = $@"DELETE FROM Records WHERE Link = '{x}'";
                        commands.Add(s);
                    }

                    if (commands.Count > 0)
                    {
                        ExecuteManyInserts(commands);

                        File.AppendAllLines("novideo.txt", result);
                    }
                    #endregion
                }
            }


            public static void WriteData(Book issue)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

                    string query = $"INSERT OR IGNORE INTO Records ('Date','Category','Title','Text','TextLength','TextParts','TextPartsLength','TextPartsCount','Tags','Link','Pictures','PicturesCount','PicturesList','CategoryName')" +
                                        $"values ('{issue.Date}','{issue.Category}','{issue.Title}','{issue.Text}','{issue.TextLength}','{issue.TextParts}','{issue.TextPartsLength}','{issue.TextPartsCount}','{issue.Tags}','{issue.Link}','{issue.Pictures}','{issue.PicturesCount}','{issue.PicturesList}','{issue.CategoryName}')";

                    try
                    {
                        ExecuteInsert(query);
                    }
                    catch (Exception e)
                    {
                        Log.WriteError("errorWriteIssueError.txt", e, issue);
                        Log.SendAndText("errorWriteIssueError.txt", e);
                    }
                }
            }

            public static void Delete(string table,string column,string clause)
            {
                string s = $@"DELETE FROM {table} WHERE {column} LIKE '%{clause}%'";
                ExecuteInsert(s);
            }

            public static void WriteData(string table, string column, Dictionary<string, string> dict)
            {

                List<string> result = new List<string>(dict.Count);


                foreach (var c in dict)
                {
                    string key = c.Key;

                    string value = c.Value;
                    string s = $@"UPDATE {table} SET {column} = '{value}' WHERE Link = '{key}'";
                    result.Add(s);
                }

                ExecuteManyInserts(result);

            }

            public static void UpdateData(string table, string column, string value, string title)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                    string query = $"UPDATE {table} SET {column}='{value}' WHERE Title='{title}'";

                    ExecuteInsert(query);
                }
            }

            public static void WriteData(Issue issue)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();

                    string query = $"INSERT OR IGNORE INTO Records ('Date','Category','Title','Text','TextLength','TextParts','TextPartsLength','TextPartsCount','Tags','Link','Pictures','PicturesCount','PicturesList')" +
                                        $"values ('{issue.Date}','{issue.Category}','{issue.Title}','{issue.Text}','{issue.TextLength}','{issue.TextParts}','{issue.TextPartsLength}','{issue.TextPartsCount}','{issue.Tags}','{issue.Link}','{issue.Pictures}','{issue.PicturesCount}','{issue.PicturesList}')";

                    try
                    {
                        ExecuteInsert(query);
                    }
                    catch (Exception e)
                    {
                        Log.WriteError("errorWriteIssueError.txt", e, issue);
                        Log.SendAndText("errorWriteIssueError.txt", e);
                    }
                }
            }

            public static void ExecuteInsert(string command)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    int i = 0;
                    Database:
                    try
                    {
                        if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                        using (var cmd = new SQLiteCommand(Connection))
                        {
                            using (var transaction = Connection.BeginTransaction())
                            {
                                cmd.CommandText = $"{command}";
                                cmd.ExecuteNonQuery();
                                transaction.Commit();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        i++;
                        if (i == 10) return;
                        if (e.Message.Contains("Constraint")) { }
                        else goto Database;
                    }
                }
            }



            public static void ExecuteManyInserts(List<string> commands)
            {
                using (SQLiteConnection Connection = new SQLiteConnection($@"Data Source={dbpath};"))
                {
                    start:
                    try
                    {
                        if (Connection.State == System.Data.ConnectionState.Closed) Connection.Open();
                        using (var cmd = new SQLiteCommand(Connection))
                        {
                            using (var transaction = Connection.BeginTransaction())
                            {
                                foreach (string c in commands)
                                {
                                    cmd.CommandText = $"{c}";
                                    cmd.ExecuteNonQuery();
                                }
                                transaction.Commit();
                            }
                        }
                        Connection.Close();
                    }
                    catch (Exception e)
                    {
                        if (e.HResult == -2147473489) Tell.ColoredMessage("Попытка добавить существующие строки", ConsoleColor.Red);
                        if (e.Message == "database is locked\r\ndatabase is locked")
                        {
                            System.Threading.Thread.Sleep(30000);
                            goto start;
                        }
                    }
                }
            }
        }
    }
}
