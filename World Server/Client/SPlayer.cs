using Word_Sever.Interface;
using Word_Sever.Servc;
using System.Collections.Generic;
using System.Net.Sockets;
using IOCP;
using Message;

namespace Word_Sever.Client
{
    public class SPlayer:Ilogic
    {
        public Dictionary<int, PlayerComp> Comps = new Dictionary<int, PlayerComp>();
        public IOCPToken<LoginPCK> token;
        public string nickName;
        public string uid;
        public Sworld sworld=null;
        public SPlayer(string nickName, string uid, IOCPToken<LoginPCK> socket)
        {
            this.nickName = nickName;
            this.uid = uid;
            this.token = socket;
        }
        public void Quit()
        {
            if(sworld!=null)
            {
                OpenProgress.Instance.log.LogYellow($"[nickName:{nickName}][uid:{uid}] quit form [world:{sworld.worldName}]");
                OpenProgress.Instance.mainWorldSvc.ExitWorld(sworld, this);
            }
            else
            OpenProgress.Instance.log.LogYellow($"[nickName:{nickName}][uid:{uid}] quit form sign_server");
            RemoveAllComp();
        }
        public T AddComp<T>() where T : PlayerComp, new()
        {
            if (GetComp<T>() != null)
                return null;
            T comp = new T();
            if (comp is IAWake aWake) { this.awakes.Add(aWake); };
            if (comp is IUpdate updates) { this.updates.Add(updates); };
            if (comp is IDestory destory) { this.destorys.Add(destory); };
            comp.splayer = this;
            Comps.Add(typeof(T).GetHashCode(), comp);
            return comp;
        }
        public T GetComp<T>() where T : PlayerComp, new()
        {
            if (Comps.TryGetValue(typeof(T).GetHashCode(), out PlayerComp Comp))
            {
                T comp = (T)Comp;
                return comp;
            }
            else
            {
                foreach(var comp in Comps)
                {
                    if(comp.Value is T com)
                    {
                        return com;
                    }
                }
                return null;

            }
        }
        public T RemoveComp<T>() where T : PlayerComp, new()
        {
            if (Comps.TryGetValue(typeof(T).GetHashCode(), out PlayerComp Comp))
            {
                T comp = (T)Comp;
                if (comp is IAWake aWake) { this.awakes.Remove(aWake); };
                if (comp is IUpdate updates) { this.updates.Remove(updates); };
                if (comp is IDestory destory) { this.destorys.Remove(destory); };
                Comps.Remove(typeof(T).GetHashCode());
                return comp;
            }
            else
                return null;

        }
        public void RemoveAllComp()
        {
            awakes.Clear();
            updates.Clear();
            destorys.Clear();
            Comps.Clear();
        }
    }
    public class PlayerComp
    {
        public SPlayer splayer;
    }
}
