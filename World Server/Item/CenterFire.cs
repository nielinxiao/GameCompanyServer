using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Word_Server.Item
{
    public class CenterFire
    {
        public readonly int netWorkID;
        public readonly float FireTime;
        public CenterFire(int networkID,int FireTime)
        {
            netWorkID = networkID;
            this.FireTime = FireTime;
        }
        DateTime FireStartDate;
        DateTime UnFireStartDate;
        bool IsFiring = false;
        public void Fire()
        {
            if(!IsFiring)
            {
                IsFiring = true;
                FireStartDate = DateTime.UtcNow;
            }
        }
        public double MulitScore = 0;
        public static int MaxSocre=40;//120/3
        public void UnFire()
        {
            if(IsFiring)
            {
                IsFiring = false;
                UnFireStartDate=DateTime.UtcNow;
            }
        }
        public double GetScore()
        {
            if (IsFiring) 
            {
                if (MulitScore < MaxSocre)
                {
                    double seconds = (DateTime.UtcNow - FireStartDate).TotalSeconds;
                    FireStartDate = DateTime.UtcNow;
                    if(MulitScore+seconds > MaxSocre)
                    {
                        double add= MaxSocre- MulitScore;
                        MulitScore =MaxSocre;
                        return add;
                    }
                    else
                    {
                        MulitScore += seconds;
                        return seconds;
                    }
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                double seconds = (DateTime.UtcNow - UnFireStartDate).TotalSeconds;
                UnFireStartDate = DateTime.UtcNow;
                if(MulitScore > 0)
                {
                    if(MulitScore- seconds < 0)
                    {
                        double end = MulitScore;
                        MulitScore = 0;
                        return -end;
                    }
                    else
                    {
                        MulitScore -= seconds;
                        return -seconds;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
