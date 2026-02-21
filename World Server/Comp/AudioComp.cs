using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Tools;
using Word_Sever;
using Word_Sever.Interface;
using Word_Sever.Servc;

namespace Word_Server.Comp
{
    public class AudioComp : WorldComp, IAWake,IDestory
    {
        VoiceTcp voiceTcp;
        public int port;
        public void Awake()
        {
            voiceTcp = new VoiceTcp();
            port=OpenProgress.Instance.pEConfig.VoicePortBase + sworld.gameWorldID;
            voiceTcp.InitIOCPServer(sworld.log.LogYellow, 10,true, OpenProgress.Instance.pEConfig.battleBindIp ,port);
            sworld.log.LogGreen($"Voice set up on [port:{port}]");
        }
        public void CloseToken(string uid)
        {
            voiceTcp.CloseToken(uid);
        }
        public void Destory()
        {
            voiceTcp.Close();
        }
    }
}