using System.Collections.Generic;
using System.Linq;

namespace B3RAP_Leecher_v3
{
    public static class Arrays
    {
        public static HashSet<string> FastRemoveDupes(this IEnumerable<string> array)
        {
            return new HashSet<string>(array);
        }

        public static IEnumerable<string> Clean(this IEnumerable<string> array)
        {
            return array.FastRemoveDupes().Where(x
                => x.Length > 5
                && !x.EndsWith("...")
                && !x.EndsWith("..")
                && !x.EndsWith(".")
            );
        }
    }
}
