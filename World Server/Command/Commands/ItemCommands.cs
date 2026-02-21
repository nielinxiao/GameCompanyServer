using System;
using System.Linq;
using Word_Sever.Servc;
using Message;

namespace Word_Sever.Command
{
    /// <summary>
    /// 给予物品命令
    /// </summary>
    public class GiveItemCommand : ICommand
    {
        public string Name => "/giveitem";
        public string Description => "给玩家发放物品";
        public string Usage => "/giveitem <玩家ID> <物品ID> <数量>";

        private LogTool _log;
        private GameService _gameService;

        public GiveItemCommand(LogTool log, GameService gameService)
        {
            _log = log;
            _gameService = gameService;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                _log.LogYellow($"[GiveItem] 参数不足。用法: {Usage}");
                return;
            }

            string playerID = args[0];
            if (!int.TryParse(args[1], out int itemID))
            {
                _log.LogYellow($"[GiveItem] 物品ID格式错误: {args[1]}");
                return;
            }

            if (!int.TryParse(args[2], out int count))
            {
                _log.LogYellow($"[GiveItem] 数量格式错误: {args[2]}");
                return;
            }

            if (count <= 0)
            {
                _log.LogYellow($"[GiveItem] 数量必须大于0");
                return;
            }

            // 查找玩家会话
            if (!_gameService.GameServer.ClientSessions.TryGetValue(playerID, out var client))
            {
                _log.LogYellow($"[GiveItem] 玩家不在线: {playerID}");
                return;
            }

            // 发送物品通过邮件系统
            ServerMessage serverMessage = new ServerMessage
            {
                Id = playerID,
                clientName = GmMessageDefaults.SenderName,
                companyName = GmMessageDefaults.CompanyName,
                Message = $"系统管理员给你发放了物品ID={itemID}，数量={count}",
                Email = new EmailMessage
                {
                    Title = "系统邮件",
                    Description = $"系统管理员给你发放了物品ID={itemID}，数量={count}",
                    Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    objectIDs = new int[] { itemID },
                    Numbers = new int[] { count }
                }
            };

            Pkg pkg = new Pkg
            {
                Head = new Head { ServerCmd = ServerCMD.GMAwardEmail },
                Body = new Body { serverMessage = serverMessage }
            };

            client.Send(pkg);
            _log.LogGreen($"[GiveItem] 成功给玩家 {playerID} 发放物品 {itemID} x{count}");
        }
    }

    /// <summary>
    /// 删除物品命令
    /// </summary>
    public class RemoveItemCommand : ICommand
    {
        public string Name => "/removeitem";
        public string Description => "删除玩家物品";
        public string Usage => "/removeitem <玩家ID> <物品ID> <数量>";

        private LogTool _log;
        private GameService _gameService;

        public RemoveItemCommand(LogTool log, GameService gameService)
        {
            _log = log;
            _gameService = gameService;
        }

        public void Execute(string[] args)
        {
            if (args.Length < 3)
            {
                _log.LogYellow($"[RemoveItem] 参数不足。用法: {Usage}");
                return;
            }

            string playerID = args[0];
            if (!int.TryParse(args[1], out int itemID))
            {
                _log.LogYellow($"[RemoveItem] 物品ID格式错误: {args[1]}");
                return;
            }

            if (!int.TryParse(args[2], out int count))
            {
                _log.LogYellow($"[RemoveItem] 数量格式错误: {args[2]}");
                return;
            }

            if (count <= 0)
            {
                _log.LogYellow($"[RemoveItem] 数量必须大于0");
                return;
            }

            // 查找玩家会话
            if (!_gameService.GameServer.ClientSessions.TryGetValue(playerID, out var client))
            {
                _log.LogYellow($"[RemoveItem] 玩家不在线: {playerID}");
                return;
            }

            // 发送删除物品消息（使用负数表示删除）
            ServerMessage serverMessage = new ServerMessage
            {
                Id = playerID,
                clientName = GmMessageDefaults.SenderName,
                companyName = GmMessageDefaults.CompanyName,
                Message = $"系统管理员删除了你的物品ID={itemID}，数量={count}",
                Email = new EmailMessage
                {
                    Title = "系统通知",
                    Description = $"系统管理员删除了你的物品ID={itemID}，数量={count}",
                    Datetime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    objectIDs = new int[] { itemID },
                    Numbers = new int[] { -count }  // 负数表示删除
                }
            };

            Pkg pkg = new Pkg
            {
                Head = new Head { ServerCmd = ServerCMD.GMAwardEmail },
                Body = new Body { serverMessage = serverMessage }
            };

            client.Send(pkg);
            _log.LogGreen($"[RemoveItem] 成功删除玩家 {playerID} 的物品 {itemID} x{count}");
        }
    }
}
