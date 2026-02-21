using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Server.Tools
{
    public static class DateTimeTick
    {
        public static float TicksToSeconds(long ticks)
        {
            return ticks / 10000000;
        }
    }
}
