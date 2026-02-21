using System;
using System.Collections.Generic;
using System.Linq;
using Word_Sever.Servc;
using Word_Sever;
using Message;

namespace Word_Sever.Command
{
    /// <summary>
    /// 服务端命令系统 - 处理控制台输入的GM指令
    /// </summary>
    public class CommandSystem
    {
        private Dictionary<string, ICommand> _commands;
        private LogTool _log;
        private GameService _gameService;

        public CommandSystem(LogTool log, GameService gameService)
        {
            _log = log;
            _gameService = gameService;
            _commands = new Dictionary<string, ICommand>();

            RegisterCommands();
        }

        /// <summary>
        /// 注册所有可用命令
        /// </summary>
        private void RegisterCommands()
        {
            // 金币命令
            RegisterCommand(new AddMoneyCommand(_log, _gameService));
            RegisterCommand(new RemoveMoneyCommand(_log, _gameService));

            // 物品命令
            RegisterCommand(new GiveItemCommand(_log, _gameService));
            RegisterCommand(new RemoveItemCommand(_log, _gameService));

            // 消息命令
            RegisterCommand(new SendMailCommand(_log, _gameService));
            RegisterCommand(new BroadcastCommand(_log, _gameService));

            // 帮助命令
            RegisterCommand(new HelpCommand(_log, _commands));
            RegisterCommand(new ListCommand(_log, _gameService));

            // 表格形式显示命令系统信息
            var cmdTable = new ConsoleTableFormatter();
            cmdTable.AddColumn("命令系统", "值");
            cmdTable.SetHeaderColor(ConsoleColor.Cyan);
            cmdTable.AddRow("已注册命令", _commands.Count);
            cmdTable.AddRow("查看帮助", "/help");
            cmdTable.AddRow("查看在线", "/list");
            cmdTable.Render();
        }

        /// <summary>
        /// 注册单个命令
        /// </summary>
        private void RegisterCommand(ICommand command)
        {
            _commands[command.Name] = command;
        }

        /// <summary>
        /// 获取所有已注册命令的名称列表
        /// </summary>
        public List<string> GetAllCommands()
        {
            return _commands.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void ExecuteCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return;

            // 去除首尾空格
            input = input.Trim();

            // 如果不是以 / 开头，添加 /
            if (!input.StartsWith("/"))
            {
                input = "/" + input;
            }

            // 解析命令和参数
            string[] parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            string commandName = parts[0].ToLower(); // 命令名转小写
            string[] args = parts.Skip(1).ToArray();

            // 查找并执行命令
            if (_commands.TryGetValue(commandName, out ICommand command))
            {
                try
                {
                    command.Execute(args);
                }
                catch (Exception ex)
                {
                    _log.LogError($"[CommandSystem] 命令执行失败: {ex.Message}");
                }
            }
            else
            {
                _log.LogYellow($"[CommandSystem] 未知命令: {commandName}，输入 /help 查看帮助");
            }
        }
    }
}
