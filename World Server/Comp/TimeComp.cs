using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Word_Sever.Interface;
using Word_Sever.Servc;
public class TimeComp : WorldComp, IUpdate
{
    private ConcurrentDictionary<int, CallBack> callBack = new ConcurrentDictionary<int, CallBack>();
    struct CallBack
    {
        public DateTime time;
        public Action action;
        public float waitSecond;
    }
    int count=-1;
    public int AddTimeSpawn(Action action,float waitSecond)
    {
        if(callBack.TryAdd(count+1,new CallBack() { time=DateTime.UtcNow,action = action ,waitSecond= waitSecond }))
        {
            count++;
            return count;
        }
        else
        {
            return -1;
        }
    }
    public void RemoveTimeSpawn(int id)
    {
        callBack.TryRemove(id, out CallBack call);
    }
    Queue<int>removeID= new Queue<int>();
    public void Update()
    {
        foreach(var call in callBack)
        {
            if((DateTime.UtcNow-call.Value.time).TotalSeconds>call.Value.waitSecond)
            {
                call.Value.action.Invoke();
                removeID.Enqueue(call.Key);
            }
        }
        while(removeID.Count>0)
        {
            callBack.TryRemove(removeID.Dequeue(),out CallBack call);
        }
    }
}
