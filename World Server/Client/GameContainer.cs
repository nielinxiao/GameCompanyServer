using Word_Sever.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Comp;

namespace Word_Sever.Servc
{
    public class GameContainer
    {
        public int ContainerID;
        public const int maxCount = 2;
        public const string worldName = "defult world";
        public List<Sworld>Worlds=new List<Sworld>();
        public int count=>Worlds.Count;
        MainWorldSvc worldSvc;
        LogTool log;
        OpenProgress openProgress;
        public bool fullWorld=>(Worlds.Count>=openProgress.pEConfig.ContainerCreatWorldCount);
        public GameContainer(int containerID)
        {
            openProgress = OpenProgress.Instance;
            log = openProgress.log;
            worldSvc = openProgress.mainWorldSvc;
            ContainerID = containerID;
        }
        public Sworld IDtoSworld(int sworldID)
        {
            foreach(var sworld in Worlds)
            {
                if(sworld.ContainerID==sworldID)
                    return sworld;
            }
            return null;
        }
        public void Awake()
        {
            Task.Run(Update);
        }
        public void AddGameWorld(string ip,int worldID,int port)
        {
            BattleWorld world = new BattleWorld(this, ip,worldID, maxCount, worldName, (ushort)port, DateTime.Now, ContainerID);
            world.AddComp<AOIComp>();
            world.AddComp<NetWorkItemComp>();
            world.AddComp<AstartComp>();
            world.AddComp<AudioComp>();
            world.AddComp<TimeComp>();
            //world.AddComp<OccupiedComp>();
            world.Awake();
            Worlds.Add(world);
            log.LogGreen($"creat worldName:[{worldName}] worldID:[{worldID}] maxCount:[{maxCount}] port:[{port}]");
            worldSvc.AddGameWorld(world);
        }
        public void AddGameWorld(string ip, int worldID, int port,string worldname,int maxcount,DateTime creatTime)
        {
            BattleWorld world = new BattleWorld(this,ip,worldID, maxcount, worldname, (ushort)port, creatTime, ContainerID);
            world.AddComp<AOIComp>();
            //world.AddComp<OccupiedComp>();
            world.AddComp<AstartComp>();
            world.AddComp<NetWorkItemComp>();
            world.AddComp<TimeComp>();
            world.AddComp<AudioComp>();
            world.Awake();
            Worlds.Add(world);
            log.LogGreen($"creat worldName:[{worldname}] worldID:[{worldID}] maxCount:[{maxcount}] port:[{port}] ");
            worldSvc.AddGameWorld(world);
        }
        public void Update()
        {
            for (int i = 0; i < Worlds.Count; i++)
            {
                Worlds[i].Update();
            }
        }
        public void Destory()
        {
            for (int i = 0; i < Worlds.Count; i++)
            {
                Worlds[i].Destory();
            }
            Worlds.Clear();
        }
        public void RemoveGameWorld(Sworld sworld)
        {
            if(Worlds.Contains(sworld))
            {
                Worlds.Remove(sworld);
                sworld.Destory();
            }
        }
    }
}
