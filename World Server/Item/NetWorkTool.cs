using IOCP;
using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Tools;
using Word_Sever.World;

namespace Word_Server.Item
{
    public class NetWorkTool
    {
        public IOCPToken<WorldPCK> tcp=null;
        public int netWorkID;
        public readonly float UseTime;
        public float value;
        public string uid;
        public bool IsFireObject=false;
        public NetWorkTool(int netWorkID,float UseTime,bool IsFireObject)
        {
            this.UseTime = UseTime;
            value = 0;
            this.IsFireObject = IsFireObject;
        }
        DateTime fireTime;
        bool IsFire = false;
        public bool Fire()
        {
            if(value>=UseTime)
            {
                return false;
            }
            else
            {
                if (tcp != null)
                {
                    fireTime = DateTime.UtcNow;
                    IsFire = true;
                    return true;
                }
                Console.WriteLine("No Grab");
                return false;
            }
        }
        public void UnFire()
        {
            if (tcp != null)
            {
                IsFire = false;
            }
        }
        public void TickTime()
        {
            if(IsFire)
            {
                value += (float)(DateTime.UtcNow - fireTime).TotalSeconds;
                fireTime = DateTime.UtcNow;
                if(value>=UseTime)
                {
                    PowerRefuse();
                }
            }
        }
        public void PowerRefuse()
        {
            if (tcp != null)
            {
                WorldPCK worldPCK = new WorldPCK();
                worldPCK.Head = new Message.Head();
                worldPCK.Head.Cmd = Cmd.WorldPowerStopFire;

                tcp.Send(worldPCK);
                Console.WriteLine("Fire time out power clsoe");
            }
        }
        public bool Grab(IOCPToken<WorldPCK> tcp)
        {
            if(this.tcp!=null)
            {
                return false;
            }
            else
            {
                this.tcp= tcp;
                return true;
            }
        }
        public void Throw()
        {
            tcp = null;
        }
    }
}
