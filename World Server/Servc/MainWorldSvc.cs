using Message;
using Word_Sever.Client;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using IOCP;
using Word_Server.Comp;
namespace Word_Sever.Servc
{
    public class MainWorldSvc : IService
    {
        /// <summary>
        /// 全部世界
        /// </summary>
        protected ConcurrentDictionary<int, Sworld>Worlds=new ConcurrentDictionary<int, Sworld>();
        /// <summary>
        /// 匹配世界列表
        /// </summary>
        protected List<int> RandomWorld = new List<int>();
        /// <summary>
        /// 正常房间等待人数
        /// </summary>
        protected ConcurrentDictionary<int, List<SPlayer>> Waiters = new ConcurrentDictionary<int, List<SPlayer>>();
        /// <summary>
        /// 匹配房间等待人数
        /// </summary>
        protected ConcurrentDictionary<int, List<SPlayer>> RandomWaiters = new ConcurrentDictionary<int, List<SPlayer>>();
        /// <summary>
        /// Container全部
        /// </summary>
        protected List<GameContainer> containers = new List<GameContainer>();
        /// <summary>
        /// 匹配创建房间队列
        /// </summary>
        protected ConcurrentQueue<int> Randomgames = new ConcurrentQueue<int>();
        /// <summary>
        /// 创建房间队列
        /// </summary>
        protected ConcurrentQueue<int> games = new ConcurrentQueue<int>();
        LogTool log;
        OpenProgress openProgress;
        public MainWorldSvc()
        {
            openProgress=OpenProgress.Instance;
            this.log = openProgress.log;
        }
        public void Init()
        {
            for(int s=0; s< openProgress.pEConfig.worldContainerCount;s++)
            {
                GameContainer container = new GameContainer(s);
                container.Awake();
                containers.Add(new GameContainer(s));
            }
            log.LogGreen($"BattleWorldSvc Add [{openProgress.pEConfig.worldContainerCount}] Containers");
        }
        public void Tick()
        {
            if(games.TryDequeue(out int worldID))
            {
                int containersID = (int)(worldID / 10);
                int port=worldID % 10;
                containers[containersID].AddGameWorld(openProgress.pEConfig.battleIp, worldID, openProgress.pEConfig.battlePortBase + containersID * 10 + port);
            }
            if (Randomgames.TryDequeue(out int worldID2))
            {
                GameContainer container = GetFreeWorldContainer();
                RandomWorld.Add(worldID2);
                container.AddGameWorld(openProgress.pEConfig.battleIp, worldID2, openProgress.pEConfig.battlePortBase + container.ContainerID * 10 + container.Worlds.Count + 1);
            }
            foreach (GameContainer container in containers)
                container.Update();
        }
        public void ExitWorld(Sworld sworld, SPlayer Player)
        {
            if(sworld != null)
            {
                sworld.TryRemovePlayer(Player);
                if(sworld.splayers.Count<=0)
                {
                    Worlds.TryRemove(sworld.gameWorldID, out Sworld sworld2);
                    sworld2.parentContainer.RemoveGameWorld(sworld2);
                }
                if(RandomWorld.Contains(sworld.gameWorldID))
                {
                    RandomWorld.Remove(sworld.gameWorldID);
                }
            }
            else
            {
                log.LogError($"World[{sworld.gameWorldID}] is null but {Player.nickName} want to exit");
            }
        }
        public void RemoveWorld(Sworld sworld)
        {
            sworld.TryRemoveAllPlayer();
            if (RandomWorld.Contains(sworld.gameWorldID))
            {
                RandomWorld.Remove(sworld.gameWorldID);
            }
            Worlds.TryRemove(sworld.gameWorldID, out Sworld sworld2);
            sworld.parentContainer.RemoveGameWorld(sworld);
        }
        int cacheCount=0;
        int RandomcacheCount=0;
        //进入世界 没有创建
        public void EnterWorld(int WorldID,SPlayer Player)
        {
            if(WorldID<=-1)
            {
                Interlocked.Increment(ref RandomcacheCount);
                Sworld sworld = null;
                foreach (var shu in RandomWorld)
                {
                    if (!Worlds[shu].Full)
                    {
                        sworld = Worlds[shu];
                    }

                }
                if (sworld == null)
                {
                    GameContainer container = GetFreeWorldContainer();
                    int worldid = (container.ContainerID) * openProgress.pEConfig.ContainerCreatWorldCount + container.Worlds.Count + 1;
                    bool Created = false;
                    RandomWaiters.AddOrUpdate(worldid, new List<SPlayer>() { Player },
                        (x, y) =>
                        {
                            if (Waiters.Count >= sworld.maxCount)
                            {
                                LoginPCK login=new LoginPCK();
                                login.Head = new Message.Head();
                                login.Head.Cmd = Cmd.MainBattleSvcRefuseEnter;
                                Player.token.Send(login);
                            }
                            else
                            {
                                y.Add(Player);
                            }
                            Created = true;
                            return y;
                        }
                        );
                    if (!Created)
                    {
                        Randomgames.Enqueue(worldid);
                    }
                }
                else
                {
                    Sworld.EnterMode enterMode = sworld.TryAddPlayer(Player);
                    if (enterMode != Sworld.EnterMode.Full)
                    {
                        LoginPCK login = new LoginPCK();
                        login.Head = new Message.Head();
                        login.Body = new LoginBody();
                        login.Head.Cmd = Cmd.MainBattleSvcAllowEnter;
                        login.Body.battleIp = sworld.ip;
                        login.Body.Battleport = sworld.port;
                        login.Body.worldID = sworld.gameWorldID;
                        login.Body.Players.AddRange(sworld.GetAllPlayerConfig());
                        login.Body.Voiceport = sworld.GetComp<AudioComp>().port;
                        login.Body.VoiceIp = sworld.ip;
                        Player.token.Send(login);
                    }
                    else
                    {
                        LoginPCK login = new LoginPCK();
                        login.Head = new Message.Head();
                        login.Head.Cmd = Cmd.MainBattleSvcRefuseEnter;
                        Player.token.Send(login);
                    }
                }
                Interlocked.Decrement(ref RandomcacheCount);
            }
            else if(WorldID>openProgress.pEConfig.worldContainerCount*openProgress.pEConfig.ContainerCreatWorldCount)
            {
                log.LogYellow($"[nickName:{Player.nickName}][uid:{Player.uid}] send error worldID which is can`t exist");
            }
            else
            {
                Interlocked.Increment(ref cacheCount);
                Sworld sworld = GetWorld(WorldID);
                if (sworld != null)
                {
                    Sworld.EnterMode enterMode = sworld.TryAddPlayer(Player);
                    if (enterMode != Sworld.EnterMode.Full)
                    {
                        LoginPCK login = new LoginPCK();
                        login.Head = new Message.Head();
                        login.Body = new LoginBody();
                        login.Head.Cmd = Cmd.MainBattleSvcAllowEnter;
                        login.Body.battleIp = sworld.ip;
                        login.Body.Battleport = sworld.port;
                        login.Body.worldID = sworld.gameWorldID;
                        login.Body.Players.AddRange(sworld.GetAllPlayerConfig());
                        login.Body.Voiceport = sworld.GetComp<AudioComp>().port;
                        login.Body.VoiceIp = sworld.ip;
                        Player.token.Send(login);
                    }
                    else
                    {
                        LoginPCK login = new LoginPCK();
                        login.Head = new Message.Head();
                        login.Head.Cmd = Cmd.MainBattleSvcRefuseEnter;
                        Player.token.Send(login);
                    }
                }
                else
                {
                    bool Created = false;
                    Waiters.AddOrUpdate(WorldID, new List<SPlayer>() { Player },
                        (x, y) =>
                        {
                            if (Waiters.Count >= sworld.maxCount)
                            {
                                LoginPCK login = new LoginPCK();
                                login.Head=new Message.Head();
                                login.Head.Cmd = Cmd.MainBattleSvcRefuseEnter;
                                Player.token.Send(login);
                            }
                            else
                            {
                                y.Add(Player);
                            }
                            Created = true;
                            return y;
                        }
                        );
                    if (!Created)
                    {
                        games.Enqueue(WorldID);
                    }
                }
                Interlocked.Decrement(ref cacheCount);
            }
        }
        //获取是否有世界
        public Sworld GetWorld(int WorldID)
        {
            if (Worlds.TryGetValue(WorldID, out Sworld sworld))
            {
                return sworld;
            }
            else
                return null;
        }
        public GameContainer GetFreeWorldContainer()
        {
            foreach(var shu in containers)
            {
                if (shu.Worlds.Count >= openProgress.pEConfig.ContainerCreatWorldCount)
                    continue;
                else
                    return shu;
            }
            return null;
        }
        //注销
        public void UnInit()
        {
            for(int s=0;s< containers.Count;s++)
            {
                containers[s].Destory();
            }
            containers.Clear();
        }
        //创建完成世界的回调
        public void AddGameWorld(Sworld sworld)
        {
            Worlds.TryAdd(sworld.gameWorldID, sworld);
            if(RandomWorld.Contains(sworld.gameWorldID))
            {
                while (true)
                {
                    if (RandomcacheCount == 0)
                    {
                        if (RandomWaiters.TryRemove(sworld.gameWorldID, out List<SPlayer> roles))
                        {
                            for (int s = 0; s < roles.Count; s++)
                            {
                                sworld.TryAddPlayer(roles[s]);
                                LoginPCK login = new LoginPCK();
                                login.Head = new Message.Head();
                                login.Body = new LoginBody();
                                login.Head.Cmd = Cmd.MainBattleSvcAllowEnter;
                                login.Body.battleIp = sworld.ip;
                                login.Body.Battleport = sworld.port;
                                login.Body.worldID = sworld.gameWorldID;
                                login.Body.Players.AddRange(sworld.GetAllPlayerConfig());
                                login.Body.Voiceport = sworld.GetComp<AudioComp>().port;
                                login.Body.VoiceIp = sworld.ip;
                                byte[] bytes = IOCPToken<LoginPCK>.Serialize(login);
                                roles[s].token.Send(bytes);
                            }
                        }
                        break;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (cacheCount == 0)
                    {
                        if (Waiters.TryRemove(sworld.gameWorldID, out List<SPlayer> roles))
                        {
                            for (int s = 0; s < roles.Count; s++)
                            {
                                sworld.TryAddPlayer(roles[s]);
                                LoginPCK login = new LoginPCK();
                                login.Head = new Message.Head();
                                login.Body = new LoginBody();
                                login.Head.Cmd = Cmd.MainBattleSvcAllowEnter;
                                login.Body.battleIp = sworld.ip;
                                login.Body.Battleport = sworld.port;
                                login.Body.worldID = sworld.gameWorldID;
                                login.Body.Players.AddRange(sworld.GetAllPlayerConfig());
                                login.Body.Voiceport = sworld.GetComp<AudioComp>().port;
                                login.Body.VoiceIp = sworld.ip;
                                byte[] bytes = IOCPToken<LoginPCK>.Serialize(login);
                                roles[s].token.Send(bytes);
                            }
                        }
                        break;
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            }
        }
    }
}
