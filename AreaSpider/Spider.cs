using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AreaSpider
{
    public static class Spider
    {
        private static readonly IList<Area> Areas = new List<Area>();

        private const string XPATH =
            "/html/body/table[2]/tbody/tr[1]/td/table/tbody/tr[2]/td/table/tbody/tr/td/table/tr";

        private const string INDEX_URL = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2013/index.html";

        private static readonly string[] Classes = { "provincetr", "citytr", "countytr", "towntr", "villagetr" };

        private static readonly Dictionary<string, SpiderThread> Threads = new Dictionary<string, SpiderThread>();
        private static readonly ConsoleColor Back = Console.BackgroundColor;
        private static readonly object Locker = new object();
        private static int Finished = 0;

        public static async Task Load()
        {
            await ParseHtml(INDEX_URL, Level.Province);
            lock (Locker)
            {
                while (Threads.Count > Finished)
                {
                    Monitor.Wait(Locker);
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (var item in Areas)
            {
                sb.Append(item);
            }
            File.WriteAllText(@"D:\areas.txt", sb.ToString());
        }

        private static void GenerateProgress(string title)
        {
            Console.WriteLine(title);
            for (int i = 0; i < 3; i++)
            {
                Console.BackgroundColor = ConsoleColor.Cyan;
                for (int j = 0; j < 40; j++)
                {
                    Console.Write(" ");
                }
                Console.BackgroundColor = Back;
                Console.Write(" ");
            }
            Console.BackgroundColor = Back;
            Console.WriteLine();
        }

        private static void UpdateProgress(int row, int start, int end, ConsoleColor color)
        {
            while (start < end)
            {
                Console.BackgroundColor = color;
                Console.SetCursorPosition(start, row);
                Console.Write(" ");
                start++;
            }
        }

        private static async Task ParseHtml(string url, Level level)
        {
            if (url == null)
                return;
            var html = await HttpHelper.GetAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var i = 0;
            var nodes = doc.DocumentNode.SelectNodes(XPATH).Where(d => d.HasAttributes && Classes.Any(r => r == d.Attributes["class"].Value));
            switch (level)
            {
                case Level.Province:
                    ParseProvince(nodes);
                    break;
                case Level.City:
                    await ParseCommon(url, nodes, level);
                    break;
            }
        }

        private static void ParseProvince(IEnumerable<HtmlNode> nodes)
        {
            int row = 0;
            foreach (var node in nodes)
            {
                var children = node.SelectNodes("td/a");
                foreach (var child in children)
                {
                    var href = child.Attributes["href"].Value;
                    var code = href.Substring(0, href.LastIndexOf('.'));
                    var title = child.InnerText;
                    var area = new Area
                    {
                        Title = title,
                        Code = code,
                        ParentUrl = INDEX_URL,
                        Url = INDEX_URL.Substring(0, INDEX_URL.LastIndexOf('/') + 1) + href
                    };
                    Areas.Add(area);
                    var thread = new SpiderThread
                    {
                        Row = row + 1,
                        Title = title,
                        Thread = new Thread(async () =>
                        {
                            await ParseHtml(area.Url, Level.City);
                            lock (Locker)
                            {
                                Finished++;
                                File.WriteAllText($@"D:\areas\{title}.txt", area.ToString(), Encoding.UTF8);
                                Monitor.Pulse(Locker);
                            }
                        })
                    };
                    thread.Thread.Start();
                    Threads.Add(code, thread);
                    GenerateProgress(title);
                    row += 2;
                    break;
                }
                break;
            }
        }

        private static async Task ParseCommon(string url, IEnumerable<HtmlNode> nodes, Level level)
        {
            var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
            int index = 1;
            foreach (var node in nodes)
            {
                var td = node.SelectNodes("td");
                string href = null;
                string code = string.Empty;
                string title = string.Empty;
                if (td[0].HasChildNodes)
                {
                    href = td[0].SelectSingleNode("a").Attributes["href"].Value;
                    code = Regex.Match(href, @"\d+\/(\d+)\.html").Groups[1].Value;
                    title = td[1].SelectSingleNode("a").InnerText;
                }
                else
                {
                    code = td[0].InnerText;
                    title = td[1].InnerText;
                }
                var area = new Area
                {
                    Code = code,
                    Title = title,
                    ParentUrl = url,
                    Url = href != null ? baseUrl + href : null
                };
                string province = code.Substring(0, 2);
                Areas.Single(d => d.Code == province).Add(area, level);
                int row = Threads[province].Row;
                if (level == Level.City)
                {
                    var thread = new Thread(async () =>
                    {
                        await ParseHtml(area.Url, level + 1);
                        lock (Locker)
                        {
                            Threads[province].Finished++;
                            int end = (int)Math.Floor((double)Threads[province].Finished / nodes.Count() * 40);
                            UpdateProgress(row, 0, end, ConsoleColor.Green);
                            if (Threads[province].Finished < nodes.Count() - 1)
                                UpdateProgress(row, 42, 82, ConsoleColor.Cyan);
                        }
                    });
                    Threads[province].SubThread.Add(thread);
                    thread.Start();
                }
                else
                {
                    await ParseHtml(area.Url, level + 1);
                    if (level == Level.County)
                    {
                        var end = (int)Math.Floor((double)index / nodes.Count() * 40);
                        UpdateProgress(row, 42, end, ConsoleColor.Green);
                        if (index < nodes.Count())
                            UpdateProgress(row, 84, 124, ConsoleColor.Cyan);
                    }
                    else
                    {
                        var end = (int)Math.Floor((double)index / nodes.Count() * 40);
                        UpdateProgress(row, 84, end, ConsoleColor.Green);
                    }
                }
                index++;
            }
        }
    }
}