using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class ToolsDocument
{
    public enum FireType
    {
        FireExtinguishers = 0,
        FireTool = 1,
        Document = 2,
        FoamFireExtinguishers = 3,
        SofaFire = 4,
    }
    FireType fireType;
    public static int GetFireDamage(FireType fireType)
    {
        switch (fireType)
        {
            case FireType.FireExtinguishers: return 10;
            case FireType.SofaFire: return 10;
            case FireType.Document: return 0;
            case FireType.FireTool: return 10;
            case FireType.FoamFireExtinguishers: return 10;
            default: return 0;
        };
    }
}
