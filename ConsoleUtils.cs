using System;

namespace B3RAP_Leecher_v3
{
    public static class ConsoleUtils
    {
        private static ConsoleColor InfoColor = ConsoleColor.White;
        private static ConsoleColor BadColor = ConsoleColor.Red;
        private static ConsoleColor GoodColor = ConsoleColor.Green;

        public static void UpdateConsoleTitle()
        {
            Console.Title = $"nαnσ scɾαρҽɾ v{Program.Version} " +
                $"| Engine : {Program.engine} " +
                $"| Website : {Program.website} " +
                $"| Keyword : {Program.keyword} " +
                $"| Errors : {Program.errors} " +
                $"| Retry : {Program.retry}";
        }

        private static void ColorWriteLine(this string msg, ConsoleColor color)
        {
            ConsoleColor reset = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = reset;
        }

        public static void Log(string message, int type)
        {
            if (type == 0)
            {
                message = message.Insert(0, "[*] ");
                message.ColorWriteLine(InfoColor);
            }
            else if (type == 1)
            {
                message = message.Insert(0, "[+] ");
                message.ColorWriteLine(GoodColor);
            }
            else
            {
                message = message.Insert(0, "[-] ");
                message.ColorWriteLine(BadColor);
            }
        }
    }
}
