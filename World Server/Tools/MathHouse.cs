using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Server.Tools
{
    public struct Vector3
    {
        public float x; public float y; public float z;
        public float Length => (float)Math.Sqrt(x * x + y * y + z * z);
        public override string ToString()
        {
            return $"x:{x},y:{y},z:{z}";
        }
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Vector3(Vector vector)
        {
            this.x = vector.x;
            this.y = vector.y;
            this.z = vector.z;
        }
        public Vector3 normal()
        {
            if (Length == 0)
                return this;
            else
                return new Vector3() { x = x / Length, y = y / Length, z = z / Length };
        }
        public static Vector3 operator -(Vector3 vec1, Vector3 vec2)
        {
            return new Vector3() { x = vec1.x - vec2.x, y = vec1.y - vec2.y, z = vec1.z - vec2.z };
        }
        public static Vector3 operator +(Vector3 vec1, Vector3 vec2)
        {
            return new Vector3() { x = vec1.x + vec2.x, y = vec1.y + vec2.y, z = vec1.z + vec2.z };
        }
        public static bool operator !=(Vector3 vec1, Vector3 vec2)
        {
            if(vec1.x==vec2.x&& vec1.y == vec2.y&& vec1.z == vec2.z)
                return false;
            return true;
        }
        public static bool operator ==(Vector3 vec1, Vector3 vec2)
        {

            if (vec1.x == vec2.x && vec1.y == vec2.y && vec1.z == vec2.z)
                return true;
            return false;
        }
        public static Vector3 operator *(Vector3 vec1, Vector3 vec2)
        {
            return new Vector3() { x = vec1.x * vec2.x, y = vec1.y * vec2.y, z = vec1.z * vec2.z };
        }
        public static Vector3 operator *(Vector3 vec1, float mulit)
        {
            return new Vector3() { x = vec1.x * mulit, y = vec1.y * mulit, z = vec1.z * mulit };
        }
        public static Vector3 operator /(Vector3 vec1, float mulit)
        {
            if (mulit != 0)
                return new Vector3() { x = vec1.x / mulit, y = vec1.y / mulit, z = vec1.z / mulit };
            else
                return vec1;
        }
        public static Vector3 operator /(Vector3 vec1, Vector3 vec2)
        {
            return new Vector3() { x = vec1.x / vec2.x, y = vec1.y / vec2.y, z = vec1.z / vec2.z };
        }
        public static float Dot(Vector3 vec1, Vector3 vec2)
        {
            return vec1.z * vec2.z+vec1.x * vec2.x+vec1.y * vec2.y;
        }
        public static Vector3 Cross(Vector3 vec1, Vector3 vec2)
        {
            return new Vector3()
            {
                x = vec1.y * vec2.z - vec1.z * vec2.y,
                y = vec1.x * vec2.z - vec1.z * vec2.x,
                z = vec1.x * vec2.y - vec1.y * vec2.x
            };
        }
        public static implicit operator Vector3(Message.Vector3 vec)
        {
            return new Vector3() { x=vec.X,y=vec.Y,z=vec.Z };
        }
        public static Vector3 zero=new Vector3() { x = 0, y = 0, z = 0 };
        public static Vector3 down=new Vector3() { x = 0, y = -1, z = 0 };
        public static Vector3 up=new Vector3() { x = 0, y = 1, z = 0 };
    }
    public struct QuaternionEuler
    {
        public float x, y, z;
        public override string ToString()
        {
            return $"x:{x},y:{y},z:{z}";
        }
        public static QuaternionEuler zero = new QuaternionEuler() { x = 0, y = 0, z = 0 };
        public static  implicit operator Vector3(QuaternionEuler vector3 )
        {
            return new Vector3() { x = vector3.x, y = vector3.y, z = vector3.z };
        }
    }
}
