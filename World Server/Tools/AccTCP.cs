using IOCP;
using Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Word_Sever;
using Word_Sever.Client;
using Word_Sever.Servc;

namespace Word_Server.Tools
{
    public class AccTCP : IOCPServer<LoginPCK>
    {
        ConcurrentDictionary<IOCPToken<LoginPCK>, string> UIDRoles = new ConcurrentDictionary<IOCPToken<LoginPCK>, string>();
        ConcurrentDictionary<string, SPlayer> playerAccount = new ConcurrentDictionary<string, SPlayer>();
        MainWorldSvc mainWorldSvc;
        OpenProgress open;
        public AccTCP(MainWorldSvc mainWorldSvc):base(2048)
        {
            this.mainWorldSvc = mainWorldSvc;
            open = OpenProgress.Instance;
        }
        public override void AcceptClient(IOCPToken<LoginPCK> client)
        {
           
        }

        public override void OnCloseAccpet(IOCPToken<LoginPCK> client)
        {
            if (UIDRoles.TryRemove(client,out string uid))
            {
                playerAccount[uid].Quit();
                if(playerAccount.TryRemove(uid, out SPlayer sPlayer))
                {
                    RemoveToken(sPlayer.token);
                }
            }
        }

        public override void OnReceiveMessage(IOCPToken<LoginPCK> client, LoginPCK message)
        {
            switch (message.Head.Cmd)
            {
                case Cmd.AccSvcRegist:
                    if (open.mysqlTcp.IsToken(message.Body.Token))
                    {
                        logaction.Invoke($"[Login][Name:{message.Body.Nickname}][UID:{message.Body.Uid}]");
                        SPlayer sPlayer = new SPlayer(message.Body.Nickname, message.Body.Uid, client);
                        playerAccount.TryAdd(message.Body.Uid, sPlayer);
                        UIDRoles.TryAdd(client, message.Body.Uid);
                        LoginPCK loginCallback = new LoginPCK();
                        loginCallback.Head = new Message.Head();
                        loginCallback.Head.Cmd = Cmd.AccSvcRegist;
                        client.Send(loginCallback);
                    }
                    else
                        logaction.Invoke("Token 不存在");
                    break;
                case Cmd.AccSvcJoin:
                    logaction.Invoke($"[Join][Name:{message.Body.Nickname}][UID:{message.Body.Uid}][WorldID:{message.Body.worldID}]");
                    if (playerAccount.ContainsKey(message.Body.Uid))
                        open.mainWorldSvc.EnterWorld(message.Body.worldID, playerAccount[message.Body.Uid]);
                    break;
                case Cmd.AccSvcTimeSync:
                    message.Body.DateTime = DateTime.UtcNow.Ticks;
                    client.Send(message);
                    break;
                case Cmd.MainBattleSvcSearch:
                    Sworld sworld= mainWorldSvc.GetWorld(message.Body.worldID);
                    LoginPCK loginPCK = new LoginPCK();
                    loginPCK.Head = new Message.Head();
                    loginPCK.Body = new LoginBody();
                    loginPCK.Head.Cmd = Cmd.MainBattleSvcSearch;
                    if(sworld != null)
                    {
                        loginPCK.Body.battleIp = sworld.ip;
                        loginPCK.Body.worldID = sworld.gameWorldID;
                        loginPCK.Body.Battleport = sworld.port;
                        loginPCK.Body.IsFull=sworld.Full;
                        loginPCK.Body.playerCount=sworld.splayers.Count;
                    }
                    else
                    {
                        loginPCK.Body.worldID =-1;
                    }
                    client.Send(loginPCK);
                    break;
            }
        }
    }
}
