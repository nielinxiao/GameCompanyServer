using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Tools;
using Word_Sever;
using Word_Sever.Client;
using Word_Sever.Interface;

namespace Word_Server.Comp
{
    public class TransformComp : PlayerComp
    {
        public int waitTickCount = 0;
        public static int mustWaitCount = 2;
        public long utcnow;
        public long Lastutcnow;
        public Vector3 normal;
        public Vector3 predict => position + normal * (bufferComp.currentSpeedOffset+ bufferComp.CurrentMoveSpeed) * (DateTime.UtcNow.Ticks - utcnow)/10000000;

        public Vector3 position= Vector3.zero;
        public Vector3 Lastposition=Vector3.zero;
        public Vector3 rotation= Vector3.zero;
        public Vector3 Headrotation= Vector3.zero;
        public bool UpdatePosition=false;
        BufferComp bufferComp;
        public int TeamID;
        private float lastSpeed=0;
        private int tolerateCount=5;
        private int currentTolerateCount=0;
        public DateTime lastDateTimeTolerate;
        private float MinSecondsTolerate=3;
        private float MinDistance=3;
        public bool SetTransformConfig(Message.Vector3 position,Message.Vector3 dir, Message.Vector3 eluer,float speed, long dateTime)
        {
            Vector3 dirVec = dir;
            Vector3 forecastPosition = Vector3.zero;
            if (this.lastSpeed!=speed)
            {
                float maxSpeed=Math.Max(this.lastSpeed,speed);
                forecastPosition = this.position + dirVec * maxSpeed * DateTimeTick.TicksToSeconds(dateTime - utcnow);
                this.lastSpeed = speed;
            }
            else
            {
                forecastPosition = this.position + dirVec * speed *DateTimeTick.TicksToSeconds(dateTime - utcnow);
            }
            Vector3 positionVec= position;
            if(Vector3.Dot(forecastPosition-positionVec, forecastPosition-this.position)<0&& (forecastPosition- positionVec).Length>=MinDistance)
            {
                if((lastDateTimeTolerate-DateTime.Now).TotalSeconds<= MinSecondsTolerate)
                {
                    lastDateTimeTolerate = DateTime.Now;
                    currentTolerateCount++;
                }
                else
                {
                    currentTolerateCount = 1;
                }
                if(currentTolerateCount>= tolerateCount)
                {
                    OpenProgress.Instance.log.LogError($"警告! 玩家 [NickName:{splayer.nickName}] [UID:{splayer.uid}]确认速度作弊!踢出游戏");
                    splayer.Quit();
                }
                else
                {
                    OpenProgress.Instance.log.LogWarning($"警告! 玩家 [NickName:{splayer.nickName}] [UID:{splayer.uid}]疑似速度作弊 打回位置更新");
                }
                Lastposition = this.position;
                this.position = position;
                Lastutcnow = utcnow;
                utcnow = dateTime;
                return false;
            }
            else
            {
                this.lastSpeed = speed;

                Lastposition = this.position;
                this.position = position;
                normal = dir;
                rotation = eluer;
                Lastutcnow = utcnow;
                utcnow = dateTime;
                return true;
            }
        }
        public void UpdateConfig(bool updateState)
        {
            UpdatePosition = updateState;
        }
        public void SetHeadQuaternion(Message.Vector3 eluer)
        {
            Headrotation = eluer;
        }
        public override string ToString()
        {
            return $"predict:[{predict}] normal:[{normal}] position:[{position}] utcnow:[{utcnow}] lastutc:[{Lastutcnow}]";
        }

        public void Awake()
        {
            bufferComp=splayer.GetComp<BufferComp>();
        }
    }
}
