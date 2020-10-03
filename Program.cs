using System;
using Leaf.xNet;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Data;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace B3RAP_Leecher_v3
{
    class Program
    {
        // Basic stuff
        private static IEnumerable<string> engines, websites, keywords;

        // Version of the scraper
        public static readonly string Version = "1.1";

        // Useful global variables for this class
        public static string engine, website, keyword;
        public static int errors, retry, fileInt;

        // File parser used to parse the config
        private static readonly FileParser config = new FileParser()
        {
            Config = "settings/settings.txt",
            Separator = ':',
            Index = 1,
            RemoveSpaces = true,
            CommentChar = "#"
        };

        // Random instance
        private static readonly Random rand = new Random();

        // Settings
        private static string pattern, scrapingType, customRegex, proxyType;
        private static int retries, timeout, writeErrorWaitTime;
        private static IEnumerable<string> proxies, customLinks;
        private static bool past24Hours, showErrors, logErrors, removeDupes, wait2Seconds;

        private static string path;
        private static readonly string logPath = "logs.txt";

        private static ProxyClient RandomProxy()
        {
            again: if (proxies == null || proxies.Count() == 0) return null;

            string proxy = proxies.ElementAt(rand.Next(proxies.Count()));

            ProxyType type = ProxyType.HTTP;
            if (proxyType == "http") type = ProxyType.HTTP;
            else if (proxyType == "socks4") type = ProxyType.Socks4;
            else if (proxyType == "socks5") type = ProxyType.Socks5;

            bool result = ProxyClient.TryParse(type, proxy, out ProxyClient client);
            if (result) return client;
            else
            {
                string error = $"The proxy {proxy} is bad, trying a new one...";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                goto again;
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "Loading...";
            Console.OutputEncoding = Encoding.Unicode;

            try
            {
                engines = File.ReadLines($"settings/{nameof(engines)}.txt");
                websites = File.ReadLines($"settings/{nameof(websites)}.txt");
                keywords = File.ReadLines($"settings/{nameof(keywords)}.txt");
                config.ReadFile();
            }
            catch
            {
                string error = "Could not read important files, exiting.";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                Thread.Sleep(2000);
                Environment.Exit(1);
            }

            try
            {
                pattern = config.ParseString("pattern");
                if (pattern.Contains("regex"))
                {
                    string[] array = pattern.Split('"');
                    pattern = array[0];
                    customRegex = array[1];
                }
                else if (pattern.Contains("preset"))
                {
                    string[] array = pattern.Split('"');
                    pattern = array[0];
                    scrapingType = array[1];
                }
                else
                {
                    pattern = EasyPattern.Parse(pattern);
                    Utils.Log("Please note that Easy Pattern doesn't work yet, " +
                        "so please don't use it.", LogType.Info);
                }
                retries = config.ParseInteger("retries");
                timeout = config.ParseInteger("timeout") * 1000;
                proxies = config.ParseStringArray("proxy_file");
                proxyType = config.ParseString("proxy_type").ToLower();
                customLinks = config.ParseStringArray("links_file");
                past24Hours = config.ParseBoolean("past_24_hours");
                showErrors = config.ParseBoolean("show_errors");
                logErrors = config.ParseBoolean("log_errors");
                writeErrorWaitTime = config.ParseInteger("write_error_wait_time") * 1000;
                removeDupes = config.ParseBoolean("remove_duplicates");
                wait2Seconds = config.ParseBoolean("wait_2_seconds_before_continue");
            }
            catch
            {
                string error = "Failed to parse settings, exiting.";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                Thread.Sleep(2000);
                Environment.Exit(1);
            }

            string[] facts = new string[]
            {
                "Results auto-save, so you can close the window at any moment and still have your result!",
                "You can have multiple settings file. :O",
                "nαnσ sσftɯαɾҽs was previously named B3RAP Softwares!",
                "B3RAP ProxyScrap (private) was the first program developed under the name B3RAP Softwares.",
                "AnErrupTion, the creator of nαnσ sσftɯαɾҽs, loves privacy so much he has an XMPP account! (anerruption@disroot.org)",
                "This is just the beginning...",
                "This was made by human hands and feet (sometimes)!",
                "StackOverflow did help for the development of this!",
                "This is better than Slayer Leecher ;)",
                "Simplicity is built-in into the program.",
                "The cake is a lie.",
                "The universe is inside a micro-organism living on another kind of creature."
            };

            if (args.Length == 0 || args[0] != "--notips")
            {
                Utils.Log($"Did you know?\n{facts[rand.Next(facts.Length)]}", LogType.Info);
                Thread.Sleep(3000);
            }

            Console.Clear();
            Console.Title = "Starting scraper...";

            fileInt = rand.Next();
            path = $"results/{scrapingType}-{fileInt}.txt";

            again: try
            {
                if (customLinks != null && customLinks.Count() > 0) ScrapeResult(customLinks, null);
                else
                    foreach (string engine in engines)
                        foreach (string website in websites)
                            foreach (string keyword in keywords)
                            {
                                Program.engine = engine;
                                Program.website = website;
                                Program.keyword = keyword;
                                retry = 1;
                                Scrape();
                            }
            }
            catch (Exception ex)
            {
                string error = $"{ex.Message}";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                if (showErrors) Utils.Log(error, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        private static void Scrape()
        {
            if (past24Hours)
            {
                if (engine.Contains("bing")) engine = "https://www.bing.com/search?filters=ex1%3a%22ez1%22&q=";
                else if (engine.Contains("yahoo")) engine = "https://search.yahoo.com/search?age=1d&btf=d&q=";
                else if (engine.Contains("yandex")) engine = "https://yandex.com/search/?within=77&text=";
                else if (engine.Contains("google")) engine = "https://www.google.com/search?tbs=qdr:d&q=";
                else if (engine.Contains("duckduckgo")) engine = "https://duckduckgo.com/?df=d&ia=web&q=";
                else if (engine.Contains("aol")) engine = "https://search.aol.com/aol/search?age=1d&btf=d&q=";
                else if (engine.Contains("rambler")) engine = "https://nova.rambler.ru/search?period=day&query=";
            }

            again: try
            {
                Utils.UpdateConsoleTitle();

                if (removeDupes && File.Exists(path))
                {
                    Utils.Log("Removing duplicates...", LogType.Info);
                    var lines = File.ReadLines(path).Clean();
                    File.WriteAllLines(path, lines);

                    string text = $"Duplicates removed, you can safely close this window if you want to stop scraping now.";
                    if (wait2Seconds)
                    {
                        text += " Waiting 2 seconds before continuing...";
                        Utils.Log(text, LogType.Success);
                        Thread.Sleep(2000);
                    }
                    else Utils.Log(text, LogType.Success);
                }

                using var req = Utils.CreateRequest(timeout, retries, RandomProxy());
                Utils.Log("Scraping links...", LogType.Info);

                var response = req.Get($"{engine}{keyword}+site:{website}").GetString();
                var regex = Regex.Matches(response, $@"(https:\/\/{website}\/\w+)");

                if (regex.Count > 0)
                {
                    var links = regex.OfType<Match>().Select(m => m.Value).FastRemoveDupes();
                    Utils.Log($"Got {links.Count()} links, scraping result...", LogType.Info);
                    ScrapeResult(links, req);
                }
            }
            catch (Exception ex)
            {
                string error = $"{ex.Message}";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                if (showErrors) Utils.Log(error, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        private static void ScrapeResult(IEnumerable<string> links, HttpRequest req)
        {
            again: try
            {
                if (req == null) req = Utils.CreateRequest(timeout, retries, RandomProxy());
                foreach (var link in links)
                {
                    var response = req.Get(link).GetString();
                    if (link.Contains("anonfiles.com"))
                    {
                        var regex = Regex.Matches(response, @"(https:\/\/.*.anonfiles.com\/.*)");
                        if (regex.Count > 0)
                        {
                            var result = regex.OfType<Match>().Select(m => m.Value).ToList();
                            if (!string.IsNullOrEmpty(result.Last()))
                            {
                                result.Add(string.Empty);
                                foreach (var res in result.Clean())
                                {
                                    var resp = req.Get(res.Replace(
                                        ">                    <img", string.Empty)
                                        .Replace("\"", string.Empty)).GetString();
                                    AppendResult(resp, link);
                                }
                            }
                        }
                    }
                    else AppendResult(response, link);
                }
                req.Dispose();
            }
            catch (Exception ex)
            {
                string error = $"{ex.Message}";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                if (showErrors) Utils.Log(error, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        private static void AppendResult(string response, string link)
        {
            again: try
            {
                if (scrapingType == "emailpass")
                {
                    GetResult(response,
                @"([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,6}):([a-zA-Z0-9_\-\.]+)",
                "combos", link);
                }
                else if (scrapingType == "userpass") GetResult(response,
                    @"[a-z0-9_-]{3,16}:([a-zA-Z0-9_\-\.]+)", "combos", link);
                else if (scrapingType == "proxies") GetResult(response,
                    @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(?=[^\d])\s*:?\s*(\d{2,5})", "proxies", link);
                else if (scrapingType == "emailonly") GetResult(response,
                    @"([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,6})", "emails", link);
                else if (scrapingType == "custom") GetResult(response, customRegex, "result", link);
            }
            catch (Exception ex)
            {
                string error = $"{ex.Message}";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                if (showErrors) Utils.Log(error, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        private static void GetResult(string response, string regexx, string type, string link)
        {
            again: try
            {
                MatchCollection regex = Regex.Matches(response, regexx);
                if (regex.Count > 0)
                {
                    var result = regex.OfType<Match>().Select(m => m.Value).ToList();
                    if (!string.IsNullOrEmpty(result.Last()))
                    {
                        result.Add(string.Empty);

                        fileagain: try
                        {
                            File.AppendAllLines(path, result);
                            Utils.Log($"Scraped {result.Count - 1} {type} - {link}", LogType.Success);
                        }
                        catch
                        {
                            Utils.Log($"Unable to write to file {path}. Retrying in 3 seconds...", LogType.Error);
                            Thread.Sleep(writeErrorWaitTime);
                            goto fileagain;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                string error = $"{ex.Message}";
                Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                if (showErrors) Utils.Log(error, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }
    }
}
