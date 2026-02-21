using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Word_Sever;
using System.Diagnostics;

namespace Word_Sever.Servc
{
    public class SignSvc : IService
    {
        OpenProgress openProgress;
        public Socket Server;
        public void Init()
        {
            openProgress = OpenProgress.Instance;
            Server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Server.Bind(new IPEndPoint(IPAddress.Parse(openProgress.pEConfig.signIp), openProgress.pEConfig.signPort));
        }

        public void Tick()
        {
        }

        public void UnInit()
        {
        }
    }
}
