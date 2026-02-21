using Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Skill;
using Word_Server.Tank;
using Word_Sever;
using Word_Sever.Client;
using Word_Sever.Interface;
using Word_Sever.World;

namespace Word_Server.Comp
{
    public class BufferComp : PlayerComp,IUpdate
    {
        protected ConcurrentDictionary<int,BufferBase> buffers = new ConcurrentDictionary<int,BufferBase>();
        public float fixHealthy;
        public float currentHealthy;
        public float fixMoveSpeed;
        public float CurrentMoveSpeed;
        public float currentSpeedOffset;
        public bool isMove=false;
        public void SetSpeedOffset(float speed)
        {
            currentSpeedOffset += speed;
        }
        public void SetTankSpeed(float speed)
        {
            buffers.Clear();
            fixMoveSpeed = speed;
            CurrentMoveSpeed=fixMoveSpeed;
            currentSpeedOffset = 0;
        }
        public void SetCurrentSpeed(float speed)
        {
            CurrentMoveSpeed = speed;
        }
        public void SetHealthy(float healthy)
        {
            fixHealthy = healthy;
            currentHealthy = healthy;
        }
        public Dictionary<int, DateTime> bufferIds=new Dictionary<int, DateTime>();
        public void AddBuffer(int bufferId,long timeInvoke)
        {
            SkillConfig.Config config= SkillConfig.GetConfigById(bufferId);
            if (bufferIds.TryGetValue(bufferId, out DateTime dateTime))
            {
                if ((DateTime.UtcNow - dateTime).TotalSeconds < config.CDTime)
                {
                    return;
                }
                else
                {
                    bufferIds[bufferId] = DateTime.UtcNow;
                }
            }
            else
            {
                bufferIds.Add(bufferId, DateTime.UtcNow);
            }
            WorldPCK world=new WorldPCK();
            world.Body = new WorldBody();
            world.Head = new Message.Head();
            world.Head.Cmd=Cmd.WorldAddBuffer;
            world.Body.skillId = bufferId;
            world.Body.DateTime = timeInvoke;
            world.Body.Uid = splayer.uid;
            ((BattleWorld)splayer.sworld).BorastCast(world,true);
            BufferBase bufferBase= SkillConfig.GetPrefabById(bufferId, this);
            lock (buffers)
                buffers.TryAdd(config.skillId, bufferBase);
            OpenProgress.Instance.log.LogGreen($"[Uid:{splayer.uid}] invoke [bufferid:{bufferId}][bufferName:{config.skillName}]");
            bufferBase.onStartBuffer();
        }
        public void RemoveBuffer(int bufferId)
        {
            if (buffers.TryRemove(bufferId,out BufferBase value)) 
            {
                value.onEndBuffer();
                OpenProgress.Instance.log.LogYellow($"[Uid:{splayer.uid}] remove [bufferid:{bufferId}][bufferName:{SkillConfig.GetConfigById(bufferId).skillName}]");
            }
        }
        Queue<int>removeBufferID = new Queue<int>();
        public void Update()
        {
            lock (buffers)
                foreach (var buffer in buffers)
                {
                    buffer.Value.Tick();
                    if (buffer.Value.duraTime != -1)
                    {
                        if ((DateTime.UtcNow - buffer.Value.duratimer).TotalSeconds >= buffer.Value.duraTime)
                        {
                            removeBufferID.Enqueue(buffer.Key);
                        }
                    }
                }
            while (removeBufferID.Count > 0)
            {
                RemoveBuffer(removeBufferID.Dequeue());
            }
        }
    }
}
