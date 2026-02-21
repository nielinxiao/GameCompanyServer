using System;
using Word_Sever;
using Word_Sever.Storage;

namespace Word_Sever.Servc
{
    /// <summary>
    /// 游戏服务 - 管理GameServer、HttpServer和LocalDataStorage
    /// 实现IService接口,负责初始化、更新和清理
    /// </summary>
    public class GameService : IService
    {
        private LogTool _log;
        private GameServer _gameServer;
        private HttpServer _httpServer;
        private ChatSocketServer _chatSocketServer;
        private LocalDataStorage _localStorage;

        /// <summary>
        /// 公开访问GameServer实例 - 用于GM命令系统
        /// </summary>
        public GameServer GameServer => _gameServer;

        /// <summary>
        /// 公开访问HttpServer实例 - 用于GM命令系统获取在线玩家
        /// </summary>
        public HttpServer HttpServer => _httpServer;

        /// <summary>
        /// 公开访问ChatSocketServer实例 - 用于聊天推送
        /// </summary>
        public ChatSocketServer ChatSocketServer => _chatSocketServer;

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Init()
        {
            try
            {
                _log = OpenProgress.Instance.log;

                // ═══════════════════════════════════════════════════
                // 【配置保存模式】- 在这里修改
                // ═══════════════════════════════════════════════════
                // 模式1: 立即保存 - 每次客户端请求都立即保存（适合开发测试）
                _localStorage = new LocalDataStorage("Data", SaveMode.Instant);

                // 模式2: 定时保存 - 每24小时保存一次（适合生产环境）
                //_localStorage = new LocalDataStorage("Data", SaveMode.Interval, saveIntervalHours: 24);

                // 模式3: 定时保存 - 每1小时保存一次（适合频繁测试）
                // _localStorage = new LocalDataStorage("Data", SaveMode.Interval, saveIntervalHours: 1);
                // ═══════════════════════════════════════════════════

                // 创建游戏服务器（业务逻辑层）
                _gameServer = new GameServer(_localStorage);

                // 创建聊天Socket服务器（只用于聊天实时推送）
                _chatSocketServer = new ChatSocketServer();
                _chatSocketServer.Init("0.0.0.0", 45678);
                _chatSocketServer.Start();

                // 创建HTTP服务器（通信层）
                _httpServer = new HttpServer();
                _httpServer.Init(_gameServer, _chatSocketServer, "0.0.0.0", 45677);
                _httpServer.Start();

                // 表格形式显示初始化结果
                var initTable = new ConsoleTableFormatter();
                initTable.AddColumn("服务", "状态", "端口");
                initTable.SetHeaderColor(ConsoleColor.Green);
                initTable.AddRow("数据存储", "已启动", "-");
                initTable.AddRow("游戏服务器", "已启动", "-");
                initTable.AddRow("聊天Socket", "已启动", "45678");
                initTable.AddRow("HTTP服务器", "已启动", "45677");
                initTable.Render();
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameService] 初始化失败: {ex.Message}");
                _log.LogError($"[GameService] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 每帧更新 - HTTP模式下只需检查自动保存
        /// </summary>
        public void Tick()
        {
            try
            {
                // HTTP模式下不需要处理消息队列（异步处理）

                // 检查是否需要自动保存
                if (_localStorage != null)
                {
                    _localStorage.CheckAutoSave();
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameService] Tick异常: {ex.Message}");
                _log.LogError($"[GameService] 堆栈跟踪: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 卸载服务 - 关闭服务器和保存数据
        /// </summary>
        public void UnInit()
        {
            try
            {
                _log.LogYellow("[GameService] 正在卸载服务...");

                // 关闭HTTP服务器
                if (_httpServer != null)
                {
                    _httpServer.Stop();
                    _httpServer = null;
                    _log.LogGreen("[GameService] HttpServer 已关闭");
                }

                // 关闭聊天Socket服务器
                if (_chatSocketServer != null)
                {
                    _chatSocketServer.Stop();
                    _chatSocketServer = null;
                    _log.LogGreen("[GameService] ChatSocketServer 已关闭");
                }

                // 清理游戏服务器
                if (_gameServer != null)
                {
                    _gameServer = null;
                    _log.LogGreen("[GameService] GameServer 已清理");
                }

                // 强制保存数据
                if (_localStorage != null)
                {
                    _localStorage.ForceSave();
                    _localStorage = null;
                    _log.LogGreen("[GameService] LocalDataStorage 数据已保存");
                }

                _log.LogGreen("[GameService] 服务卸载完成");
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameService] UnInit异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前在线玩家数
        /// </summary>
        public int GetOnlinePlayerCount()
        {
            return _gameServer?.GetOnlinePlayerCount() ?? 0;
        }

        /// <summary>
        /// 获取数据存储统计信息
        /// </summary>
        public string GetStorageStats()
        {
            if (_localStorage == null)
                return "数据存储未初始化";

            int stringCount = _localStorage.GetStringKeyCount();
            int hashCount = _localStorage.GetHashKeyCount();

            return $"String键: {stringCount}, Hash表: {hashCount}";
        }
    }
}
