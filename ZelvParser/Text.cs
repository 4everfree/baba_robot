using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZelvParser
{
    public static class Text
    {
        //обработка текста перед разбиением
        public static List<string> Splitter(string s_targ, int pictCount)
        {
            //длина всего текста
            int i_len = s_targ.Length;

            //количество частей текста
            int i_part = i_len / pictCount;

            //минимальный кусок текста
            int s_min = AppSettings.TextMinLimit;

            //максимальный кусок текста
            int s_max = AppSettings.TextMaxLimit;

            //проверка чтобы минимальная часть была больше минимального куска текста 
            if (i_part > s_min) i_part = s_min;

            //список куда сохраняются части текста
            List<string> output = new List<string>();

            //начальная позиция символа
            int i_start = 0;
            //позиция символа на которой происходит остановка
            int i_stop = 0;

            //цикл разрезания текста
            //for(int n =0;n<pictCount;n++)
            while (i_start < i_len)
            {
                if (i_start + i_part > i_len)
                {
                    output.Add(s_targ.Substring(i_start, i_len - i_start));
                    break;
                }
                else if(i_len < s_min)
                {
                    output.Add(s_targ.Substring(i_start, i_len - i_start));
                    break;
                }
                else
                {
                    i_stop = s_targ.IndexOf(" ", i_start + i_part);
                    if (i_stop == 0) i_stop = i_start + i_part;
                    if(i_stop==-1)
                    {
                        output.Add(s_targ.Substring(i_start, i_len-i_start));
                        break;
                    }
                    else
                    //if(i_stop - i_start > s_max )  
                    output.Add(s_targ.Substring(i_start, i_stop - i_start + 1));
                }
                i_start = i_stop + 1;
            }
            return output;
        }

        public static Dictionary<string,string> SplitterDot(string s_targ)
        {
            //список куда сохраняются части текста
            Dictionary<string,string> output = new Dictionary<string,string>();

            var textArray = s_targ.Split('.').ToList<string>();

            Dictionary<string, int> dict = new Dictionary<string, int>();

            //отфильтровывание маленьких кусочков
            foreach (string part in textArray)
            {
                string particle = part.Trim();
                bool text = int.TryParse(particle, out int res1);
                if(!text) if (particle.Length > 1) if (!dict.ContainsKey(particle)) dict.Add(particle, particle.Length);
            }


            //временный список
            List<string> res = new List<string>();
            int i = 0;
            if(AppSettings.Ads=="F"&&AppSettings.AdsPosition==1)
            {
                i = 1;
                output.Add(Guid.NewGuid().ToString(), dict.ElementAt(0).Key);
            }
            int counter = 0;
            //цикл отбора
            for (; i < dict.Count; i++)
            {
                //взять блок.
                var tp = dict.ElementAt(i);

                res.Add(tp.Key + ".");
                counter = counter + tp.Value;


                if (i + 1 < dict.Count)
                {
                    var next_tp = dict.ElementAt(i + 1);

                    if (counter + next_tp.Value > AppSettings.TextMinLimit)
                    {
                        counter = 0;
                        output.Add(Guid.NewGuid().ToString(), string.Join(" ", res));
                        res.Clear();
                    }
                }
                else
                {
                    output.Add(Guid.NewGuid().ToString(), string.Join(" ", res));
                }

            }
            return output;
        }


        //разбиение картинок он передает значения количества частей текста и названия фоток
        public static List<string> PicturesCount(int textCount, List<string> pictures)
        {
            List<string> output = new List<string>();
            //вначале нужно узнать количество фото к статье.
            int count = pictures.Count;

            
            //если 1 текст = 1 фото
            if (textCount == count)
            {
                foreach (string row in pictures)  output.Add(row);
            }

            //если фото больше чем текстов, то использовать цикл присваивания 1й части текста 1го фото.
            if (textCount > count)
            {
                for (int i = 0, j = 0; i < textCount; i++, j++)
                {
                    if (j == pictures.Count) j = 0;
                    output.Add(pictures[j]);
                }
            }

            //если меньше - то цикл перебора по текстам а затем по фото.
            if (textCount < count)
            {
                for (int i = 0, j = 0; i < textCount; i++, j++) output.Add(pictures[j]);
            }

            return output;
        }

        //частотность слов
        public static string Frequency(string text)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            text = text.ToLower();
            string result = RemovePunctuation(text);
            List<string> txt_arr = Cleanup(result);
            
            try
            { 
            for (int i = 0; i < txt_arr.Count; i++)
            {
                string[] s_t = new string[3];
                s_t[0] = txt_arr[i].Trim();
                if (i < txt_arr.Count) s_t[1] = $"{s_t[0]} {txt_arr[i + 1].Trim()}";
                    if (i + 1 < txt_arr.Count - 1) s_t[2] = $"{s_t[1]} {txt_arr[i + 2].Trim()}";
                    else break;
                foreach (string row in s_t)
                {
                    if(!string.IsNullOrEmpty(row))
                    if (row.Length > 3)
                    {
                        if (dict.Keys.Contains(row)) dict[row] = dict[row] + 1;
                        else dict.Add(row, 1);
                    }
                }
            }
            }
            catch
            {

            }
            dict = SortedDictionaryByValue(dict);


            string temp = string.Join(", ", dict.Keys);
            temp = temp.Substring(0, 490);
            string[] separator = { ", " };
            List<string> f = temp.Substring(0,temp.LastIndexOf(',')).Split(separator, StringSplitOptions.None).ToList();
            f = (List<string>)f.Distinct().ToList();
            
            return string.Join(", ",f);
        }

        static Dictionary<string, int> SortedDictionaryByValue(Dictionary<string, int> dict)
        {

            ArrayList arrayList = new ArrayList();
            Dictionary<int, List<string>> dictTemp = new Dictionary<int, List<string>>();

            foreach (var row in dict)
            {
                List<string> l = new List<string>();
                if (!dictTemp.Keys.Contains(row.Value))
                {
                    dictTemp.Add(row.Value, l);
                    arrayList.Add(row.Value);
                }

                dictTemp[row.Value].Add(row.Key);
            }

            arrayList.Sort();
            arrayList.Reverse();

            dict.Clear();

            foreach (int value in arrayList)
            {
                List<string> l = new List<string>();
                l = dictTemp[value];

                foreach (var item in l) dict.Add(item, value);
            }

            return dict;
        }

        static string RemovePunctuation(string text)
        {
            text = text.Replace("-", " ").Replace("  ", " ").Replace("«", "").Replace("»", "");
            //text = System.Text.RegularExpressions.Regex.Replace(text, @"[^а-я0-9 ]","");
            return text;
        }

        static List<string> Cleanup(string text)
        {

            //получить маску
            string msk = string.Join(" ",File.ReadAllLines(AppSettings.StopWords));
   
            //сделать пробелы в маске
            msk = $" {msk} ";

            string[] txt_arr = text.Split(' ');

            List<string> output = new List<string>();

            foreach (string row in txt_arr)
                if (row.Length > 2) if (!msk.Contains(row)) output.Add(row.Replace(",","").Replace("?","").Replace(".",""));

            return output;
        }

        public static string DirtyWords(string text,bool type)
        {
            string filepath = System.IO.Path.Combine(Program.Path, "dirtywords.txt"); 
            if (!File.Exists(filepath)) return text;
            string[] words = File.ReadAllLines(filepath);

            List<string> list = new List<string>();

            foreach (string line in words)
            {
                string temp = line;
                temp = type ? line.ToLower() : line.ToUpper();
                string[] pair = temp.Split(':');
                

                bool r = System.Text.RegularExpressions.Regex.IsMatch(text,$@"\b{pair[0]}\b");
                
                if (r)
                {
                    text = text.Replace($"{pair[0]}",$"{pair[1]}");
                }
            }
            

            return text;
        }
    }
}
