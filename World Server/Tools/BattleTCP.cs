using IOCP;
using Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
using Word_Server.Comp;
using Word_Server.Item;
using Word_Server.Tank;
using Word_Sever.Client;
using Word_Sever.Servc;
using Word_Sever.World;

namespace Word_Server.Tools
{
    public class BattleTCP:IOCPServer<WorldPCK>
    {

        BattleWorld Sworld;
        public BattleTCP(BattleWorld sworld) :base(2048)
        {
            Sworld = sworld;
        }
        public override void AcceptClient(IOCPToken<WorldPCK> client)
        {

        }
        public override void OnCloseAccpet(IOCPToken<WorldPCK> client)
        {

        }
        public void SpawnCallBack()
        {
            NetWorkItemComp netWorkItemComp = Sworld.GetComp<NetWorkItemComp>();
            netWorkItemComp.SetEndDateTime(DateTime.UtcNow);
           
            foreach (var shu in Sworld.splayers)
            {
                WorldPCK worldPCK = new WorldPCK();
                worldPCK.Head = new Message.Head();
                worldPCK.Body = new WorldBody();
                worldPCK.Body.EndTime = netWorkItemComp.EndDateTime.ToString();
                if (shu.Value.GetComp<TransformComp>().TeamID==1)
                {
                    //放火
                    worldPCK.Head.Cmd = Cmd.WorldGameStart;
                }
                else
                {
                    //灭火
                    worldPCK.Head.Cmd = Cmd.WorldGameStartUnFireSpawn;
                    worldPCK.Body.Position = Map.TeamIDToMapVector3(0);
                    worldPCK.Body.Rotation = Map.TeamIDToMapEluer(0);
                    worldPCK.Body.CurrentHealthy = Sworld.splayers[shu.Key].GetComp<BufferComp>().currentHealthy;
                    worldPCK.Body.FixHealthy = Sworld.splayers[shu.Key].GetComp<BufferComp>().fixHealthy;
                }
                Sworld.players[shu.Key].Send(worldPCK);
            }
            Sworld.GetComp<TimeComp>().AddTimeSpawn(Spwanprop, 3);
        }
        public void Spwanprop()
        {
            AstartComp astart= Sworld.GetComp<AstartComp>();
            List<PathNode>nodes= astart.nodes;
            Random random = new Random();
            PathNode node= nodes[random.Next(nodes.Count)];
            while(node.IsBlock)
            {
                node = nodes[random.Next(nodes.Count)];
            }
            astart.moveNode = node;
            WorldPCK worldPCK = new WorldPCK();
            worldPCK.Head = new Message.Head();
            worldPCK.Body = new WorldBody();
            worldPCK.Head.Cmd=Cmd.WorldPropSpawn;
            worldPCK.Body.Position =new Message.Vector3() { X = node.bound.center.x, Y = node.bound.center.y, Z = node.bound.center.z };
            Sworld.log.LogGreen("道具出现在" + node.bound.center.ToString());
            Sworld.BorastCast(worldPCK);
        }
        public static int WaiteTime=5;
        public override void OnReceiveMessage(IOCPToken<WorldPCK> client, WorldPCK message)
        {
            switch (message.Head.Cmd)
            {
                case Cmd.WorldRegistPlayer:
                    if (!Sworld.splayers.ContainsKey(message.Body.Uid))
                        return;
                    if (!Sworld.players.ContainsKey(message.Body.Uid))
                    {
                        logaction.Invoke($"{message.Body.Uid}玩家登入服务器");

                        //BufferComp
                        BufferComp bufferComp = Sworld.splayers[message.Body.Uid].AddComp<BufferComp>();
                        bufferComp.SetHealthy(30);
                        bufferComp.SetPlayerSpeed(4);
                        //TransformComp
                        Sworld.splayers[message.Body.Uid].AddComp<TransformComp>().Awake();
                        Sworld.players.TryAdd(message.Body.Uid, client);
                        message.Head.Cmd = Cmd.WorldRegistPlayer;
                        //NetworkItemComp
                        NetWorkItemComp netWorkItemComp = Sworld.GetComp<NetWorkItemComp>();
                        message.Body.Tools.AddRange(netWorkItemComp.configs);
                        
                        client.Send(message);
                        if (Sworld.players.Count >= Sworld.maxCount)
                        {
                            WorldPCK worldPCK = new WorldPCK();
                            worldPCK.Head = new Message.Head();
                            worldPCK.Body = new WorldBody();
                            int index = -1;
                            foreach (var shu in Sworld.players)
                            {
                                index++;
                                int teamID = index % 2;
                                //teamID = 0;
                                Sworld.splayers[shu.Key].GetComp<TransformComp>().TeamID = teamID;
                                worldPCK.Body.TeamID = teamID;
                                if (teamID == 1)
                                {
                                    worldPCK.Head.Cmd = Cmd.WorldFireSpawn;
                                    worldPCK.Body.Position = Map.TeamIDToMapVector3(teamID);
                                    worldPCK.Body.Rotation = Map.TeamIDToMapEluer(teamID);
                                    worldPCK.Body.CurrentHealthy = Sworld.splayers[shu.Key].GetComp<BufferComp>().currentHealthy;
                                    worldPCK.Body.FixHealthy = Sworld.splayers[shu.Key].GetComp<BufferComp>().fixHealthy;
                                    shu.Value.Send(worldPCK);
                                }
                            }
                            Sworld.GetComp<TimeComp>().AddTimeSpawn(SpawnCallBack, WaiteTime);
                            WorldPCK timePck = new WorldPCK();
                            timePck.Head = new Message.Head();
                            timePck.Body = new WorldBody();
                            timePck.Head.Cmd = Cmd.WorldEndTimeAsync;
                            //timePck.Body.EndTime = netWorkItemComp.EndDateTime.ToString();
                            timePck.Body.EndTime = DateTime.UtcNow.AddSeconds(WaiteTime).ToString();
                            Sworld.BorastCast(timePck);
                        }
                        else
                        {
                            WorldPCK worldPCK = new WorldPCK();
                            worldPCK.Head = new Message.Head();
                            worldPCK.Body = new WorldBody();
                            worldPCK.Head.Cmd = Cmd.WorldWaitPlayer;
                            worldPCK.Body.WaiterNowPlayerCount = Sworld.players.Count;
                            worldPCK.Body.WaiterMax = Sworld.maxCount;
                            Sworld.BorastCast(worldPCK);
                        }
                    }
                    else
                    {
                        Sworld.players[message.Body.Uid] = client;
                        logaction.Invoke("ReRegist World successful");
                    }
                    break;
                case Cmd.WorldExit:
                    if (Sworld.splayers.TryGetValue(message.Body.Uid,out SPlayer value1))
                    {
                        logaction.Invoke($"玩家 [nickname:{value1.nickName}][uid:{value1.uid}] 退出游戏频道");
                        value1.Quit();
                    }
                    break;
                case Cmd.WorldMove:
                    if (Sworld.splayers.TryGetValue(message.Body.Uid,out SPlayer value2))
                    {
                        TransformComp configTrans = value2.GetComp<TransformComp>();
                        if (configTrans != null)
                        {
                            if (configTrans.waitTickCount < TransformComp.mustWaitCount)
                                configTrans.waitTickCount++;
                            BufferComp bufferComp = configTrans.splayer.GetComp<BufferComp>();
                            if (message.Body.IsFast)
                            {
                                if (configTrans.SetTransformConfig(message.Body.Position, message.Body.moveDir, message.Body.Rotation, 6, message.Body.DateTime))
                                    bufferComp.SetPlayerSpeed(6);
                                else
                                    break;
                            }
                            else
                            {
                                if(configTrans.SetTransformConfig(message.Body.Position, message.Body.moveDir, message.Body.Rotation, 4, message.Body.DateTime))
                                    bufferComp.SetPlayerSpeed(4);
                                else
                                    break;
                            }
                            configTrans.SetHeadQuaternion(message.Body.Headrotation);
                            configTrans.UpdateConfig(true);

                            BufferComp speedComp = configTrans.splayer.GetComp<BufferComp>();
                            speedComp.isMove = message.Body.IsMove;
                            //log.LogWhite($"[{splayers[player.uid].nickName}][Move {configTrans.position}][Roation {configTrans.rotation}]");
                        }
                    }
                    break;
                case Cmd.WorldItemFire:

                    if (Sworld.GetComp<NetWorkItemComp>().GetNetWorkTool(message.Body.GrabItemID, out NetWorkTool netWorkToolfire))
                    {
                        logaction.Invoke($"玩家:{message.Body.Uid} 试图放{message.Body.itemID}火");
                        if (netWorkToolfire.value <= netWorkToolfire.UseTime)
                        {
                            CenterFire centerFire = Sworld.GetComp<NetWorkItemComp>().fireSystem.GetCenerFireById(message.Body.itemID);
                            if (centerFire != null)
                            {
                                centerFire.Fire();
                            }
                            byte[] ItemFirebts = IOCPToken<WorldPCK>.Serialize(message);
                            foreach (var item in Sworld.players)
                            {
                                item.Value.Send(ItemFirebts);
                            }
                        }
                    }
                    break;
                case Cmd.WorldUnItemFire:
                    if (Sworld.GetComp<NetWorkItemComp>().GetNetWorkTool(message.Body.GrabItemID, out NetWorkTool netWorkToolunfire))
                    {
                        logaction.Invoke($"玩家:{message.Body.Uid} 试图灭{message.Body.itemID}火");
                        if (netWorkToolunfire.value <= netWorkToolunfire.UseTime)
                        {
                            CenterFire centerFire = Sworld.GetComp<NetWorkItemComp>().fireSystem.GetCenerFireById(message.Body.itemID);
                            if (centerFire != null)
                            {
                                centerFire.UnFire();
                            }
                            byte[] UnFirebts = IOCPToken<WorldPCK>.Serialize(message);
                            foreach (var item in Sworld.players)
                            {
                                item.Value.Send(UnFirebts);
                            }
                        }
                    }
                    break;
                case Cmd.WorldAddBuffer:
                    //Sworld.splayers[message.Body.Uid].GetComp<BufferComp>().AddBuffer(message.Body.skillId,message.Body.DateTime);
                    break;
                case Cmd.WorldIteraction:
                    byte[] items = IOCPToken<WorldPCK>.Serialize(message);
                    foreach (var item in Sworld.players)
                    {
                        if (item.Value == client) continue;
                        item.Value.Send(items);
                    }
                    logaction.Invoke($"玩家:{message.Body.Uid} 与{message.Body.itemID}发生交互");
                    break;
                case Cmd.WorldJump:
                    byte[] jumpitems = IOCPToken<WorldPCK>.Serialize(message);
                    foreach (var item in Sworld.players)
                    {
                        if (item.Value == client) continue;
                        item.Value.Send(jumpitems);
                    }
                    logaction.Invoke($"玩家:{message.Body.Uid} 跳跃");
                    break;
                case Cmd.WorldFire:
                    byte[] bts = IOCPToken<WorldPCK>.Serialize(message);
                    logaction.Invoke("Fire " + message.Body.GrabItemID);
                    if (Sworld.GetComp<NetWorkItemComp>().GetNetWorkTool(message.Body.GrabItemID, out NetWorkTool netWorkTool))
                    {
                        if (message.Body.IsFire)
                        {
                            if (netWorkTool.Fire())
                            {
                                foreach (var item in Sworld.players)
                                {
                                    if (item.Value == client) continue;
                                    item.Value.Send(bts);
                                }
                            }
                            else
                            {
                                netWorkTool.PowerRefuse();
                            }
                        logaction.Invoke($"玩家:{message.Body.Uid} 使用道具{message.Body.GrabItemID}");
                        }
                        else
                        {
                            netWorkTool.UnFire();
                            foreach (var item in Sworld.players)
                            {
                                if (item.Value == client) continue;
                                item.Value.Send(bts);
                            }
                        logaction.Invoke($"玩家:{message.Body.Uid} 停止使用道具{message.Body.GrabItemID}");
                        }
                    }
                    break;
                case Cmd.WorldGrab:
                    byte[] Grabbts = IOCPToken<WorldPCK>.Serialize(message);
                    if (message.Body.IsLocalNetwork)
                    {
                        foreach (var item in Sworld.players)
                        {
                            if (item.Value == client) continue;
                            item.Value.Send(Grabbts);
                        }
                        logaction.Invoke($"玩家:{message.Body.Uid} 拾取本地{message.Body.GrabItemID}");
                        break;
                    }
                    if (Sworld.GetComp<NetWorkItemComp>().GetNetWorkTool(message.Body.GrabItemID, out NetWorkTool netWorkTool2))
                    {
                        /*foreach (var item in Sworld.players)
                        {
                            if (item.Value == client) continue;
                            item.Value.Send(Grabbts);
                        }*/
                        if (netWorkTool2.Grab(client))
                        {
                            foreach (var item in Sworld.players)
                            {
                                if (item.Value == client) continue;
                                item.Value.Send(Grabbts);
                            }
                        }
                        else
                        {
                            client.Send(Grabbts);
                        }
                    logaction.Invoke($"玩家:{message.Body.Uid} 拾取{message.Body.GrabItemID}");
                    }
                    break;
                case Cmd.WorldThrow:
                    byte[] throwbyts = IOCPToken<WorldPCK>.Serialize(message);
                    if(message.Body.IsLocalNetwork)
                    {
                        foreach (var item in Sworld.players)
                        {
                            if (item.Value == client) continue;
                            item.Value.Send(throwbyts);
                        }
                    logaction.Invoke($"玩家:{message.Body.Uid} 扔下本地{message.Body.GrabItemID}");
                        break;
                    }
                    if (Sworld.GetComp<NetWorkItemComp>().GetNetWorkTool(message.Body.GrabItemID, out NetWorkTool netWorkTool3))
                    {
                        netWorkTool3.Throw();
                        foreach (var item in Sworld.players)
                        {
                            if (item.Value == client) continue;
                            item.Value.Send(throwbyts);
                        }
                    logaction.Invoke($"玩家:{message.Body.Uid} 扔下{message.Body.GrabItemID}");
                    }
                    break;
                case Cmd.WorldSitDown:
                    byte[] sitdownbyts = IOCPToken<WorldPCK>.Serialize(message);
                    foreach (var item in Sworld.players)
                    {
                        if (item.Value == client) continue;
                        item.Value.Send(sitdownbyts);
                    }
                    logaction.Invoke($"玩家:{message.Body.Uid} 下蹲/起立");
                    break;
                case Cmd.WorldMark:
                    SPlayer owner = Sworld.splayers[message.Body.Uid];
                    int teamid = owner.GetComp<TransformComp>().TeamID;
                    foreach (var item in Sworld.splayers)
                    {
                        if (item.Value == owner) continue;
                        else if (item.Value.GetComp<TransformComp>().TeamID == teamid)
                        {
                            Sworld.players[item.Key].Send(message);
                        }
                    }
                    logaction.Invoke($"玩家:{owner.nickName} 标记了一处地点");
                    break;
                case Cmd.WorldPlayerFireSelf:
                    byte[] PlayerFireSelf = IOCPToken<WorldPCK>.Serialize(message);
                    if(Sworld.splayers.TryGetValue(message.Body.Uid,out SPlayer player))
                    {
                        player.GetComp<BufferComp>().Damage(ToolsDocument.GetFireDamage((ToolsDocument.FireType)(message.Body.TeamID)));
                    }
                    logaction.Invoke($"玩家:{message.Body.Uid} 着火 {message.Body.UseTimeAddTime}");
                    break;
                case Cmd.WorldPlayerSave:
                    if (Sworld.splayers.TryGetValue(message.Body.Uid, out SPlayer player2))
                    {
                        player2.GetComp<BufferComp>().SetProgress(10);
                    }
                    break;
                case Cmd.WorldPlayerUnFireSelf:
                    if (Sworld.splayers.TryGetValue(message.Body.Uid, out SPlayer playerunfire))
                    {
                        message.Body.CurrentHealthy = playerunfire.GetComp<BufferComp>().currentHealthy;
                        byte[] PlayerUnFireSelf = IOCPToken<WorldPCK>.Serialize(message);
                        foreach (var item in Sworld.players)
                        {
                            item.Value.Send(PlayerUnFireSelf);
                        }
                        logaction.Invoke($"玩家:{message.Body.Uid} 停止着火 {message.Body.UseTimeAddTime}");
                    }
                    break;
                case Cmd.WorldToolTimeAdd:
                    if(Sworld.GetComp<NetWorkItemComp>().GetNetWorkTool(message.Body.itemID,out NetWorkTool network))
                    {
                        network.value= Math.Max(0,network.value-message.Body.UseTimeAddTime);
                        logaction.Invoke($"ItemID:{network.netWorkID}加耐久 {message.Body.UseTimeAddTime}");
                    }
                    break;
                case Cmd.WorldPropGrab:
                    AstartComp comp=Sworld.GetComp<AstartComp>();
                    if (Sworld.splayers.TryGetValue(message.Body.Uid, out SPlayer sPlayer))
                    {
                        TransformComp playercomp = sPlayer.GetComp<TransformComp>();
                        if (!comp.isRuning)
                        {
                            int aimTeam = -1;
                            if (playercomp.TeamID == 1)
                            {
                                aimTeam = 0;
                            }
                            else
                            {
                                aimTeam = 1;
                            }
                            foreach (var Astartplayer in Sworld.splayers.Values)
                            {
                                TransformComp trans = Astartplayer.GetComp<TransformComp>();
                                if (trans.TeamID == aimTeam)
                                {
                                    comp.Enity.Add(trans);
                                }
                            }
                            comp.RunAstart();
                            WorldPCK worldPCK = new WorldPCK();
                            worldPCK.Body = new WorldBody();
                            worldPCK.Head = new Message.Head();
                            worldPCK.Head.Cmd = Cmd.WorldRobotOpen;
                            worldPCK.Body.Position = new Message.Vector3() { X = comp.moveNode.bound.center.x, Y = comp.moveNode.bound.center.y, Z = comp.moveNode.bound.center.z };
                            Sworld.BorastCast(worldPCK);
                        }
                    }
                    break;
            }
        }
    }
}
