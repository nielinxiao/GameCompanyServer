using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Server.Config
{
    public class ExploitsConfig
    {
        public string exploitName;
        public string descipt;
    }

    public static class FightConfig
    {
        public static ExploitsConfig IDtoExploits(int id)
        {
            switch (id)
            {
                case 0:
                    return new ExploitsConfig()
                    {
                        exploitName ="连续击败",
                        descipt="连续击杀两个以上"
                    };
                case 1:
                    return new ExploitsConfig()
                    {
                        exploitName = "命中",
                        descipt="命中就算"
                    };
                case 2:
                    return new ExploitsConfig()
                    {
                        exploitName = "击败",
                        descipt="击杀第一次"
                    };
                default:
                    return null;
            }
        }
    }
}
