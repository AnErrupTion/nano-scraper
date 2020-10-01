﻿using System;
using Leaf.xNet;
using System.IO;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Text;

namespace B3RAP_Leecher_v3
{
    class Program
    {
        // Basic stuff
        private static string[] engines, websites, keywords;

        // Version of the scraper
        public static readonly string Version = "3.0";

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
        private static int retries, timeout;
        private static string[] proxies, customLinks;
        private static bool past24Hours, showErrors;

        private static ProxyClient RandomProxy()
        {
            again: if (proxies == null || proxies.Length == 0) return null;

            string proxy = proxies[rand.Next(proxies.Length)];

            ProxyType type = ProxyType.HTTP;
            if (proxyType == "http") type = ProxyType.HTTP;
            else if (proxyType == "socks4") type = ProxyType.Socks4;
            else if (proxyType == "socks5") type = ProxyType.Socks5;

            bool result = ProxyClient.TryParse(type, proxy, out ProxyClient client);
            if (result) return client; else
            {
                $"The proxy {proxy} is bad, trying a new one...".ColorWriteLine(ConsoleUtils.BadColor);
                goto again;
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "Loading...";
            Console.OutputEncoding = Encoding.Unicode;

            try
            {
                engines = File.ReadAllLines($"settings/{nameof(engines)}.txt");
                websites = File.ReadAllLines($"settings/{nameof(websites)}.txt");
                keywords = File.ReadAllLines($"settings/{nameof(keywords)}.txt");
                config.ReadFile();
            }
            catch
            {
                "Could not read important files, exiting.".ColorWriteLine(ConsoleUtils.BadColor);
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
                    Console.WriteLine("PLEASE NOTE THAT EASY PATTERN DOESN'T WORK YET, SO PLEASE DON'T USE IT.");
                }
                retries = config.ParseInteger("retries");
                timeout = config.ParseInteger("timeout") * 1000;
                proxies = config.ParseStringArray("proxy_file");
                proxyType = config.ParseString("proxy_type").ToLower();
                customLinks = config.ParseStringArray("links_file");
                past24Hours = config.ParseBoolean("past_24_hours");
                showErrors = config.ParseBoolean("show_errors");
            }
            catch
            {
                "Failed to parse settings, exiting.".ColorWriteLine(ConsoleUtils.BadColor);
                Thread.Sleep(2000);
                Environment.Exit(1);
            }

            string[] facts = new string[]
            {
                "Results auto-save, so you can close the window at any moment and still have your result!",
                "You can have multiple settings file. :O",
                "nαnσ sσftɯαɾҽs was previously named B3RAP Softwares!",
                "B3RAP ProxyScrap (private) was the first program developed under the name B3RAP Softwares.",
                "AnErrupTion, the creator of nαnσ sσftɯαɾҽs, loves privacy so much he has an XMPP account! (anerruption@disroot.org)"
            };

            if (args.Length == 0 || args[0] != "--notips")
            {
                "Did you know?".ColorWriteLine(ConsoleColor.Cyan);
                facts[rand.Next(facts.Length)].ColorWriteLine(ConsoleColor.Yellow);
                Thread.Sleep(5000);
            }

            Console.WriteLine("Starting scraper...");
            fileInt = rand.Next();
            again: try
            {
                if (customLinks != null && customLinks.Length > 0) ScrapeResult(customLinks, null);
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
                if (showErrors) ("[ERROR] " + ex.Message).ColorWriteLine(ConsoleUtils.BadColor);
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
                using HttpRequest req = new HttpRequest
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
                    Proxy = RandomProxy()
                };

                if (retries > 0)
                {
                    req.Reconnect = true;
                    req.ReconnectDelay = timeout;
                    req.ReconnectLimit = retries;
                }

                req.SslCertificateValidatorCallback += (sender, certificate, chain, sslPolicyErrors) => true;
                req.AddHeader("Accept", "*/*");

                "Scraping links...".UpdateConsoleTitle();

                string response = req.Get($"{engine}{keyword}+site:{website}").ToString();
                MatchCollection regex = Regex.Matches(response, $@"(https:\/\/{website}\/\w+)");

                if (regex.Count > 0)
                {
                    string[] links = regex.OfType<Match>().Select(m => m.Value).FastRemoveDupes();
                    $"Got {links.Length} links, scraping result...".UpdateConsoleTitle();
                    ScrapeResult(links, req);
                }
            }
            catch (Exception ex)
            {
                if (showErrors) ("[ERROR] " + ex.Message).ColorWriteLine(ConsoleUtils.BadColor);
                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        private static void ScrapeResult(string[] links, HttpRequest req)
        {
            again: try
            {
                foreach (string link in links)
                {
                    string response = req.Get(link).ToString();
                    if (link.Contains("anonfiles.com"))
                    {
                        MatchCollection regex = Regex.Matches(response, @"(https:\/\/.*.anonfiles.com\/.*)");
                        if (regex.Count > 0)
                        {
                            List<string> result = regex.OfType<Match>().Select(m => m.Value).ToList();
                            if (!string.IsNullOrEmpty(result.Last()))
                            {
                                result.Add(string.Empty);
                                result = result.Clean();
                                foreach (string res in result) AppendResult(req.Get(res.Replace(">                    <img", string.Empty).Replace("\"", string.Empty)).ToString());
                            }
                        }
                    }
                    else AppendResult(response);
                }
            }
            catch (Exception ex)
            {
                if (showErrors) ("[ERROR] " + ex.Message).ColorWriteLine(ConsoleUtils.BadColor);
                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        private static void AppendResult(string response)
        {
            again: try
            {
                if (scrapingType == "emailpass") GetResult(response,
                @"([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5}):([a-zA-Z0-9_\-\.]+)", "combos");
                else if (scrapingType == "userpass") GetResult(response,
                    @"[a-z0-9_-]{3,16}:([a-zA-Z0-9_\-\.]+)", "combos");
                else if (scrapingType == "proxies") GetResult(response,
                    @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})(?=[^\d])\s*:?\s*(\d{2,5})", "proxies");
                else if (scrapingType == "emailonly") GetResult(response,
                    @"([a-zA-Z0-9_\-\.]+)@([a-zA-Z0-9_\-\.]+)\.([a-zA-Z]{2,5})", "emails");
                else if (scrapingType == "custom") GetResult(response, customRegex, "result");
            }
            catch (Exception ex)
            {
                if (showErrors) ("[ERROR] " + ex.Message).ColorWriteLine(ConsoleUtils.BadColor);
                errors++;
                if (retries > 0)
                    if (retry <= retries)
                    {
                        retry++;
                        goto again;
                    }
            }
        }

        private static void GetResult(string response, string regexx, string type)
        {
            again: try
            {
                MatchCollection regex = Regex.Matches(response, regexx);
                if (regex.Count > 0)
                {
                    List<string> result = regex.OfType<Match>().Select(m => m.Value).ToList();
                    if (!string.IsNullOrEmpty(result.Last()))
                    {
                        result.Add(string.Empty);

                        string text = string.Join("\n", result);
                        Console.WriteLine(text);

                        $"Scraped {result.Count} {type}".UpdateConsoleTitle();
                        fileagain: try
                        {
                            string path = $"results/{type}-{fileInt}.txt";
                            File.AppendAllLines(path, result);
                            List<string> lines = File.ReadAllLines(path).ToList();
                            File.WriteAllLines(path, lines.Clean().ToList());
                        }
                        catch
                        {
                            Thread.Sleep(3000);
                            goto fileagain; 
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (showErrors) ("[ERROR] " + ex.Message).ColorWriteLine(ConsoleUtils.BadColor);
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