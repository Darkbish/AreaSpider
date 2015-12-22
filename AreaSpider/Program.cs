using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AreaSpider
{
    class Program
    {
        static void Main(string[] args)
        {
            //Spider.Load().Wait();
            //Spider.GenerateProgress("uxyi");
            var area = new Area { Code = "442000", Title = "市辖区" };
            Spider.ParseHtml("http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2013/44/4420.html", area, Level.Town)
                .Wait();
            File.WriteAllText($@"D:\{area.Code}.csv", area.ToString(), Encoding.UTF8);
        }
    }
}
