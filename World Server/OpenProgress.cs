using Word_Sever.Servc;
using Word_Sever.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Message;

namespace Word_Sever
{
    public class OpenProgress
    {
        public static OpenProgress Instance;
        public List<IService> Services = new List<IService>();
        public LogTool log;
        public TimerSvc timerSvc;
        public MainWorldSvc mainWorldSvc;
        public AccSvc accSvc;
        public GameService gameService;
        public PEConfig pEConfig;
        public MysqlTcp mysqlTcp;
        public CommandSystem commandSystem;
        public class PEConfig
        {
            //登录服IP
            public string signIp;
            //登录服端口
            public int signPort;
            //战斗服IP
            public string battleIp;
            public string battleBindIp;
            // 战斗服基础的端口号 后续在此基础上添加
            public int battlePortBase;
            //几个线程
            public int worldContainerCount;
            //一个线程几个世界
            public int ContainerCreatWorldCount;
            //玩家battle udp监听端口
            public int clientBattlePort;
            public int VoicePortBase;
        }
        public void Start()
        {
            pEConfig = new PEConfig();
            pEConfig.signIp = "0.0.0.0";
            pEConfig.signPort = 5757;
            pEConfig.battleIp = "49.233.248.132";//49.233.248.132
            pEConfig.battleBindIp = "0.0.0.0";
            pEConfig.battlePortBase = 5758;
            pEConfig.clientBattlePort = 5756;
            pEConfig.worldContainerCount = 4;
            pEConfig.ContainerCreatWorldCount = 4;
            pEConfig.VoicePortBase = pEConfig.battlePortBase+pEConfig.worldContainerCount* pEConfig.ContainerCreatWorldCount+1;
            log = new LogTool(LogAction,ConsoleColor.White);
            //Servic
            timerSvc=new TimerSvc();
            Services.Add(timerSvc);
            SignSvc signSvc=new SignSvc();
            Services.Add(signSvc);
            mainWorldSvc=new MainWorldSvc();
            Services.Add(mainWorldSvc);
            accSvc = new AccSvc();
            Services.Add(accSvc);

            // GameCompany服务 - 监听端口45677
            gameService = new GameService();
            Services.Add(gameService);

            mysqlTcp=new MysqlTcp();
            mysqlTcp.InitIOCPServer((str) => log.LogGreen(str), 10, true, "0.0.0.0", 3366);

            //Modle
            Init();

            // 初始化命令系统
            commandSystem = new CommandSystem(log, gameService);
        }
        private void Init()
        {
            foreach(var shu in Services)
            {
                shu.Init();
            }
        }
        public void Update()
        {
            foreach(var shu in Services)
            {
                shu.Tick();
            }
        }
        public void Uninit()
        {
            foreach(var shu in Services)
            {
                shu.UnInit();
            }
            mysqlTcp.CloseServer();
            Environment.Exit(0);
        }
        private void LogAction(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}