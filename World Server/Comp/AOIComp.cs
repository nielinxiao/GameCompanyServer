using Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Word_Server.Tools;
using Word_Sever.Client;
using Word_Sever.Interface;
using Word_Sever.Servc;
using Word_Sever.Tools;
using Word_Sever.World;

namespace Word_Server.Comp
{
    public class AOIComp : WorldComp, IUpdate,IAWake
    {
        public float aoi_radius=20;
        public float Interval=0.05f;
        DateTime timer;
        BattleWorld world;

        public void Awake()
        {
            timer = DateTime.UtcNow;
            world = (BattleWorld)sworld;
        }

        public bool InBounds(Word_Server.Tools.Vector3 player, Word_Server.Tools.Vector3 other)
        {
            return true;
            if ((player - other).Length > aoi_radius)
                return false;
            else
                return true;
        }
        public void Update()
        {
            if ((DateTime.UtcNow - timer).TotalSeconds > Interval)
            {
                timer = DateTime.UtcNow;
                foreach (var uid in world.players)
                {
                    List<Message.Player> configs = new List<Message.Player>();
                    if (!world.splayers.ContainsKey(uid.Key)) continue;
                    TransformComp playerTrans = world.splayers[uid.Key].GetComp<TransformComp>();
                    foreach (var other in world.players)
                    {
                        if (!world.splayers.ContainsKey(other.Key) || other.Key == uid.Key) continue;

                        SPlayer sPlayer = world.splayers[other.Key];
                        TransformComp trans = sPlayer.GetComp<TransformComp>();
                        BufferComp buffer = sPlayer.GetComp<BufferComp>();
                        //world.log.LogGreen(trans.ToString());
                        if (buffer != null&&trans.UpdatePosition && InBounds(playerTrans.position, trans.position) && trans.waitTickCount >= TransformComp.mustWaitCount)
                        {
                            //world.log.LogYellow($"uid:[{other.Key}] predict :[{trans.predict}] rotation:[{trans.rotation}]");
                            configs.Add(new Player
                            {
                                Position = new Message.Vector3() { X = trans.predict.x, Y = trans.predict.y, Z = trans.predict.z },
                                Rotation = new Message.Vector3() { X = trans.rotation.x, Y = trans.rotation.y, Z = trans.rotation.z },
                                Uid = other.Key,
                                Currentspeed = buffer.CurrentMoveSpeed+buffer.currentSpeedOffset,
                                moveDir = new Message.Vector3()
                                {
                                    X = trans.normal.x,
                                    Y = trans.normal.y,
                                    Z = trans.normal.z
                                },
                                DateTime= DateTime.UtcNow.Ticks,
                                nickName = sPlayer.nickName,
                                IsMove=buffer.isMove,
                                Headrotation= new Message.Vector3()
                                {
                                    X = trans.Headrotation.x,
                                    Y = trans.Headrotation.y,
                                    Z = trans.Headrotation.z
                                },
                                TeamId=trans.TeamID,
                            });
                        }
                    }
                    if (configs.Count > 0)
                    {
                        WorldPCK worldPCK = new WorldPCK();
                        worldPCK.Body = new WorldBody();
                        worldPCK.Head = new Message.Head();
                        worldPCK.Head.Cmd=Cmd.WorldMove;
                        worldPCK.Body.Players.AddRange(configs);
                        uid.Value.Send(worldPCK);
                    }
                }
                foreach (var uid in world.splayers)
                {
                   TransformComp transformComp= uid.Value.GetComp<TransformComp>();
                    if (transformComp != null)
                    {
                        transformComp.UpdateConfig(false);
                    }
                }
            }
        }
    }
}