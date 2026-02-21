using Word_Sever.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Word_Sever.Servc
{
    public class ConfigSvc : IService
    {
        OpenProgress openProgress;
        public Configs.Server serverConfig;
        public Configs.Client clientConfig;
        LogTool log;
        public ConfigSvc()
        {
            openProgress= OpenProgress.Instance;
            serverConfig = new Configs.Server();
            clientConfig = new Configs.Client();
            log =openProgress.log;


            Configs.serverConfigPath = $"{System.IO.Directory.GetCurrentDirectory()}\\ServerConfig.xml";
            Configs.clientConfigPath = $"{System.IO.Directory.GetCurrentDirectory()}\\ClientConfig.xml";
            string[] Worldparams = ReloadXml(Configs.serverConfigPath, "ServerConfig",
                ("containerCount", serverConfig.containerCount.ToString()),
                    ("maxCount", serverConfig.maxCount.ToString()),
                    ("signPort", serverConfig.signPort.ToString()),
                    ("mainPort", serverConfig.mainPort.ToString()),
                    ("serverip", serverConfig.serverip.ToString()));
            if (Worldparams != null)
            {
                serverConfig.containerCount = Convert.ToUInt32(Worldparams[0]);
                serverConfig.maxCount = Convert.ToUInt32(Worldparams[1]);
                serverConfig.signPort = Convert.ToInt32(Worldparams[2]);
                serverConfig.mainPort = Convert.ToInt32(Worldparams[3]);
                serverConfig.serverip = Worldparams[4];
                log.LogYellow($"Worldparams reloading successful");
            }
            string[] Clientparams = ReloadXml(Configs.clientConfigPath, "ClientConfig",
                 ("signPort", clientConfig.signPort.ToString())
                );
            if (Clientparams != null)
            {
                clientConfig.signPort = Convert.ToInt32(Clientparams[0]);
                log.LogYellow($"Clientparams reloading successful");
            }
        }
        public void Init()
        {
            
        }
        string[] ReloadXml(string path,string title, params (string, string)[] values)
        {
            string titlename = "";
            List<XmlNode> nodes;
            if (openProgress.resSvc.ReadXml(path, out titlename, out  nodes))
            {
                try
                {
                    string[] xml = new string[nodes.Count];
                    for(int count=0;count<nodes.Count;count++)
                    {
                        xml[count] = nodes[count].InnerText;
                    }
                    return xml;
                }
                catch
                {
                    log.LogError($"{title} Error Reloading Now");
                    openProgress.resSvc.WriteXml(path, title,values);
                    return null;
                }
            }
            else
            {
                log.LogYellow($"Reseting {title}...");
                openProgress.resSvc.WriteXml(path, title,values);
                return null;
            }
        }
        public void Tick()
        {
        }

        public void UnInit()
        {
        }
    }
}
