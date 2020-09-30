using System.Collections.Generic;

namespace B3RAP_Leecher_v3
{
    public static class Arrays
    {
        public static string[] FastRemoveDupes(this IEnumerable<string> array)
        {
            HashSet<string> set = new HashSet<string>(array);
            string[] arr = new string[set.Count];
            set.CopyTo(arr);
            return arr;
        }

        public static List<string> Clean(this IEnumerable<string> array)
        {
            List<string> result = new List<string>();
            foreach (string line in array) if (!result.Contains(line) && line.Length > 5) result.Add(line);
            return result;
        }
    }
}
