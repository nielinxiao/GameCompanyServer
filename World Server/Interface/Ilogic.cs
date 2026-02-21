using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Sever.Interface
{
    public abstract class Ilogic
    {
        protected List<IAWake>awakes=new List<IAWake>();
        protected List<IUpdate> updates=new List<IUpdate>();
        protected List<IDestory> destorys=new List<IDestory>();
        public virtual void Awake()
        {
            for (int i = 0; i < awakes.Count; i++)
            {
                awakes[i].Awake();
            }
        }
        public virtual void Update()
        {
            for (int i = 0; i < updates.Count; i++)
            {
                updates[i].Update();
            }
        }
        public virtual void Destory()
        {
            for(int i=0; i<destorys.Count; i++)
            {
                destorys[i].Destory();
            }
            awakes.Clear();
            updates.Clear();
            destorys.Clear();
        }
    }
    public interface IAWake
    {
        void Awake();
    }
    public interface IUpdate
    {
        void Update();
    }
    public interface IDestory
    {
        void Destory();
    }
}
