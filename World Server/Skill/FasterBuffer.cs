using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Comp;

namespace Word_Server.Skill
{
    public class FasterBuffer : BufferBase
    {
        public FasterBuffer(BufferComp bufferComp) : base(0,2, bufferComp)
        {

        }
        public override void onStartBuffer()
        {
            base.onStartBuffer();
            bufferComp.SetSpeedOffset(bufferComp.fixMoveSpeed*1.5f);
        }
        public override void onEndBuffer()
        {
            bufferComp.SetSpeedOffset(-bufferComp.fixMoveSpeed * 1.5f);
        }
        public override void Tick()
        {

        }
    }
}
