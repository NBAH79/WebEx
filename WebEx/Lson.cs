using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebEx
{
    public static class Lson
    {

        public const char DIVIDER = '|'; //U+007C vertical bar (односимвольный, меньше 128)

        public static string[] Parse(string text) => text.Split(DIVIDER);

        public static string ToLson(string[] strings)
        {
            var ret = strings[0];
            for (int n = 1; n < strings.Length; n++) ret += DIVIDER + strings[n];
            return ret;
        }

        public static string PairToString(string key, string value){
            return key+":"+value;
        }

        //надо раскодировать параметры евента аналогично стилю из html
        public static Dictionary<string, string> Pairs(string parameters)
        {
            if (string.IsNullOrEmpty(parameters)) return new();
            string[] parts = parameters.Split(';');
            Dictionary<string, string> ret = new();
            foreach (var p in parts)
            {
                int d = p.IndexOf(':');
                if (d <= 0) ret.TryAdd(p, "");
                else ret.TryAdd(p[..d], p[(d + 1)..]);
            }
            return ret;
        }

        //раскодировать параметры url, тут еще '?' есть и &
        public static Dictionary<string, string> Url(string url)
        {
            var parameters=url[(url.IndexOf('?')+1)..];
            if (string.IsNullOrEmpty(parameters)) return new();
            string[] parts = parameters.Split('&');
            Dictionary<string, string> ret = new();
            foreach (var p in parts)
            {
                int d = p.IndexOf('=');
                if (d <= 0) ret.TryAdd(p, "");
                else ret.TryAdd(p[..d], p[(d + 1)..]);
            }
            return ret;
        }

        public static (string,string) GetPair(string text,char symbol){
            int n = text.IndexOf(symbol);
            if (n <= 0) return (text, "");
            return (text[..n], text[(n + 1)..]);
        }

        //раскодировка класса из html
        public static string[] Classes(string classes)
        {
            string[] ret = classes.Split(' ');
            return ret;
        }
    }
}
