using System.Collections.Generic;
using System.Threading;

namespace AreaSpider
{
    public class SpiderThread
    {
        public string Title { get; set; }
        public int Row { get; set; }

        public Thread Thread { get; set; }
    }
}