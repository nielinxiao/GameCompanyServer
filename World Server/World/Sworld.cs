using Message;
using Word_Sever.Client;
using Word_Sever.Interface;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOCP;

namespace Word_Sever.Servc
{
    public class Sworld : Ilogic
    {
        public GameContainer parentContainer;
        public OpenProgress open;
        public DateTime creatTime;
        public int ContainerID;
        public int maxCount;
        public string worldName;
        public int gameWorldID;
        public ushort port; 
        public string ip;
        public bool Full=>splayers.Count>=maxCount;
        public ConcurrentDictionary<string,SPlayer> splayers=new ConcurrentDictionary<string, SPlayer>();
        protected Dictionary<int,WorldComp>Comps=new Dictionary<int, WorldComp>();
        public LogTool log;
        public Sworld(GameContainer parentContainer, string ip,int gameWorldID, int maxCount, string worldName, ushort port, DateTime creatTime, int containerID)
        {
            open = OpenProgress.Instance;
            log = open.log;
            this.parentContainer = parentContainer;
            this.gameWorldID = gameWorldID;
            this.maxCount = maxCount;
            this.worldName = worldName;
            this.port = port;
            this.creatTime = creatTime;
            ContainerID = containerID;
            this.ip = ip;
        }
        public enum EnterMode
        {
            Successful,
            Exist,
            Full,
        }
        public List<Message.Player>GetAllPlayerConfig()
        {
            List<Message.Player> configs= new List<Message.Player>();
            foreach(var player in splayers.Values)
            {
                configs.Add(new Message.Player() { nickName = player.nickName, Uid = player.uid });
            }
            return configs;
        }
        //Player Enter GameWorld
        public virtual EnterMode TryAddPlayer(SPlayer sPlayer)
        {
            if (!splayers.ContainsKey(sPlayer.uid))
            {
                if (sPlayer.sworld != null)
                {
                    log.LogYellow($"[nickName:{sPlayer.nickName}][uid:{sPlayer.uid}] translate form [world:{sPlayer.sworld.worldName}] to [world:{this.worldName}]");
                    sPlayer.Quit();
                }
                if (splayers.Count>=maxCount)
                {
                    return EnterMode.Full;
                }
                sPlayer.sworld = this;
                splayers.TryAdd(sPlayer.uid,sPlayer);
                log.LogYellow($"Player:[{sPlayer.nickName}] Enter World:[{gameWorldID}]");
                return EnterMode.Successful;
            }
            else
            {
                log.LogYellow($"World:[{gameWorldID}] exis Player:[{sPlayer.nickName}] ");
                return EnterMode.Exist;
            }
        }
        public virtual void TryRemovePlayer(SPlayer sPlayer)
        {
            lock (splayers)
                if (splayers.TryRemove(sPlayer.uid, out SPlayer splayer))
                {
                    splayer.sworld = null;
                    log.LogYellow($"Player:[{sPlayer.nickName}] Exit World:[{gameWorldID}]");
                }
                else
                    log.LogError($"Player:[{sPlayer.nickName}] Exit World:[{gameWorldID}] Error");
        }
        public virtual void TryRemoveAllPlayer()
        {
            foreach (SPlayer sPlayer in splayers.Values)
            {
                sPlayer.sworld = null;
            }
            splayers.Clear();
        }
        public override void Destory()
        {
            base.Destory();
            Comps.Clear();
            log.LogGreen($"[{gameWorldID}]world destory successful");
        }
        public T AddComp<T>()where T:WorldComp,new()
        {
            if (GetComp<T>() != null)
                return null;
            T comp = new T();
            if(comp is IAWake aWake) { this.awakes.Add(aWake); };
            if(comp is IUpdate updates) { this.updates.Add(updates); };
            if(comp is IDestory destory) { this.destorys.Add(destory); };
            comp.sworld = this;
            Comps.Add(typeof(T).GetHashCode(), comp);
            return comp;
        }
        public T GetComp<T>()where T:WorldComp,new()
        {
            if (Comps.TryGetValue(typeof(T).GetHashCode(), out WorldComp Comp))
            {
                T comp = (T)Comp;
                return comp;
            }
            else
            {
                foreach (var comp in Comps)
                {
                    if (comp.Value is T com)
                    {
                        return com;
                    }
                }
                return null;
            }
        }
        public T RemoveComp<T>() where T : WorldComp, new()
        {
            if(Comps.TryGetValue(typeof(T).GetHashCode(),out WorldComp Comp))
            {
                T comp = (T)Comp;
                if (comp is IAWake aWake) { this.awakes.Remove(aWake); };
                if (comp is IUpdate updates) { this.updates.Remove(updates); };
                if (comp is IDestory destory) { this.destorys.Remove(destory); };
                return comp;
            }
            else
                return null;
        }
    }
    public class WorldComp
    {
        public Sworld sworld;
        public T GetComp<T>()where T : WorldComp, new()
        {
            return sworld.GetComp<T>();
        }
    }
}
