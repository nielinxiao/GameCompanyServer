using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Comp;

namespace Word_Server.Skill
{
    public static class SkillConfig
    {
        public class Config
        {
            public int skillId;
            public string skillName;
            public int CDTime;
            public Config(int skillId,string skillName,int CDTime)
            {
                this.skillId = skillId;
                this.skillName = skillName;
                this.CDTime = CDTime;
            }
        }
        public static BufferBase GetPrefabById(int skillId,BufferComp bufferComp)
        {
            switch (skillId)
            {
                case 0:
                    return new FasterBuffer(bufferComp);
                default:
                    return null;
            }
        }
        public static Config GetConfigById(int skillID)
        {
            switch (skillID) 
            {
                case 0:
                    return new Config(0,"提速",3);
                default:
                    return null;
            }
        }
    }
}
