using System;
using System.Collections.Generic;
using System.Threading;

namespace AreaSpider
{
    public class SpiderThread
    {
        public SpiderThread()
        {
            SubThread = new List<Thread>();
        }
        public string Title { get; set; }
        public int Row { get; set; }

        public int Finished { get; set; }

        public Thread Thread { get; set; }

        public IList<Thread> SubThread { get; set; }

    }
}