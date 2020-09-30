using System.IO;
using System.Linq;

namespace B3RAP_Leecher_v3
{
    public class FileParser
    {
        public string Config { get; set; }

        public char Separator { get; set; }

        public bool RemoveSpaces { get; set; }

        public int Index { get; set; }

        private string[] configuration = new string[] { };

        public string CommentChar { get; set; }

        public string ParseString(string str)
        {
            if (configuration.Length > 0)
                foreach (string line in configuration)
                    if (!line.Contains(CommentChar) && line.Contains(str))
                    {
                        string value = line.Split(Separator)[Index];
                        return RemoveSpaces ? value.Replace(" ", string.Empty) : value;
                    }
            return null;
        }

        public int ParseInteger(string str)
        {
            bool success = int.TryParse(ParseString(str), out int result);
            if (success) return result;
            else return 0;
        }

        public bool ParseBoolean(string str)
        {
            bool success = bool.TryParse(ParseString(str), out bool result);
            if (success) return result;
            else return false;
        }

        public string[] ParseStringArray(string str)
        {
            string file = ParseString(str);
            if (File.Exists(file))
            {
                string[] array = File.ReadAllLines(file);
                if (array.Any()) return array;
            }
            return null;
        }

        public void ReadFile()
        {
            configuration = File.ReadAllLines(Config);
        }
    }
}
