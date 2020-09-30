using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace B3RAP_Leecher_v3
{
    public static class Utils
    {
        private static Random random = new Random();
        public static string RandomString(int length)   //Thanks Stackoverflow
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Range(1, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}
