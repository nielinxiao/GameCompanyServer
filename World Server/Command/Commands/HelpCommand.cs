using System;
using System.Collections.Generic;
using System.Linq;
using Word_Sever;

namespace Word_Sever.Command
{
    /// <summary>
    /// 帮助命令
    /// </summary>
    public class HelpCommand : ICommand
    {
        public string Name => "/help";
        public string Description => "显示所有可用命令";
        public string Usage => "/help [命令名]";

        private LogTool _log;
        private Dictionary<string, ICommand> _commands;

        public HelpCommand(LogTool log, Dictionary<string, ICommand> commands)
        {
            _log = log;
            _commands = commands;
        }

        public void Execute(string[] args)
        {
            if (args.Length > 0)
            {
                // 显示特定命令的详细帮助
                string cmdName = args[0].ToLower();
                if (!cmdName.StartsWith("/"))
                    cmdName = "/" + cmdName;

                if (_commands.TryGetValue(cmdName, out ICommand cmd))
                {
                    var table = new ConsoleTableFormatter();
                    table.AddColumn("项目", "内容");
                    table.SetHeaderColor(ConsoleColor.Cyan);
                    table.AddRow("命令名称", cmd.Name);
                    table.AddRow("功能描述", cmd.Description);
                    table.AddRow("使用方法", cmd.Usage);
                    table.Render();
                }
                else
                {
                    var table = new ConsoleTableFormatter();
                    table.AddColumn("错误", "详情");
                    table.SetHeaderColor(ConsoleColor.Yellow);
                    table.AddRow("未知命令", cmdName);
                    table.Render();
                }
            }
            else
            {
                // 显示所有命令列表 - 表格形式
                var table = new ConsoleTableFormatter();
                table.AddColumn("命令", "描述", "用法");
                table.SetHeaderColor(ConsoleColor.Cyan);

                // 按名称排序
                foreach (var cmd in _commands.Values.OrderBy(c => c.Name))
                {
                    table.AddRow(cmd.Name, cmd.Description, cmd.Usage);
                }

                table.Render();
            }
        }
    }

    /// <summary>
    /// 列出在线玩家命令
    /// </summary>
    public class ListCommand : ICommand
    {
        public string Name => "/list";
        public string Description => "显示所有在线玩家";
        public string Usage => "/list";

        private LogTool _log;
        private Word_Sever.Servc.GameService _gameService;

        public ListCommand(LogTool log, Word_Sever.Servc.GameService gameService)
        {
            _log = log;
            _gameService = gameService;
        }

        public void Execute(string[] args)
        {
            // HTTP模式：从SessionManager获取在线玩家
            var sessionManager = _gameService.HttpServer?.SessionManager;
            if (sessionManager == null)
            {
                _log.LogError("[List] SessionManager未初始化");
                return;
            }

            int playerCount = sessionManager.GetOnlineCount();
            var onlinePlayers = sessionManager.GetOnlinePlayers();

            if (playerCount == 0)
            {
                var emptyTable = new ConsoleTableFormatter();
                emptyTable.AddColumn("在线玩家");
                emptyTable.SetHeaderColor(ConsoleColor.Yellow);
                emptyTable.AddRow("当前没有在线玩家");
                emptyTable.Render();
                return;
            }

            // 表格形式的在线玩家列表
            var table = new ConsoleTableFormatter();
            table.AddColumn("#", "玩家", "公司");
            table.SetHeaderColor(ConsoleColor.Cyan);

            int validPlayerIndex = 1;
            for (int i = 0; i < onlinePlayers.Length; i++)
            {
                var session = sessionManager.GetSession(onlinePlayers[i]);
                if (session != null && !string.IsNullOrEmpty(session.PlayerName))
                {
                    string company = string.IsNullOrEmpty(session.CompanyName) ? "(无公司)" : session.CompanyName;
                    table.AddRow(validPlayerIndex, session.PlayerName, company);
                    validPlayerIndex++;
                }
            }

            table.AddRow("合计", $"{validPlayerIndex - 1} 人在线", "");
            table.Render();
        }
    }
}
