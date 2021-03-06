using System;
using Leaf.xNet;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Data;
using System.Threading;
using System.Text;
using System.Collections.Generic;

namespace Nano_Scraper
{
    class Program
    {
        // Basic stuff
        private static IEnumerable<string> engines, websites, keywords;

        // Version of the scraper
        public static readonly string Version = "1.2";

        // Useful global variables for this class
        public static string engine, website, keyword;
        public static int errors, retry, linksScraped, resultScraped;
        public static string dateTimeFileName;

        // File parser used to parse the config
        private static readonly FileParser config = new FileParser()
        {
            Config = "settings/settings.txt",
            Separator = '=',
            Index = 1,
            RemoveSpaces = true,
            CommentChar = "#"
        };

        // Random instance
        private static readonly Random rand = new Random();

        // Settings
        private static string pattern, scrapingType, proxyType;
        private static int retries, timeout, writeErrorWaitTime;
        private static IEnumerable<string> proxies, customLinks;
        private static List<string> scrapedLinks = new List<string>();
        private static bool past24Hours, showErrors, logErrors, removeDupes, wait2Seconds, showCouldNotGetAnyResult, showDuplicateLinkCannotScrape;

        private static string path;
        private static readonly string logPath = "logs.txt";

        // Gets a random proxy, retursn null if it can't get one or there are no proxies.
        private static ProxyClient RandomProxy()
        {
            again: if (proxies == null || proxies.Count() == 0) return null;

            var proxy = proxies.ElementAt(rand.Next(proxies.Count()));

            ProxyType type = ProxyType.HTTP;
            if (proxyType == "http") type = ProxyType.HTTP;
            else if (proxyType == "socks4") type = ProxyType.Socks4;
            else if (proxyType == "socks5") type = ProxyType.Socks5;

            var result = ProxyClient.TryParse(type, proxy, out ProxyClient client);
            if (result) return client;
            else
            {
                var error = $"The proxy {proxy} is bad, trying a new one.";
                if (showErrors) Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                goto again;
            }
        }

        // The main method
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
                var error = "Could not read important files, exiting.";
                if (showErrors) Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                Thread.Sleep(2000);
                Environment.Exit(1);
            }

