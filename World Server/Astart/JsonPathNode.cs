using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Word_Server.Tools;

[System.Serializable]
public struct Bound
{
    public Vector3 center;
    public Vector3 size;
    public bool Contains(Vector3 vector3)
    {
        float Length = (center - vector3).Length;
        if (Length <= size.x/2*1.5f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
public struct Vector
{ 
    public float x; public float y; public float z;
#if UNITY_EDITOR
    public Vector(Vector3 vector3)
    {
        x=vector3.x; y=vector3.y; z=vector3.z;
    }
#endif
}
[System.Serializable]
public struct JsonPathNode
{
    public List<int>NegiborId;
    public Bound bound;
    public int ColliderID;
    public bool IsBlock;
}
[System.Serializable]
public class PathNode
{
    public List<PathNode> Negibor=new List<PathNode>();
    public Bound bound;
    public float Distance;
    public PathNode parentNode;
    public int ColliderID;
    public bool IsBlock;
    public bool OverWrite;
}
[System.Serializable]
public struct JsonSystem
{
    public List <JsonPathNode> paths;
}
