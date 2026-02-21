using System;
using System.Globalization;
using System.Linq;
using Word_Sever.Servc;
using Message;

namespace Word_Sever.Command
{
    /// <summary>
    /// 添加金币命令
    /// </summary>
    public class AddMoneyCommand : ICommand
    {
        public string Name => "/addmoney";
        public string Description => "给玩家添加金币";
        public string Usage => "/addmoney <玩家ID> <金额>";

        private LogTool _log;
        private GameService _gameService;

        public AddMoneyCommand(LogTool log, GameService gameService)
        {
            _log = log;
            _gameService = gameService;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 2)
            {
                _log.LogYellow($"[AddMoney] 参数不足。用法: {Usage}");
                return;
            }

            string playerID = args[0];
            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
            {
                _log.LogYellow($"[AddMoney] 金额格式错误: {args[1]}");
                return;
            }

            if (amount <= 0)
            {
                _log.LogYellow($"[AddMoney] 金额必须大于0");
                return;
            }

            // 查找玩家会话
            if (!_gameService.GameServer.ClientSessions.TryGetValue(playerID, out var client))
            {
                _log.LogYellow($"[AddMoney] 玩家不在线: {playerID}");
                return;
            }

            // 发送添加金币消息
            ServerMessage serverMessage = new ServerMessage
            {
                Id = playerID,
                clientName = GmMessageDefaults.SenderName,
                companyName = GmMessageDefaults.CompanyName,
                Message = $"系统管理员给你发放了 {amount} 金币",
                GMMoney = amount
            };

            Pkg pkg = new Pkg
            {
                Head = new Head { ServerCmd = ServerCMD.GMAddMoney },
                Body = new Body { serverMessage = serverMessage }
            };

            client.Send(pkg);
            _log.LogGreen($"[AddMoney] 成功给玩家 {playerID} 添加 {amount} 金币");
        }
    }

    /// <summary>
    /// 扣除金币命令
    /// </summary>
    public class RemoveMoneyCommand : ICommand
    {
        public string Name => "/removemoney";
        public string Description => "扣除玩家金币";
        public string Usage => "/removemoney <玩家ID> <金额>";

        private LogTool _log;
        private GameService _gameService;

        public RemoveMoneyCommand(LogTool log, GameService gameService)
        {
            _log = log;
            _gameService = gameService;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 2)
            {
                _log.LogYellow($"[RemoveMoney] 参数不足。用法: {Usage}");
                return;
            }

            string playerID = args[0];
            if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
            {
                _log.LogYellow($"[RemoveMoney] 金额格式错误: {args[1]}");
                return;
            }

            if (amount <= 0)
            {
                _log.LogYellow($"[RemoveMoney] 金额必须大于0");
                return;
            }

            // 查找玩家会话
            if (!_gameService.GameServer.ClientSessions.TryGetValue(playerID, out var client))
            {
                _log.LogYellow($"[RemoveMoney] 玩家不在线: {playerID}");
                return;
            }

            // 发送扣除金币消息（使用负数）
            ServerMessage serverMessage = new ServerMessage
            {
                Id = playerID,
                clientName = GmMessageDefaults.SenderName,
                companyName = GmMessageDefaults.CompanyName,
                Message = $"系统管理员扣除了你 {amount} 金币",
                GMMoney = -amount  // 负数表示扣除
            };

            Pkg pkg = new Pkg
            {
                Head = new Head { ServerCmd = ServerCMD.GMAddMoney },
                Body = new Body { serverMessage = serverMessage }
            };

            client.Send(pkg);
            _log.LogGreen($"[RemoveMoney] 成功扣除玩家 {playerID} {amount} 金币");
        }
    }
}
