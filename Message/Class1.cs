using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Message
{
    public enum cmd
    {
        Client_ask,
        Client_join,
        Server_answer,
        Server_Allow,
        Server_Refuse,
        //客户端在没有进入游戏前一直发送 Ping 保证 UDP套接字存在
        World_Regist,
        World_Move,
        World_Move_CallBack,
        World_Regist_TankID,
        World_Fire,
        World_Hit,
        World_Explore,
        World_Occup,
        World_EnterOccup,
        World_RegistFire,
        World_AddBuffer,
        Time_Async,
    }
    [System.Serializable]
    public class BaseMsg
    {
        public cmd cmd;
    }
    [Serializable]
    public class FireCallBack:BaseMsg
    {
        public int Count;
    }
    [Serializable]
    public class SpawnCallBack: BaseMsg
    {
        public Vector3 spawnPos;
        public QuaternionEluer eluer;
    }
    //Enter world
    [System.Serializable]
    public class SignMsg:BaseMsg
    {
        public string uid;
        public string Nickname;
        public int worldID;
    }
    [System.Serializable]
    public class WorldConfig: BaseMsg
    {
        public string worldName;
        public uint maxCount;
        public uint port;
        public int worldID;
        public int ContainerID;
    }
    [Serializable]
    public struct Player
    {
        public string nickName;
        public string UID;
    }
    [Serializable]
    public class HealthyCallBack : BaseMsg
    {
        public float healthy;
        public float damage;
        public string hiterUid;
        public string hiterName;
        public string beHituid;
    }
    [Serializable]
    public class HitConfig:BaseMsg
    {
        public int cannolBollID;
        public string hiterUid;
        public string hiterName;
        public string beHituid;
        public float damage;
    }

    [System.Serializable]
    public class TankConfig : BaseMsg 
    {
        public string uid;
        public int TeamID;
        public int Tankid;
    }
    [Serializable]
    public class EnterOccup:BaseMsg
    {
        public string uid;
        public int occupID; 
    }
    [System.Serializable]
    public class EnterWorld:BaseMsg
    {
        public string battleIp;
        public int battleport;
        public string worldName;
        public int worldID;
        public List<Player> PlayerNameOrUID=new List<Player>();
    }
    [System.Serializable]
    public class Occupeid: BaseMsg
    {
        public List<Occup>occups=new List<Occup>();

    [System.Serializable]
        public struct Occup
        {
            public int id;
            public float progress;
        }

    }

    //同步
    [System.Serializable]
    public class Ping : BaseMsg
    {
        public string uid;
        public string nickname;
    }
    [System.Serializable]
    public struct Vector3
    {
        public float x, y, z;
    }
    [System.Serializable]
    public class AddBuffer:BaseMsg
    {
        public string uid;
        public int skillId;
        public DateTime InvokeBuffer;
    }
    [System.Serializable]
    public struct QuaternionEluer
    {
        public float x, y, z;
    }
    [System.Serializable]
    public class ExploreConfig : BaseMsg
    {
        public List<PersonExplore>explores=new List<PersonExplore>();
        [System.Serializable]
        public struct PersonExplore
        {
            public string uid;
            public string Nickname;
            public int KillPerson;
            public int deathCount;
            public int score;
            public int TeamID;
        }
    }
    [System.Serializable]
    public class FireConfig :BaseMsg
    {
        public string uid;
        public int CannolID;
        public Vector3 dir;
        public Vector3 position;
        public DateTime utcNow;
    }
    [System.Serializable]
    public class MoveConfig: BaseMsg
    {
        public string uid;
        public DateTime utc;
        public Vector3 position;
        public Vector3 dir;
        public QuaternionEluer careel_roation;
        public QuaternionEluer roation;
        public bool isMove;
    }

    [System.Serializable]
    public class TimeAsync:BaseMsg
    {
        public DateTime dateTime;
    }
    [System.Serializable]
    public class MoveCallBack : BaseMsg
    {
        public List<PlayerConfig> config=new List<PlayerConfig>();
        [System.Serializable]
        public struct PlayerConfig
        {
            public int TankId;
            public int teamID;
            public string uid;
            public string nickname;
            public Vector3 position;
            public Vector3 dir;
            public QuaternionEluer roation;
            public QuaternionEluer careelBoll_roatition;
            public DateTime utc;
            public float currentspeed;
            public bool isMove;
        }
    }
}
