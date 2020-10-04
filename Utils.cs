using Leaf.xNet;
using System;

namespace B3RAP_Leecher_v3
{
    public enum LogType
    {
        Info = 0,
        Success = 1,
        Error = 2
    }

    public static class Utils
    {
        private static readonly ConsoleColor InfoColor = ConsoleColor.White;
        private static readonly ConsoleColor BadColor = ConsoleColor.Red;
        private static readonly ConsoleColor GoodColor = ConsoleColor.Green;

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

        public static HttpRequest CreateRequest(int timeout, int retries, ProxyClient client)
        {
            HttpRequest req = new HttpRequest()
            {
                UserAgent = Http.ChromeUserAgent(),
                EnableEncodingContent = false,
                IgnoreInvalidCookie = true,
                IgnoreProtocolErrors = true,
                UseCookies = false,
                ConnectTimeout = timeout,
                ReadWriteTimeout = timeout,
                AllowAutoRedirect = true,
                MaximumAutomaticRedirections = 10,
                Proxy = client,
                Reconnect = false
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
            else
            {
                message = message.Insert(0, "[-] ");
                message.ColorWriteLine(BadColor);
            }
        }
    }
}
