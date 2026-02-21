using IOCP;
using Message;
using MySqlX.XDevAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Word_Sever;
using Word_Sever.Client;

namespace Word_Server.Tools
{
    public class VoiceTcp : IOCPServer<VoicePCK>
    {
        public List<IOCPToken<VoicePCK>>clients=new List<IOCPToken<VoicePCK>>();
        public List<string>uids=new List<string>();
        public VoiceTcp() :base(81000)
        { }
        public override void AcceptClient(IOCPToken<VoicePCK> client)
        {
            if (IsDestory)
                return;
            lock (clients)
                if (!clients.Contains(client))
                {
                    clients.Add(client);
                    uids.Add("");
                    logaction.Invoke("Voice Tcp Regist");
                }
        }
        public void CloseToken(string uid)
        {
            int index = uids.IndexOf(uid);
            lock (clients)
                if (index != -1)
                {
                    clients.RemoveAt(index);
                    uids.RemoveAt(index);

                }
        }
        public override void OnCloseAccpet(IOCPToken<VoicePCK> client)
        {
            if(IsDestory)
                return;
            lock (clients)
            {
                int index=clients.IndexOf(client);
                if (index != -1)
                {
                    clients.RemoveAt(index);
                    uids.RemoveAt(index);
                }
                logaction.Invoke("Voice Tcp Remove");
            }
        }
        bool IsDestory = false;
        public void Close()
        {
            IsDestory = true;
            lock (clients)
                clients.Clear();
            CloseServer();
        }
        public override void OnReceiveMessage(IOCPToken<VoicePCK> client, VoicePCK message)
        {
            logaction.Invoke(message.Body.Uid + "Say");
            int count = -1;
            lock (clients)
                foreach (var item in clients)
                {
                    count++;
                    if (item == client)
                    {
                        uids[count] = message.Body.Uid;
                    }
                    else
                    {
                        item.Send(message);
                    }
                }
        }
    }
}
