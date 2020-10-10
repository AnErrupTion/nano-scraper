using Leaf.xNet;
using System;
using System.IO;
using System.Threading;

namespace Nano_Scraper
{
    public enum LogType
    {
        Info = 0,
        Success = 1,
        Error = 2,
        Warning = 3
    }

    public static class Utils
    {
        private static readonly ConsoleColor InfoColor = ConsoleColor.White;
        private static readonly ConsoleColor BadColor = ConsoleColor.Red;
        private static readonly ConsoleColor GoodColor = ConsoleColor.Green;
        private static readonly ConsoleColor WarnColor = ConsoleColor.Yellow;

        public static void UpdateConsoleTitle()
        {
            Console.Title = $"Nano Scraper v{Program.Version} " +
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

        public static void RemoveDupes(string path, bool wait2Seconds)
        {
            var lines = File.ReadLines(path).Clean();
            File.WriteAllLines(path, lines);

            var text = $"Duplicates removed, you can safely close this window if you want to stop scraping now.";
            if (wait2Seconds) text += " Waiting 2 seconds before continuing.";

            Log(text, LogType.Info);
            if (wait2Seconds) Thread.Sleep(2000);
        }

        public static HttpRequest CreateRequest(int timeout, int retries, ProxyClient client)
        {
            HttpRequest req = new HttpRequest()
            {
                UserAgent = Http.RandomUserAgent(),
                IgnoreInvalidCookie = true,
                IgnoreProtocolErrors = true,
                ConnectTimeout = timeout,
                AllowAutoRedirect = true,
                MaximumAutomaticRedirections = 10,
                Proxy = client,
                Reconnect = false,
                UseCookies = true,
                Cookies = new CookieStorage()
            };

            if (retries > 0)
            {
                req.Reconnect = true;
                req.ReconnectDelay = timeout;
                req.ReconnectLimit = retries;
            }

            req.SslCertificateValidatorCallback += (sender, certificate, chain, sslPolicyErrors) => true;
            req.AddHeader("Accept", "*/*");

            return req;
        }

        public static void Log(string message, LogType type) => Log(message, (int)type);

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
            else if (type == 2)
            {
                message = message.Insert(0, "[-] ");
                message.ColorWriteLine(BadColor);
            }
            else if (type == 3)
            {
                message = message.Insert(0, "[/] ");
                message.ColorWriteLine(WarnColor);
            }
        }
    }
}
