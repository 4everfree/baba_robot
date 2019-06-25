using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ZelvParser
{
    /// <summary>
    /// class wrapper for newtonsoft deserialization methods
    /// </summary>
    class Deserialize
    {
        /// <summary>
        /// get json string from database and return list of pictures, based on class Pictures 
        /// </summary>
        /// <param name="json">json string from database</param>
        /// <returns>list of pictures saved in database</returns>
        public static List<Picture> JsonToListPicture(string json)
        {
            var ja = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(json);
            return ja.ToObject<List<Picture>>();
        }
        /// <summary>
        /// get json string from database and return list of strings, based on class Pictures 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static List<string> JsonToListString(string json)
        {
            var ja = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(json);
            return ja.ToObject<List<string>>();
        }
        /// <summary>
        /// get json string from database and return dictionary of key,value pair of strings 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Dictionary<string, string> JsonToDictionaryString(string json)
        {
            var ja = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(json);
            return ja.ToObject<Dictionary<string,string>>();
        }

    }
}
