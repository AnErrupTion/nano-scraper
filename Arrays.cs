using System.Collections.Generic;
using System.Linq;

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

        public static IEnumerable<string> Clean(this IEnumerable<string> array)
        {
            return array.FastRemoveDupes().Where(x => x.Length > 5);
        }
    }
}
