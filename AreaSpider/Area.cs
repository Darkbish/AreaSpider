using System.Collections.Generic;

namespace AreaSpider
{
    public class Area
    {
        public string ParentUrl { get; set; }

        public string Url { get; set; }

        public string Title { get; set; }

        public string Code { get; set; }

        public Dictionary<string,Area> Sub { get; set; } 
    }
}