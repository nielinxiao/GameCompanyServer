using Message;
using Word_Sever.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Tools;

namespace Word_Sever.Servc
{
    public class AccSvc : IService
    {
        OpenProgress openProgress;
        LogTool log;
        public AccSvc()
        {
            openProgress = OpenProgress.Instance;
            log=openProgress.log;
        }
        AccTCP accTCP;
        public void Init()
        {
            accTCP = new AccTCP(openProgress.mainWorldSvc);
            accTCP.InitIOCPServer(log.LogWhite, 10, true, openProgress.pEConfig.signIp, openProgress.pEConfig.signPort);
        }
        public void Tick()
        {

        }
        public void UnInit()
        {
            accTCP.CloseServer();
        }
    }
}