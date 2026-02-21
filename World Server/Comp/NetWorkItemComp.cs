using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Item;
using Word_Sever.Interface;
using Word_Sever.Servc;
using Word_Sever.World;
using static Word_Server.Comp.NetWorkItemComp;

namespace Word_Server.Comp
{
    public class NetWorkItemComp : WorldComp, IAWake,IUpdate
    {
        public List<Message.ToolConfig> configs = new List<Message.ToolConfig>();
        private Dictionary<int, NetWorkTool> networks = new Dictionary<int, NetWorkTool>();
        public CenterFireSystem fireSystem = new CenterFireSystem();
        public const int PEROOT_COUNT = 48;
        public const int CENTER_FIRE_TIME = 120;
        public const int CENTER_COUNT = 3;
        public const int HOLD_TIME = 120;
        public int WorldSeconds=600;
        public DateTime EndDateTime=default(DateTime);
        public void SetEndDateTime(DateTime dateTime)
        {
            this.EndDateTime = dateTime.AddSeconds(WorldSeconds);
        }
        public bool GetNetWorkTool(int ID,out NetWorkTool netWorkTool)
        {
            if(networks.TryGetValue(ID,out NetWorkTool netWorkTool2))
            {
                netWorkTool= netWorkTool2;
                return true;
            }
            else
            {
                netWorkTool = null;
                return false;
            }
        }
        public void Awake()
        {
            configs.Add(tool(0,  (-78.873f, -8.619f, 44.454f), (0, 0, 0)));
            configs.Add(tool(0,  (-78.873f, -8.619f, 44.454f), (0, 0, 0)));
            configs.Add(tool(0,  (-78.873f, -8.619f, 44.454f), (0, 0, 0)));
            configs.Add(tool(1, (-67.063f, -4.649f, 34.802f), (0, 0, 0)));
            configs.Add(tool(1, (-67.063f, -4.649f, 34.802f), (0, 0, 0)));
            configs.Add(tool(1, (-67.063f, -4.649f, 34.802f), (0, 0, 0)));
            configs.Add(tool(2, (-68.094f, -8.619f, 39.03f), (0, 0, 0)));
            configs.Add(tool(2, (-68.094f, -8.619f, 39.03f), (0, 0, 0)));
            configs.Add(tool(2, (-68.094f, -8.619f, 39.03f), (0, 0, 0)));
            Queue<int> queue = new Queue<int>();
            foreach (var config in configs) 
            {
                NetWorkTool tool= GetToolByID(config.ToolsID);
                networks.Add(config.NetworkID,tool );
                if(tool.IsFireObject)
                {
                    queue.Enqueue(config.NetworkID);
                }
            }
            List<CenterFire> FireObjects = new List<CenterFire>();
            List<int> list = new List<int>(); 
            list.Add(Random(ref list));
            list.Add(Random(ref list));
            list.Add(Random(ref list));
            foreach(var itemid in list)
            {
                FireObjects.Add(new CenterFire(itemid, CENTER_FIRE_TIME));
            }
            sworld.log.LogYellow($"此局的 中枢是 [1:{FireObjects[0].netWorkID} 2:{FireObjects[1].netWorkID} 3:{FireObjects[2].netWorkID}]");
            fireSystem.RegistCenter(FireObjects, OnFireComplite, HOLD_TIME, (BattleWorld)sworld);
        }
        public int Random(ref List<int>ints)
        {
            Random random = new Random();
            int temp=(int)(random.NextDouble() * PEROOT_COUNT);
            while(ints.Contains(temp))
            {
                temp = (int)(random.NextDouble() * PEROOT_COUNT);
            }
            return temp;
        }
        public void OnFireComplite()
        {
            sworld.log.LogGreen("燃烧殆尽 反方胜利");
            WorldPCK worldPCK = new WorldPCK();
            worldPCK.Head = new Message.Head();
            worldPCK.Head.Cmd=Cmd.WorldFireWinner;
            ((BattleWorld)sworld).BorastCast(worldPCK);
            IsComplite=true;
            sworld.open.mainWorldSvc.RemoveWorld(sworld);
        }
        public void OnUnFireComplite()
        {
            WorldPCK worldPCK = new WorldPCK();
            worldPCK.Head = new Message.Head();
            worldPCK.Head.Cmd = Cmd.WorldUnFireWinner;
            ((BattleWorld)sworld).BorastCast(worldPCK);
            IsComplite=true;
            sworld.open.mainWorldSvc.RemoveWorld(sworld);
        }
        public NetWorkTool GetToolByID(int id)
        {
            switch (id)
            {
                case 0:
                    return new FireExtinguishers(id);
                case 1:
                    return new FireTool(id);
                case 2:
                    return new Document(id);
                default : return null;
            }
        }
        public Message.ToolConfig tool(int itemID, (float, float, float) postion, (float, float, float) rotation)
        {
            return new Message.ToolConfig()
            {
                ToolsID = itemID,
                Position = new Message.Vector3() { X = postion.Item1, Y = postion.Item2, Z = postion.Item3 },
                Rotation = new Message.Vector3() { X = rotation.Item1, Y = rotation.Item2, Z = rotation.Item3 },
                NetworkID= configs.Count,
            };
        }

        bool IsComplite = false;
        public void Update()
        {
            if (IsComplite)
                return;
            if (fireSystem != null)
                fireSystem.Tick();
            if(EndDateTime!=default(DateTime))
            {
                if((EndDateTime-DateTime.UtcNow).TotalSeconds<=0)
                {
                    OnUnFireComplite();
                }
            }
            foreach (var network in networks) 
            {
                network.Value.TickTime();
            }
        }
    }
}
