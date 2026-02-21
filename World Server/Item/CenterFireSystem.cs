using IOCP;
using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Comp;
using Word_Sever;
using Word_Sever.Servc;
using Word_Sever.World;

namespace Word_Server.Item
{
    public class CenterFireSystem
    {
        private List<CenterFire> Fires;
        Action Complite;
        public int EndScores;
        bool isWaiterForRegistCenter = true;
        Dictionary<int,CenterFire>keyValuePairs = new Dictionary<int,CenterFire>();
        BattleWorld sworld;
        public void RegistCenter(List<CenterFire>fires,Action OnComplite,int EndSocre,BattleWorld sworld)
        {
            Fires = fires;
            AllSocres = 0;
            Complite=OnComplite;
            EndScores = EndSocre;
            isWaiterForRegistCenter = false;
            this.sworld = sworld;
            foreach (CenterFire fi in Fires) 
            {
                keyValuePairs.Add(fi.netWorkID, fi);
            }
        }
        public CenterFire GetCenerFireById(int id)
        {
            if(keyValuePairs.TryGetValue(id,out CenterFire centerFire))
            {
                return centerFire;
            }
            return null;
        }

        public double AllSocres = 0;
        public static float TickTime = 1;
        public DateTime LastTickTime=default(DateTime);
        public void Tick()
        {
            if (isWaiterForRegistCenter)
                return;
            if(LastTickTime==default(DateTime)||(DateTime.UtcNow- LastTickTime).TotalSeconds>TickTime)
            {
                LastTickTime = DateTime.UtcNow;
            }
            else
            {
                return;
            }
            foreach (var item in Fires)
            {
                double score = item.GetScore();
                if (score!=0)
                {
                    AllSocres += score;
                    OpenProgress.Instance.log.LogWhite("正在燃烧中枢 燃烧进度 :"+ AllSocres);
                }
            }
            if(AllSocres>=EndScores)
            {
                isWaiterForRegistCenter=true;
                Complite.Invoke();
                keyValuePairs.Clear();
            }
            else
            {
                WorldPCK worldPCK = new WorldPCK();
                worldPCK.Body = new WorldBody();
                worldPCK.Head = new Message.Head();
                worldPCK.Head.Cmd = Cmd.WorldScore;
                int socre = Convert.ToInt32(AllSocres);
                worldPCK.Body.Score = socre;
                byte[] bytes = IOCPToken<WorldPCK>.Serialize(worldPCK);
                foreach (var item in sworld.players)
                {
                    item.Value.Send(bytes);
                }
            }
        }
    }
}
