using Word_Sever;
using System;
using System.Collections.Concurrent;
public class TimerSvc:IService
{
    private LogTool log;
    public TimerSvc()
    {
        log = OpenProgress.Instance.log;
    }
    public void Init() 
    {
        log.LogGreen("TimerSvc Loading Successfull...");
        services = new ConcurrentDictionary<int, Service>();
    }
    public class Service
    {
        public Action tickAction;//执行
        public float delay;//延迟
        public double interval;//间隔
        public uint Count;
        public Action callBack;//回调
        public DateTime utcNow;
    }
    private ConcurrentDictionary<int,Service> services;
    public int AddTask( Action tickAction,float delay,double interval, uint Count, Action callBack, DateTime utcNow)
    {
        Service service=new Service() { callBack=callBack, interval=interval, Count=Count, utcNow=utcNow,tickAction=tickAction,delay=delay};
        int count = 0;
        while(true)
        {
            if (!services.ContainsKey(count))
                break;
            count++;
        }
        services.TryAdd(count, service);
        log.LogGreen($"Successfull Add id:{count} Task.");
        return count;
    }
    public bool RemoveTask(int id)
    {
        if(services.ContainsKey(id))
        {
            Service secv;
            services.TryRemove(id,out secv);
            log.LogGreen($"Successfull Remove id:{id} Task.");
            return true;
        }
        else
        {
            log.LogError($"Have No id:{id} in TimerSvc.");
            return false;
        }
    }
    public void Tick()
    { 
        foreach(var shu in services)
        {
            Service service = shu.Value;
            if((DateTime.UtcNow-service.utcNow).TotalSeconds >=service.interval+service.delay)
            {
                service.tickAction.Invoke();
                if(service.Count!=0)
                {
                    --service.Count;
                    if(service.Count==0)
                    {
                        shu.Value.callBack.Invoke();
                        RemoveTask(shu.Key);
                        continue;
                    }
                }
                service.utcNow=service.utcNow.AddSeconds(service.interval);
            }
        }
    }
    public void UnInit() 
    {
        services.Clear();
        services = null;
        log.LogGreen("TimeSvc Remove Successful");
    }
}
