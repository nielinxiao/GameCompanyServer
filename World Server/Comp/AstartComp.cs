using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Server.Tools;
using Word_Sever.Client;
using Word_Sever.Interface;
using Word_Sever.Servc;
using Word_Sever.World;

namespace Word_Server.Comp
{
    public class AstartComp:WorldComp,IAWake,IUpdate
    {
        public void Awake()
        {
            string path=Directory.GetCurrentDirectory();
            LoadMapJson(path+"\\Baker.txt");
        }
        public void LoadMapJson(string path)
        {
            nodes.Clear();
            using (StreamReader streamReader = File.OpenText(path))
            {
                JsonSystem JsonSystem = JsonConvert.DeserializeObject<JsonSystem>(streamReader.ReadToEnd());
                sworld.log.LogYellow($"反序列化成功 读取到Node:[{JsonSystem.paths.Count}]");
                foreach(var node in JsonSystem.paths)
                {
                    nodes.Add(new PathNode() {
                        bound = node.bound, 
                        ColliderID = node.ColliderID,
                        Distance = float.PositiveInfinity,
                        IsBlock = node.IsBlock,
                        parentNode = null });
                }
                for (int i = 0; i < nodes.Count; i++) 
                {
                    foreach(int id in JsonSystem.paths[i].NegiborId)
                    {
                        nodes[i].Negibor.Add(nodes[id]);
                    }
                }
            }
        }
        public List<PathNode>nodes=new List<PathNode>();
        public bool isRuning = false;
        public List<TransformComp> Enity=new List<TransformComp>();
        public const int StatBoundsID = 0;
        public void RunAstart()
        {
            sworld.log.LogWhite("A* 启动");
            isRuning = true;
            Finding = false;
            moveTime = DateTime.UtcNow;
            near = false;
            Task.Run(MATH);
        }
        public void StopRuning()
        {
            isRuning= false;
        }
        public DateTime dateTime;
        public float NearTime=0.5f;
        public float MoveSpeed=3;
        public static float MinAim = 1f; 
        public Vector3 dir;
        DateTime moveTime;
        float SendTimer=0.2f;
        Vector3 distance;
        public void SendAsync(Vector3 vector3,bool NearBounds)
        {
            Message.WorldPCK worldPCK = new Message.WorldPCK();
            worldPCK.Body = new Message.WorldBody();
            worldPCK.Head = new Message.Head();
            worldPCK.Head.Cmd=Message.Cmd.WorldRobotMove;
            worldPCK.Body.Position =new Message.Vector3()
            {
                X = vector3.x,
                Y = vector3.y,
                Z = vector3.z
            };
            worldPCK.Body.NearBounds = NearBounds;
            worldPCK.Body.Uid = nearComp.splayer.uid;
            ((BattleWorld)sworld).BorastCast(worldPCK);
        }
        public void SendFire(bool fire)
        {
            Message.WorldPCK worldPCK = new Message.WorldPCK();
            worldPCK.Head=new Message.Head();
            worldPCK.Body=new Message.WorldBody();
            if(fire)
            {
                worldPCK.Head.Cmd= Message.Cmd.WorldRobotFire;
            }
            else
            {
                worldPCK.Head.Cmd = Message.Cmd.WorldRobotUnFire;
            }
            ((BattleWorld)sworld).BorastCast(worldPCK);
        }
        public bool RunOut=true;
        TransformComp nearComp = null;
        bool near = false;
        public void Update()
        {
            if(isRuning)
            {
                if ((DateTime.UtcNow - dateTime).TotalSeconds > NearTime)
                {
                    dateTime = DateTime.UtcNow;
                    if (nearComp == null)
                    {
                        nearComp = Enity[0];
                    }
                    float length = (nearComp.position - moveNode.bound.center).Length;
                    for (int i = 0; i < Enity.Count; i++)
                    {
                        float currentLenght = (Enity[i].position - moveNode.bound.center).Length;
                        if (currentLenght < length)
                        {
                            nearComp = Enity[i];
                            length = currentLenght;
                        }
                    }
                    if (moveNode == null && nearComp.position != Vector3.zero)
                    {
                        foreach(var shu in nodes)
                        {
                            if(shu.bound.Contains(nearComp.position))
                            {
                                moveNode =shu;
                                break;
                            }
                        }
                    }
                }
                if (moveNode != null&&!Finding)
                {
                    if ((DateTime.UtcNow - moveTime).TotalSeconds >= SendTimer)
                    {
                        moveTime = DateTime.UtcNow;
                        if (endNode.Count == 0)
                        {
                            if(moveNode.bound.Contains(nearComp.position))
                            {
                                if(!near)
                                {
                                    near = true;
                                    distance = nearComp.position;
                                    SendTimer = Math.Max((moveNode.bound.center - distance).Length / MoveSpeed, 0.2f);
                                    SendAsync(distance, true);
                                    SendFire(true);
                                }
                            }
                            else 
                            {
                                SendFire(false);
                                Finding = true;
                                near = false;
                                Task.Run(MATH);
                            }
                        }
                        else
                        {
                            PathNode temp = endNode.Pop();
                            moveNode = temp;
                            distance = moveNode.bound.center;
                            SendTimer =Math.Max((moveNode.bound.center - temp.bound.center).Length / MoveSpeed,0.2f);
                            near = false;
                            SendAsync(distance, false);
                        }
                    }
                }
            }
        }
        public PathNode moveNode;
        private async Task MATH()
        {
            waiterRun.Clear();
            foreach(var shu in  nodes)
            {
                shu.parentNode = null;
                shu.Distance = float.PositiveInfinity;
                shu.OverWrite = false;
            }
            AimVec = nearComp.position;
            RunOut = false;
            endNode.Clear();
            CurrentNode=moveNode;
            CurrentNode.Distance = 0;
            count = 0;
            UpdateNode() ;
        }
        int count = 0;
        Vector3 AimVec;
        public PathNode CurrentNode;
        bool Finding = false;
        public float limitLength = 1;
        public void Find()
        {
            if(count <= 1)
            {
                SendFire(true);
            }
            else
            {
                SendFire(false);
            }
            Finding = false;
            sworld.log.LogWhite($"追踪到了 遍历节点数:{count}");
            if(CurrentNode.parentNode != null)
            {
                while (CurrentNode.parentNode != null)
                {
                    endNode.Push(CurrentNode.parentNode);
                    CurrentNode = CurrentNode.parentNode;
                }
            }
            else
            {
                endNode.Push(CurrentNode);
            }
            waiterRun.Clear();
        }
        private void UpdateNode()
        {
            if (count>=1600)
            {
                sworld.log.LogWhite($"没找到 遍历节点数:{count}");
                Finding = false;
                return;
            }
            bool update=false;
            foreach (var shu in CurrentNode.Negibor)
            {
                if(shu.OverWrite)continue;
                if (shu.bound.Contains(AimVec))
                {
                    shu.parentNode = CurrentNode;
                    CurrentNode = shu;
                    count++;
                    Find();
                    return;
                }
                else if (shu.IsBlock)
                {
                    continue;
                }
                else if (shu.Distance > CurrentNode.Distance + (CurrentNode.bound.center- shu.bound.center).Length)
                {
                    shu.Distance = CurrentNode.Distance + (CurrentNode.bound.center - shu.bound.center).Length;
                    shu.parentNode = CurrentNode;
                    waiterRun.Add(shu);
                    update = true;
                }
            }
            if (waiterRun.Count == 0)
            {
                Finding = false;
                sworld.log.LogWhite($"没找到 遍历节点数:{count}");
            }
            else
            {
                CurrentNode.OverWrite = true;
                if (update)
                {
                    CurrentNode = Sort_First();
                }
                else
                {
                    CurrentNode = waiterRun[0];
                }
                count++;
                waiterRun.Remove(CurrentNode);
                UpdateNode();
            }
        }
        Stack<PathNode>endNode= new Stack<PathNode>();
        public List<PathNode>waiterRun=new List<PathNode>();
        private PathNode Sort_First()
        {
            int n = waiterRun.Count;
            if (n > 0)
            {
                PathNode endnode= waiterRun[0];
                float minDistance = waiterRun[0].Distance + (AimVec - waiterRun[0].bound.center).Length;
                float tempDistance = 0;
                for (int i = 1; i < n - 1; i++)
                {
                    tempDistance = (waiterRun[0].Distance + (AimVec - waiterRun[0].bound.center).Length);
                    if (tempDistance < minDistance)
                    {
                        endnode = waiterRun[i];
                        minDistance = tempDistance;
                    }
                }
                return endnode;
            }
            else
            {
                sworld.log.LogError("数组为空");
                return null;
            }
        }
        private void BubbleSort()
        {
            int n = waiterRun.Count;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - i - 1; j++)
                {
                    Vector3 waitj=waiterRun[j].bound.center;
                    Vector3 waitj1= waiterRun[j + 1].bound.center;
                    if (waiterRun[j].Distance + (waitj - AimVec).Length > waiterRun[j + 1].Distance + (waitj1 - AimVec).Length)
                    {
                        // 交换 arr[j] 和 arr[j + 1]
                        PathNode temp = waiterRun[j];
                        waiterRun[j] = waiterRun[j + 1];
                        waiterRun[j + 1] = temp;
                    }
                }
            }
        }
    }
}
