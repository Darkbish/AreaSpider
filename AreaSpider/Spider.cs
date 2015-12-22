using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AreaSpider
{
    public static class Spider
    {
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
            var area = new Area { Title = "Title", Code = "Code" };
            Console.WindowWidth = Console.LargestWindowWidth * 3 / 4;
            Console.WindowHeight = Console.LargestWindowHeight * 3 / 4;
            await ParseHtml(INDEX_URL, area, Level.Province);
            foreach (var item in Threads)
            {
                item.Value.Thread.Start();
            }
            lock (Locker)
            {
                while (Threads.Count > Finished || Threads.Any(d => d.Value.Finished < d.Value.SubThread.Count))
                {
                    Monitor.Wait(Locker);
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach (var item in area.Sub)
            {
                sb.Append(item);
            }
            File.WriteAllText(@"D:\areas.txt", sb.ToString(), Encoding.UTF8);
        }

        public static void GenerateProgress(string title)
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

        public static async Task ParseHtml(string url, Area area, Level level)
        {
            var html = await HttpHelper.GetAsync(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var i = 0;
            var nodes = doc.DocumentNode.SelectNodes(XPATH).Where(d => d.HasAttributes && Classes.Any(r => r == d.Attributes["class"].Value));
            switch (level)
            {
                case Level.Province:
                    ParseProvince(nodes, area);
                    break;
                case Level.City:
                case Level.County:
                case Level.Town:
                case Level.Village:
                    await ParseCommon(url, nodes, area, level);
                    break;
            }
        }

        private static void ParseProvince(IEnumerable<HtmlNode> nodes, Area parent)
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
                    parent.Add(area);
                    GenerateProgress(title);
                    var thread = new SpiderThread
                    {
                        Row = row + 1,
                        Title = title,
                        Thread = new Thread(async () =>
                        {
                            await ParseHtml(area.Url, area, Level.City);
                            lock (Locker)
                            {
                                Finished++;
                                //File.WriteAllText($@"D:\areas\{title}.txt", area.ToString(), Encoding.UTF8);
                                Monitor.Pulse(Locker);
                            }
                        })
                    };
                    Threads.Add(code, thread);
                    row += 2;
                }
            }
        }

        private static async Task ParseCommon(string url, IEnumerable<HtmlNode> nodes, Area parent, Level level)
        {
            var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
            int index = 1;
            foreach (var node in nodes)
            {
                var td = node.SelectNodes("td");
                string href = null;
                string code;
                string title;
                if (td[0].HasChildNodes && string.Equals(td[0].FirstChild.Name, "a", StringComparison.OrdinalIgnoreCase))
                {
                    href = td[0].SelectSingleNode("a").Attributes["href"].Value;
                    code = Regex.Match(href, @"\d+\/(\d+)\.html").Groups[1].Value;
                    title = td[1].SelectSingleNode("a").InnerText;
                }
                else
                {
                    code = td[0].InnerText;
                    title = td.Count > 2 ? td[2].InnerText : td[1].InnerText;
                }
                var area = new Area
                {
                    Code = code,
                    Title = title,
                    ParentUrl = url,
                    Url = href != null ? baseUrl + href : null
                };
                parent.Add(area);
                if (area.Url == null)
                    continue;
                string province = code.Substring(0, 2);
                int? row = null;
                if (Threads.ContainsKey(province))
                    row = Threads[province]?.Row;
                var step = 40;
                var start = (int)(level - 1) * step + (int)(level - 1);
                var max = (int)level * step + (int)(level - 1);
                index++;
                if (level == Level.City)
                {
                    var thread = new Thread(async () =>
                    {
                        await ParseHtml(area.Url, area, level + 1);
                        if (row.HasValue)
                            lock (Locker)
                            {
                                Threads[province].Finished++;
                                CalcProgress(row.Value, Threads[province].Finished, start, max, nodes.Count());
                                Monitor.Pulse(Locker);
                            }
                    });
                    Threads[province].SubThread.Add(thread);
                    thread.Start();
                }
                else
                {
                    await ParseHtml(area.Url, area, level + 1);
                    if (row.HasValue && (level == Level.County || level == Level.Town))
                        CalcProgress(row.Value, index, start, max, nodes.Count(), level == Level.County);
                }
            }
        }

        private static void CalcProgress(int row, int finished, int start, int max, int count, bool updateChild = true)
        {
            var end = start + (int)Math.Floor((double)finished / count * 40);
            var step = 41;
            if (end > max)
                end = max;
            UpdateProgress(row, start, end, ConsoleColor.Green);
            if (finished < count && updateChild)
                UpdateProgress(row, start + step, max + step, ConsoleColor.Cyan);
        }
    }
}