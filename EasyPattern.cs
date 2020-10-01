namespace B3RAP_Leecher_v3
{
    public static class EasyPattern
    {
        public static string ParseLink(string pattern)
        {
            return $"({pattern.Replace("/", @"\/")})";
        }
        
        public static string ParseToken(string str)
        {
            string final;
            if (str == "[a-Z]")
                final = "a-zA-Z";
            else
            {
                if (str != "[0-9]")
                {
                    string chara = $@"\{str.Split('[')[1]}";
                    final = $@"{chara.Remove(chara.Length - 1)}";
                }
                else final = "0-9";
            }
            return final;
        }

        public static string GetCorrectToken(string token)
        {
            return $"[{token}";
        }

        public static bool IsToken(string str)
        {
            return str.StartsWith("[") && str.EndsWith("]");
        }

        public static string Parse(string pattern)
        {
            if (pattern.Contains("http://") || pattern.Contains("https://"))
                return ParseLink(pattern);

            string[] tokens = pattern.Split('[');
            string final = "[";

            foreach (string toke in tokens)
            {
                string token = ParseToken(GetCorrectToken(toke));
                if (!string.IsNullOrEmpty(token))
                {
                    final += token;
                }
            }

            return final + "]+";
        }
    }
}
