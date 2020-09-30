using System;

namespace B3RAP_Leecher_v3
{
    public static class ConsoleUtils
    {
        public static ConsoleColor BadColor = ConsoleColor.Red;

        public static void UpdateConsoleTitle(this string str)
        {
            Console.Title = $"nαnσ scɾαρҽɾ v{Program.Version} | {str} " +
                $"| Engine : {Program.engine} " +
                $"| Website : {Program.website} " +
                $"| Keyword : {Program.keyword} " +
                $"| Errors : {Program.errors} " +
                $"| Retry : {Program.retry}";
        }

        public static void ColorWriteLine(this string msg, ConsoleColor color)
        {
            ConsoleColor reset = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = reset;
        }
    }
}
