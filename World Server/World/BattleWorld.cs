using Message;
using Word_Sever.Servc;
using System;
using System.Collections.Generic;
using Word_Sever.Client;
using Word_Server.Comp;
using System.Net.Sockets;
using System.Collections.Concurrent;
using Word_Server.Tank;
using Word_Sever.Tools;
using System.Diagnostics;
using IOCP;
using Word_Server.Tools;

namespace Word_Sever.World
{
    public class BattleWorld : Sworld
    {
        public ConcurrentDictionary<string, IOCPToken<WorldPCK>> players = new ConcurrentDictionary<string, IOCPToken<WorldPCK>>();
        public BattleWorld(GameContainer container,string ip, int gameWorldID, int maxCount, string worldName, ushort port, DateTime creatTime, int containerID) : base(container, ip, gameWorldID, maxCount, worldName, port, creatTime, containerID) { }
        public BattleTCP battleTCP;
        public override void Awake()
        {
            base.Awake();
            battleTCP = new BattleTCP(this);
            battleTCP.InitIOCPServer(log.LogYellow, 10, true, open.pEConfig.battleBindIp, port);
        }
        public override void TryRemovePlayer(SPlayer sPlayer)
        {
            base.TryRemovePlayer(sPlayer);
            lock (players)
                if (players.TryRemove(sPlayer.uid, out IOCPToken<WorldPCK> wo))
                {
                    battleTCP.RemoveToken(wo);
                    GetComp<AudioComp>().CloseToken(sPlayer.uid);
                    log.LogYellow($"[nickname:{sPlayer.nickName}][uid:{sPlayer.uid}] quit battleWorld successful");
                }
                else
                {
                    log.LogYellow($"[nickname:{sPlayer.nickName}][uid:{sPlayer.uid}] quit battleWorld failed");
                }
        }
        public override void TryRemoveAllPlayer()
        {
            base.TryRemoveAllPlayer();
            AudioComp comp = GetComp<AudioComp>();
            foreach (var player in players)
            {
                comp.CloseToken(player.Key);
                battleTCP.RemoveToken(player.Value);
            }
            players.Clear();
        }
        public void BorastCast(WorldPCK baseMsg)
        {
            byte[]bytes=  IOCPToken<WorldPCK>.Serialize(baseMsg);
            foreach (var item in players)
            {
                item.Value.Send(bytes);
            }
        }
        public void BorastCast(WorldPCK baseMsg, string uid)
        {
            byte[] bytes = IOCPToken<WorldPCK>.Serialize(baseMsg);
            foreach (var item in players)
            {
                if(item.Key == uid) continue;
                item.Value.Send(bytes);
            }
        }
        public override void Update()
        {
            base.Update();
            lock(splayers)
            foreach (var player in splayers.Values)
            {
                player.Update();
            }
        }
        public override void Destory()
        {
            TryRemoveAllPlayer();
            battleTCP.CloseServer();
            base.Destory();
        }
    }
}