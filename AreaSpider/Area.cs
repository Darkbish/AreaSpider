using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AreaSpider
{
    public class Area
    {
        public Area()
        {
            Sub = new List<Area>();
        }
        public string ParentUrl { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public string Code { get; set; }

        public  IList<Area> Sub { get; set; }

        public override string ToString()
        {
            string result = Write(this);
            return result;
        }

        private string Write(Area area)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\"{0}\",=\"{1}\"", area.Title, area.Code);
            sb.AppendLine();
            if (area.Sub != null && area.Sub.Count > 0)
            {
                foreach (var item in area.Sub)
                {
                    string result = Write(item);
                    sb.Append(result);
                }
                //sb.AppendLine();
            }
            return sb.ToString();
        }

        public void Add(Area area)
        {
            Sub.Add(area);
        }
    }
}