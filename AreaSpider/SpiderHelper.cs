using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AreaSpider
{
    public class SpiderHelper
    {
        private const string INDEX_URL = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2013/index.html";

        private const string BASE_URL = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2013/";
        private const int HUNDRED = 100;
        private static Dictionary<string, Area> Areas = new Dictionary<string, Area>();

        public static Dictionary<string, SpiderThread> Threads = new Dictionary<string, SpiderThread>();
        private static int finished;
        private static readonly object locker = new object();
        private static readonly ConsoleColor back = Console.BackgroundColor;

        private static readonly IDictionary<Level, IList<string>> Failure = new Dictionary<Level, IList<string>>
        {
            {Level.Province, new List<string>()},
            {
                Level.City, new List<string>()
            },
            {Level.County, new List<string>()},
            {Level.Town, new List<string>()},
            {Level.Village, new List<string>()}
        };

        public static async Task<string> ExportCounties(string pcode, string ccode, string cocode, string url)
        {
            Areas = new Dictionary<string, Area>
            {
                {
                    pcode,
                    new Area
                    {
                        Sub = new Dictionary<string, Area>
                        {
                            {
                                ccode, new Area
                                {
                                    Sub = new Dictionary<string, Area>
                                    {
                                        {
                                            cocode, new Area
                                            {
                                                Sub = new Dictionary<string, Area>()
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            await LoadTowns(pcode, ccode, cocode, url);
            var sb = new StringBuilder();
            Write(sb, Areas[pcode]);
            return sb.ToString();
        }

        public static async Task<string> Export()
        {
            await LoadProvince();
            Console.WindowWidth = Console.LargestWindowWidth;
            Console.WindowHeight = Console.LargestWindowHeight;
            foreach (var item in Threads)
            {
                Console.WriteLine("{0}：", item.Value.Title);
                Console.BackgroundColor = ConsoleColor.Cyan;
                for (var j = 0; j < HUNDRED; j++)
                {
                    Console.Write(" ");
                }
                Console.WriteLine();
                Console.BackgroundColor = back;
                Console.WriteLine("Cities：");
                Console.BackgroundColor = ConsoleColor.Cyan;
                for (int j = 0; j < HUNDRED; j++)
                {
                    Console.Write(" ");
                }
                Console.WriteLine();
                Console.BackgroundColor = back;
                Console.WriteLine("Counties：");
                Console.BackgroundColor = ConsoleColor.Cyan;
                for (int j = 0; j < HUNDRED; j++)
                {
                    Console.Write(" ");
                }
                Console.WriteLine();
                Console.BackgroundColor = back;
            }
            Console.BackgroundColor = ConsoleColor.Yellow;
            foreach (var item in Threads)
            {
                item.Value.Thread.Start();
            }
            lock (locker)
            {
                while (Threads.Count > finished)
                {
                    Monitor.Wait(locker);
                }
            }
            var sb = new StringBuilder();
            foreach (var item in Areas)
            {
                Write(sb, item.Value);
            }
            var fail = new StringBuilder();
            foreach (var item in Failure)
            {
                fail.AppendFormat("{0}\n", item.Key);
                foreach (var url in item.Value)
                {
                    fail.AppendFormat("{0}\n", url);
                }
                fail.AppendFormat("\n");
            }
            File.WriteAllText(@"D:\failture.txt", fail.ToString());
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, Area area)
        {
            //sb.AppendFormat("{0}\t{1}\t{2}\t{3}\n", area.Title, area.Code, area.Url, area.ParentUrl);
            sb.AppendFormat("\"{0}\",\"'{1}\"", area.Title, area.Code);
            sb.AppendLine();
            if (area.Sub != null && area.Sub.Count > 0)
            {
                foreach (var item in area.Sub)
                {
                    Write(sb, item.Value);
                }
            }
        }

        private static async Task LoadProvince()
        {
            var html = await HttpHelper.GetAsync(INDEX_URL);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var province =
                doc.DocumentNode.SelectNodes(
                    "/html/body/table[2]/tbody/tr[1]/td/table/tbody/tr[2]/td/table/tbody/tr/td/table/tr[@class='provincetr']/td/a");
            var i = 0;
            foreach (var item in province)
            {
                var href = item.Attributes["href"].Value;
                var code = href.Substring(0, href.IndexOf('.'));
                var title = item.InnerText;
                var area = new Area
                {
                    Title = title,
                    Code = code,
                    ParentUrl = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2013/index.html",
                    Url = BASE_URL + href,
                    Sub = new Dictionary<string, Area>()
                };
                Areas.Add(code, area);
                var thread = new Thread(async () =>
                {
                    await LoadCities(code, area.Url);
                    lock (locker)
                    {
                        finished++;
                        Export(code);
                        Monitor.Pulse(locker);
                    }
                });
                var st = new SpiderThread {Title = title, Row = i, Thread = thread};
                Threads.Add(code, st);
                i += 6;
            }
        }

        private static void Export(string code)
        {
            var area = Areas[code];
            var sb = new StringBuilder();
            Write(sb, area);
            File.WriteAllText($@"D:\areas\{area.Title}.txt", sb.ToString());
        }

        private static async Task LoadCities(string parentCode, string url)
        {
            var html = await HttpHelper.GetAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var cities =
                doc.DocumentNode.SelectNodes(
                    "/html/body/table[2]/tbody/tr[1]/td/table/tbody/tr[2]/td/table/tbody/tr/td/table/tr[@class='citytr']/td[position()=2]/a");
            var i = 0;
            if (cities != null && cities.Count > 0)
                foreach (var item in cities)
                {
                    var href = item.Attributes["href"].Value;
                    var code = Regex.Match(href, @"\d+\/(\d+)\.html").Groups[1].Value;
                    var title = item.InnerText;
                    var area = new Area
                    {
                        Title = title,
                        Code = code,
                        ParentUrl = url,
                        Url = BASE_URL + href,
                        Sub = new Dictionary<string, Area>()
                    };
                    Areas[parentCode].Sub.Add(code, area);
                    try
                    {
                        if (code == "4419" || code == "4420")
                            await LoadTowns(parentCode, area.Code, area.Code + "00", area.Url);
                        else
                            await LoadCounties(parentCode, area.Code, area.Url);
                    }
                    catch (Exception)
                    {
                        Failure[Level.City].Add(area.Url);
                    }
                    lock (locker)
                    {
                        i++;
                        Console.BackgroundColor = ConsoleColor.Cyan;
                        for (int j = 0; j < HUNDRED; j++)
                        {
                            Console.SetCursorPosition(j, Threads[parentCode].Row + 3);
                            Console.Write(" ");
                        }
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        var length = Math.Floor((double) i/cities.Count*HUNDRED);
                        for (var j = 0; j < length; j++)
                        {
                            Console.SetCursorPosition(j, Threads[parentCode].Row + 1);
                            Console.Write(" ");
                        }
                        Console.BackgroundColor = back;
                    }
                }
            else
                Failure[Level.Province].Add(url);
        }

        private static async Task LoadCounties(string pcode, string ccode, string url)
        {
            var html = await HttpHelper.GetAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var counties =
                doc.DocumentNode.SelectNodes(
                    "/html/body/table[2]/tbody/tr[1]/td/table/tbody/tr[2]/td/table/tbody/tr/td/table/tr[@class='countytr']");

            var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
            var i = 0;
            if (counties != null && counties.Count > 0)
                foreach (var item in counties)
                {
                    var flag = false;
                    var a = item.SelectSingleNode("td[2]/a");
                    string href = string.Empty, code, title;
                    if (a == null)
                    {
                        flag = true;
                        code = item.SelectSingleNode("td[1]").InnerText.Substring(0, 6);
                        title = item.SelectSingleNode("td[2]").InnerText;
                    }
                    else
                    {
                        href = a.Attributes["href"].Value;
                        code = Regex.Match(href, @"\d+\/(\d+)\.html").Groups[1].Value;
                        title = a.InnerText;
                    }
                    var area = new Area
                    {
                        Title = title,
                        Code = code,
                        ParentUrl = url,
                        Url = baseUrl + href,
                        Sub = new Dictionary<string, Area>()
                    };
                    Areas[pcode].Sub[ccode].Sub.Add(code, area);
                    if (flag)
                        continue;
                    try
                    {
                        await LoadTowns(pcode, ccode, area.Code, area.Url);
                    }
                    catch (Exception)
                    {
                        Failure[Level.County].Add(area.Url);
                    }
                    lock (locker)
                    {
                        i++;
                        Console.BackgroundColor = ConsoleColor.Cyan;
                        for (int j = 0; j < HUNDRED; j++)
                        {
                            Console.SetCursorPosition(j, Threads[pcode].Row + 5);
                            Console.Write(" ");
                        }
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        var length = Math.Floor((double)i / counties.Count * HUNDRED);
                        for (int j = 0; j < length; j++)
                        {
                            Console.SetCursorPosition(j, Threads[pcode].Row + 3);
                            Console.Write(" ");
                        }
                        Console.BackgroundColor = back;
                    }
                }
            else
                Failure[Level.City].Add(url);
        }

        private static async Task LoadTowns(string pcode, string ccode, string cocode, string url)
        {
            string html;
            try
            {
                html = await HttpHelper.GetAsync(url);
            }
            catch (Exception)
            {
                Console.WriteLine(url);
                throw;
            }
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var towns =
                doc.DocumentNode.SelectNodes(
                    "/html/body/table[2]/tbody/tr[1]/td/table/tbody/tr[2]/td/table/tbody/tr/td/table/tr[@class='towntr']/td[position()=2]/a");
            var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
            var i = 0;
            if (towns != null && towns.Count > 0)
                foreach (var item in towns)
                {
                    var href = item.Attributes["href"].Value;
                    var code = Regex.Match(href, @"\d+\/(\d+)\.html").Groups[1].Value;
                    var title = item.InnerText;
                    var area = new Area
                    {
                        Title = title,
                        Code = code,
                        ParentUrl = url,
                        Url = baseUrl + href,
                        Sub = new Dictionary<string, Area>()
                    };
                    Areas[pcode].Sub[ccode].Sub[cocode].Sub.Add(code, area);
                    try
                    {
                        await LoadVillage(pcode, ccode, cocode, area.Code, area.Url);
                    }
                    catch (Exception)
                    {
                        Failure[Level.Town].Add(area.Url);
                    }
                    lock (locker)
                    {
                        i++;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        var length = Math.Floor((double)i / towns.Count * HUNDRED);
                        for (int j = 0; j < length; j++)
                        {
                            Console.SetCursorPosition(j, Threads[pcode].Row + 5);
                            Console.Write(" ");
                        }
                        Console.BackgroundColor = back;
                    }
                }
            else
                Failure[Level.County].Add(url);
        }

        private static async Task LoadVillage(string pcode, string ccode, string cocode, string tcode, string url)
        {
            var html = await HttpHelper.GetAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var village =
                doc.DocumentNode.SelectNodes(
                    "/html/body/table[2]/tbody/tr[1]/td/table/tbody/tr[2]/td/table/tbody/tr/td/table/tr[@class='villagetr']");
            if (village != null && village.Count > 0)
                foreach (var item in village)
                {
                    var code = item.SelectSingleNode("td[1]").InnerText;
                    var title = item.SelectSingleNode("td[3]").InnerText;
                    var area = new Area
                    {
                        Code = code,
                        Title = title,
                        ParentUrl = url
                    };
                    Areas[pcode].Sub[ccode].Sub[cocode].Sub[tcode].Sub.Add(code, area);
                }
            else
                Failure[Level.Village].Add(url);
        }
    }
}