using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Comp;
using Word_Server.Tools;

namespace Word_Server.Tank
{
    public static class RealmRoot
    {
        public class Realm
        {
            public Vector3 position;
            public float Radius;
            public float Progress;
            public List<TransformComp>red;
            public List<TransformComp>blue;
        }
        public static Realm GetRealm(int mapID)
        {
            switch (mapID)
            {
                case 0:
                    return new Realm()
                    {
                        Radius = 15f,
                        position = new Vector3() { x = 0, y = -1.3f, z = 0 },
                        Progress=0,
                        blue = new List<TransformComp>(),
                        red = new List<TransformComp>()
                    };
                default:
                    return default(Realm);
            }
        }
    }

}
