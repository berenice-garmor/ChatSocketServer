using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatSocketServer1
{
    class weatherInfo
    {
        weatherInfo wi = new weatherInfo();
        public class main
        {
            public double temp { get; set; }

        }

        public class weather
        {
            public string desc { get; set; }
        }

        public class root
        {
            public main main { get; set; }
            public List <weather> weatherList { get; set; }
        }
    }
}
