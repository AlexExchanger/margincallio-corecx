using System;
using System.Text;
using System.Collections.Concurrent;

namespace CoreCX
{
    public static class Extensions
    {
        public static bool TryParseToBool(this string str, out bool result)
        {
            if (str == "0")
            {
                result = false;
                return true;
            }
            else if (str == "1")
            {
                result = true;
                return true;
            }
            else 
            {
                result = false;
                return false;
            }
        }

        public static int ToInt32(this bool val)
        {
            return val ? 1 : 0;
        }

        public static string Hexadecimal(this byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            T item;
            while (queue.TryDequeue(out item))
            {
                //do nothing
            }
        }
    }
}
