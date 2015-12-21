using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AreaSpider
{
    public class Area
    {
        public Area()
        {
            Sub = new Dictionary<string, IList<Area>>();
        }
        public string ParentUrl { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public string Code { get; set; }

        public Dictionary<string, IList<Area>> Sub { get; set; }

        public IList<Area> this[string key]
        {
            get { return Sub[key]; }
            set { Sub[key] = value; }
        }

        public override string ToString()
        {
            string result = Write(this);
            return result;
        }

        private string Write(Area area)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("\"{0}\",\"'{1}\"", area.Title, area.Code);
            sb.AppendLine();
            if (area.Sub != null && area.Sub.Count > 0)
            {
                foreach (var item in area.Sub)
                {
                    string result = Write(item.Value);
                    sb.Append(result);
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public void Add(Area area, Level level)
        {
            var parentCode = area.Code.Substring(0, area.Code.Length - 2);
                    var province = parentCode.Substring(0, 2);
            switch (level)
            {
                case Level.City:
                    if(!Sub.ContainsKey(parentCode))
                        Sub[parentCode] = new List<Area>();
                    Sub[parentCode].Add(area);
                    break;
                case Level.County:
                    var city = Sub[province].Single(d => d.Code == parentCode);
                    if(!city.Sub.ContainsKey(area.Code))
                        city.Sub[parentCode] = new List<Area>();
                    city.Sub[parentCode].Add(area);
                    break;
                case Level.Town:
                    var cityCode = parentCode.Substring(0, 4);
                    var county = Sub[province].Single(d => d.Code == cityCode);
                    if(!county.Sub.ContainsKey(parentCode))
                        county.Sub[parentCode] = new List<Area>();
                    county.Sub[parentCode].Add(area);
                    break;
                    case Level.Village:
                    cityCode = parentCode.Substring(0, 4);
                    var countyCode = parentCode.Substring(0, 6);
                    var town =
                        Sub[province].Single(d => d.Code == cityCode).Sub[countyCode].Single(d => d.Code == parentCode);
            }
        }
    }
}