using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Server.Tank
{
    public static class Map
    {
        public static Vector3 TeamIDToMapVector3(int id)
        {
            switch (id)
            {
                case 0:
                    return new Vector3() { X = -95.65f, Y = -6.2f, Z = 39.31f };//灭火
                case 1:
                    return new Vector3() { X = -78.41f, Y = -4.57f, Z = 39.31f };//放火
                default:
                    return new Vector3() { X = -95.65f, Y = -6.2f, Z = 39.31f };
            }
        }
        public static Vector3 TeamIDToMapEluer(int id)
        {
            switch (id)
            {
                case 0:
                    return new Vector3() { X = 0, Y = 90f, Z = 0 };
                case 1:
                    return new Vector3() { X = 0, Y = 90f, Z = 0 };
                default:
                    return new Vector3() {X = 0, Y = 0, Z = 0 };
            }
        }
    }
}