            try
            {
                pattern = config.ParseString("pattern");
                scrapingType = "custom";

                if (pattern.Contains("preset\""))
                {
                    var array = pattern.Split('"');
                    pattern = array[0];
                    scrapingType = array[1];
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
                showCouldNotGetAnyResult = config.ParseBoolean("show_could_not_get_any_result");
                showDuplicateLinkCannotScrape = config.ParseBoolean("show_duplicate_link_cannot_scrape");
            }
            catch
            {
                var error = "Failed to parse settings, exiting.";
                if (showErrors) Utils.Log(error, LogType.Error);
                if (logErrors) File.AppendAllText(logPath, error + Environment.NewLine);
                Thread.Sleep(2000);
                Environment.Exit(1);
            }

            var facts = new string[]
            {
                "Results auto-save, so you can close the window at any moment and still have your result!",
                "You can have multiple settings file. :O",
                "Nano Softwares was previously named B3RAP Softwares!",
                "B3RAP ProxyScrap (private) was the first program developed under the name B3RAP Softwares.",
                "AnErrupTion, the creator of Nano Softwares, loves privacy so much he has an XMPP account! (anerruption@disroot.org)",
                "This is just the beginning...",
                "This was made by human hands and feet! (sometimes)",
                "StackOverflow did help for the development of this!",
                "This is better than Slayer Leecher! ;)",
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
            Console.Title = "Starting scraper.";

            dateTimeFileName = DateTime.Now.ToString("yyyy.dd.M-HH.mm.ss");
            path = $"results/{scrapingType}-{dateTimeFileName}.txt";

            again: try
            {
                if (customLinks != null && customLinks.Count() > 0)
                    ScrapeResult(customLinks, null);

                else
                {
                    for (int i = 0; i < engines.Count(); i++)
                    {
                        var engine = engines.ElementAt(i);
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

                        Program.engine = engine;

                        for (int j = 0; j < websites.Count(); j++)
                        {
                            var website = websites.ElementAt(j);
                            Program.website = website;

                            for (int k = 0; k < keywords.Count(); k++)
                            {
                                var keyword = keywords.ElementAt(k);
                                Program.keyword = keyword;

                                retry = 1;
                                Scrape();
                            }
                        }
                    }

                    if (removeDupes && File.Exists(path) && linksScraped > 0 && resultScraped > 0)
                        Utils.RemoveDupes(path, wait2Seconds);
                }
            }
            catch (Exception ex)
            {
                if (logErrors) File.AppendAllText(logPath, ex.Message + Environment.NewLine);
                if (showErrors) Utils.Log(ex.Message, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        // This is where everything starts (scraping links)
        private static void Scrape()
        {
            again: try
            {
                Utils.UpdateConsoleTitle();

                if (removeDupes && File.Exists(path) && linksScraped > 0 && resultScraped > 0)
                    Utils.RemoveDupes(path, wait2Seconds);

                using var req = Utils.CreateRequest(timeout, retries, RandomProxy());

                var response = req.Get($"{engine}{keyword}+site:{website}").ToString();
                var matches = Regex.Matches(response, $@"https:\/\/{website}\/\w+");
                linksScraped = matches.Count;

                if (linksScraped > 0)
                {
                    var links = matches.Select(m => m.Value).FastRemoveDupes();
                    var count = links.Count();

                    Utils.Log($"Got {count} {(count == 1 ? "link" : "links")}, scraping result.", LogType.Info);
                    ScrapeResult(links, req);
                }
            }
            catch (Exception ex)
            {
                if (logErrors) File.AppendAllText(logPath, ex.Message + Environment.NewLine);
                if (showErrors) Utils.Log(ex.Message, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        // Scrapes results from links
        private static void ScrapeResult(IEnumerable<string> links, HttpRequest req)
        {
            again: try
            {
                if (req == null)
                    req = Utils.CreateRequest(timeout, retries, RandomProxy());

                for (int i = 0; i < links.Count(); i++)
                {
                    var link = links.ElementAt(i);
                    if (!scrapedLinks.Contains(link))
                    {
                        var response = req.Get(link).ToString().Replace("|", ":");
                        if (!response.Contains(":")) response = response.Replace(" ", ":");

                        if (link.Contains("anonfiles.com"))
                        {
                            var matches = Regex.Matches(response, @"https:\/\/.*.anonfiles.com\/.*");
                            if (matches.Count > 0)
                            {
                                var result = matches.Select(m => m.Value);
                                if (!string.IsNullOrEmpty(result.Last()))
                                {
                                    result.Append(string.Empty);
                                    result = result.Clean();

                                    for (int j = 0; j < result.Count(); j++)
                                    {
                                        var res = result.ElementAt(j);
                                        AppendResult(req.Get(res.Replace(">                    <img", string.Empty).Replace("\"", string.Empty)).ToString(), link);
                                    }
                                }
                            }
                        }
                        else AppendResult(response, link);
                    }
                    else
                    {
                        if (showDuplicateLinkCannotScrape)
                            Utils.Log("Link is duplicate, cannot scrape from it. - " + link, LogType.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                if (logErrors) File.AppendAllText(logPath, ex.Message + Environment.NewLine);
                if (showErrors) Utils.Log(ex.Message, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        // Detects the scraping type and associates the specific regex to append the result
        private static void AppendResult(string response, string link)
        {
            again: try
            {
                var regex = string.Empty;

                if (scrapingType == "emailpass")
                    regex = @"([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,6}):([a-zA-Z0-9_\-\.]+)";

                else if (scrapingType == "userpass")
                    regex = @"[a-z0-9_-]{3,16}:([a-zA-Z0-9_\-\.]+)";

                else if (scrapingType == "proxies")
                    regex = @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(?=[^\d])\s*:?\s*(\d{2,5})";

                else if (scrapingType == "emailonly")
                    regex = @"([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,6})";

                else if (scrapingType == "custom")
                    regex = pattern;

                if (!string.IsNullOrEmpty(regex))
                    GetResult(response, regex, link);
                else Utils.Log("Error, regex error beep ;-; owo :v", LogType.Warning);
            }
            catch (Exception ex)
            {
                if (logErrors) File.AppendAllText(logPath, ex.Message + Environment.NewLine);
                if (showErrors) Utils.Log(ex.Message, LogType.Error);

                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        // Finally appends the result to a file.
        private static void GetResult(string response, string regex, string link)
        {
            again: try
            {
                var matches = Regex.Matches(response, regex);
                resultScraped = matches.Count;

                if (resultScraped > 0)
                {
                    var result = matches.Select(m => m.Value);
                    if (!string.IsNullOrEmpty(result.Last()))
                    {
                        result.Append(string.Empty);

                        fileagain: try
                        {
                            File.AppendAllLines(path, result);
                            scrapedLinks.Add(link);
                            Utils.Log($"Scraped {result.Count()} {scrapingType} result. - {link}", LogType.Success);
                        }
                        catch
                        {
                            Utils.Log($"Unable to write to file {path}. Retrying in 3 seconds...", LogType.Error);
                            Thread.Sleep(writeErrorWaitTime);
                            goto fileagain;
                        }
                    }
                }
                else
                {
                    if (showCouldNotGetAnyResult)
                        Utils.Log($"Could not get any {scrapingType} result. - {link}", LogType.Error);
                }
            }
            catch (Exception ex)
            {
                if (logErrors) File.AppendAllText(logPath, ex.Message + Environment.NewLine);
                if (showErrors) Utils.Log(ex.Message, LogType.Error);

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
