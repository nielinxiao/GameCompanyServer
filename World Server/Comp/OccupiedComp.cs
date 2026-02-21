using IOCP;
using Message;
using System;
using System.Collections.Generic;
using Word_Server.Tank;
using Word_Server.Tools;
using Word_Sever;
using Word_Sever.Interface;
using Word_Sever.Servc;
using Word_Sever.Tools;
using Word_Sever.World;
using static Word_Server.Tank.RealmRoot;

namespace Word_Server.Comp
{
    public class OccupiedComp : WorldComp,/* IUpdate,*/IAWake,IDestory
    {
        Dictionary<int, Realm> Realms=new Dictionary<int, Realm>();
        public void EnterOccupied(string uid,int Realmid)
        {
            Realm realm = RealmRoot.GetRealm(Realmid);
            TransformComp playerPos =sworld.splayers[uid].GetComp<TransformComp>();
            if( playerPos == null)
                return;
            if ((realm.position - playerPos.predict).Length <= realm.Radius)
            {
                if (!Realms[Realmid].blue.Contains(playerPos))
                {
                    Realms[Realmid].blue.Add(playerPos);
                }
            }
        }
        public const float TickTime = 1;
        public DateTime nowTime=default(DateTime);
        public float UpperSpeed = 1/ (10*TickTime);//20
        public int maxScore=1500;
        public static float ClampMin(float value,float minValue)
        {
            if(value <= minValue)
                return minValue;
            else return value;
        }
        public static float ClampMax(float value, float maxValue)
        {
            if (value >= maxValue)
                return maxValue;
            else return value;
        }
        /*public void Update()
        {
            if(nowTime==default(DateTime)||(DateTime.UtcNow -nowTime).TotalSeconds>TickTime)
            {
                nowTime = DateTime.UtcNow;
                List<TransformComp> comps = new List<TransformComp>();
                List<Occupied>occups=new List<Occupied>();
                for (int i = 0; i < Realms.Count; i++)
                {
                    Queue<TransformComp> q = new Queue<TransformComp>();
                    foreach (var blue in Realms[i].blue)
                    {
                        if ((blue.predict - Realms[i].position).Length > Realms[i].Radius || 
                            blue.splayer.GetComp<TankComp>() == null||
                            !sworld.splayers.ContainsKey(blue.splayer.uid))
                        {
                            q.Enqueue(blue);
                        }
                    }
                    while (q.Count > 0)
                        Realms[i].blue.Remove(q.Dequeue());
                    foreach (var red in Realms[i].red)
                    {
                        if ((red.predict - Realms[i].position).Length > Realms[i].Radius||
                            red.splayer.GetComp<TankComp>()==null||
                            !sworld.splayers.ContainsKey(red.splayer.uid))
                        {
                            q.Enqueue(red);
                        }
                    }
                    while (q.Count > 0)
                        Realms[i].red.Remove(q.Dequeue());


                    if (Realms[i].Progress != 1)
                    {
                        if (Realms[i].red.Count > 0 && Realms[i].blue.Count <= 0)
                        {
                            foreach (var red in Realms[i].red)
                            {
                                red.splayer.GetComp<exploitsComp>().Occupied((int)(maxScore * UpperSpeed));
                            }
                            Realms[i].Progress += UpperSpeed;
                            Realms[i].Progress =ClampMax(Realms[i].Progress, 1);
                        }
                    }
                    if(Realms[i].Progress != -1)
                    {
                        if (Realms[i].red.Count <= 0 && Realms[i].blue.Count > 0)
                        {
                            foreach (var blue in Realms[i].blue)
                            {
                                blue.splayer.GetComp<exploitsComp>().Occupied((int)(maxScore * UpperSpeed));
                            }
                            Realms[i].Progress -= UpperSpeed;
                            Realms[i].Progress = ClampMin(Realms[i].Progress, -1);
                        }
                    }
                    if (Realms[i].Progress != 0&& Realms[i].Progress!=1 && Realms[i].Progress != -1)
                        sworld.log.LogGreen($"occupiedID:{i} progress {Realms[i].Progress}");
                    occups.Add(new Occupied() { Id = i, Progress = Realms[i].Progress });
                }
                WorldPCK worldPCK=new WorldPCK();
                worldPCK.Body = new WorldBody();
                worldPCK.Head = new Message.Head();
                worldPCK.Head.Cmd = Cmd.WorldEnterOccup;
                worldPCK.Body.Occupieds.AddRange(occups);
                byte[] bytes = IOCPToken<WorldPCK>.Serialize(worldPCK);
                foreach (var client in ((BattleWorld)sworld).players)
                {
                    client.Value.Send(bytes);
                }

            }
        }*/

        public void Awake()
        {
            Realms.Add(0, RealmRoot.GetRealm(0));
        }

        public void Destory()
        {
            Realms.Clear();
        }
    }
}
