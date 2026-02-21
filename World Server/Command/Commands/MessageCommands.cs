using System;
using System.Linq;
using Word_Sever.Servc;
using Message;

namespace Word_Sever.Command
{
    internal static class GmMessageDefaults
    {
        public const string SenderId = "GM_SYSTEM";
        public const string SenderName = "GM系统";
        public const string CompanyName = "SYSTEM";
    }

    /// <summary>
    /// 发送邮件命令
    /// </summary>
    public class SendMailCommand : ICommand
    {
        public string Name => "/sendmail";
        public string Description => "给玩家发送邮件";
        public string Usage => "/sendmail <玩家ID> <标题> <内容>";

        private LogTool _log;
        private GameService _gameService;

        public SendMailCommand(LogTool log, GameService gameService)
        {
            _log = log;
            _gameService = gameService;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                _log.LogYellow($"[SendMail] 参数不足。用法: {Usage}");
                return;
            }

            string playerID = args[0];
            string title = args[1];
            string content = string.Join(" ", args.Skip(2));  // 剩余所有参数作为内容

            // 查找玩家会话
            if (!_gameService.GameServer.ClientSessions.TryGetValue(playerID, out var client))
            {
                _log.LogYellow($"[SendMail] 玩家不在线: {playerID}");
                return;
            }

            // 发送邮件消息
            ServerMessage serverMessage = new ServerMessage
            {
                Id = playerID,
                clientName = GmMessageDefaults.SenderName,
                companyName = GmMessageDefaults.CompanyName,
                Message = content,
                Email = new EmailMessage
                {
                    Title = title,
                    Description = content,
                    Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    objectIDs = new int[0],
                    Numbers = new int[0]
                }
            };

            Pkg pkg = new Pkg
            {
                Head = new Head { ServerCmd = ServerCMD.GMAwardEmail },
                Body = new Body { serverMessage = serverMessage }
            };

            client.Send(pkg);
            _log.LogGreen($"[SendMail] 成功发送邮件给玩家 {playerID}");
            _log.LogGreen($"  标题: {title}");
            _log.LogGreen($"  内容: {content}");
        }
    }

    /// <summary>
    /// 广播消息命令
    /// </summary>
    public class BroadcastCommand : ICommand
    {
        public string Name => "/broadcast";
        public string Description => "向所有在线玩家广播消息";
        public string Usage => "/broadcast <消息内容>";

        private LogTool _log;
        private GameService _gameService;

        public BroadcastCommand(LogTool log, GameService gameService)
        {
            _log = log;
            _gameService = gameService;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 1)
            {
                _log.LogYellow($"[Broadcast] 参数不足。用法: {Usage}");
                return;
            }

            string message = string.Join(" ", args);  // 所有参数组合为消息

            // 使用ChatSocketServer进行实时广播
            var chatSocketServer = _gameService.ChatSocketServer;
            if (chatSocketServer == null)
            {
                _log.LogError("[Broadcast] ChatSocketServer未初始化");
                return;
            }

            // 获取在线玩家数量
            var sessionManager = _gameService.HttpServer?.SessionManager;
            int playerCount = sessionManager?.GetOnlineCount() ?? 0;

            if (playerCount == 0)
            {
                var emptyTable = new ConsoleTableFormatter();
                emptyTable.AddColumn("广播");
                emptyTable.SetHeaderColor(ConsoleColor.Yellow);
                emptyTable.AddRow("当前没有在线玩家");
                emptyTable.Render();
                return;
            }

            // 通过ChatSocket实时广播GM消息（BroadcastMessage内部有表格日志）
            chatSocketServer.BroadcastMessage(
                GmMessageDefaults.SenderName,      // GM系统
                GmMessageDefaults.CompanyName,     // SYSTEM
                message
            );
        }
    }
}
