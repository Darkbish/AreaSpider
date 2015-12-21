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
            //var codes = SpiderHelper.Export().Result;
            //File.WriteAllText(@"D:\area.txt", codes);
            //string pcode = "44";
            //string ccode = "4420";
            //string cocode = "441900";
            //string result = SpiderHelper.ExportCounties(pcode, ccode, cocode, "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2013/44/4420.html").Result;
            //File.WriteAllText($@"D:\{ccode}.txt", result);
            string content = Read();
            Write(content);
        }

        static string Read()
        {
            StringBuilder sb = new StringBuilder();
            using (StreamReader reader = new StreamReader(@"D:\1.txt", Encoding.UTF8))
            {
                string line = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Replace("\n", "");
                    line = line.Replace("\t", ",");
                    var temp = line.Split(new[] {@","},StringSplitOptions.None);
                    var title = temp[0];
                    var code = temp[1];
                    sb.AppendFormat("\"{0}\",\"'{1}\"", title, code);
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        static void Write(string content)
        {
            File.WriteAllText(@"D:\1.csv",content,Encoding.UTF8);
        }
    }
}
