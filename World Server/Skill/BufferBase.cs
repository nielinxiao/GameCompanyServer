using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Comp;

namespace Word_Server.Skill
{
    public abstract class BufferBase
    {
        public int BufferId;
        public BufferComp bufferComp;
        public float duraTime;
        public DateTime duratimer;
        public BufferBase(int BufferId, float duraTime, BufferComp bufferComp)
        {
            this.BufferId = BufferId;
            this.duraTime = duraTime;
            this.bufferComp= bufferComp;
        }
        public virtual void onStartBuffer()
        {
            duratimer = DateTime.UtcNow;
        }
        public abstract void Tick();
        public abstract void onEndBuffer();
    }
}
