using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Word_Sever.Servc
{
    /// <summary>
    /// 聊天专用 Socket 服务器 - 只用于实时推送聊天消息
    /// </summary>
    public class ChatSocketServer
    {
        private Socket _serverSocket;
        private ConcurrentDictionary<string, Socket> _clients; // playerId -> Socket
        private bool _isRunning;
        private string _ip;
        private int _port;

        public ChatSocketServer()
        {
            _clients = new ConcurrentDictionary<string, Socket>();
        }

        public void Init(string ip, int port)
        {
            _ip = ip;
            _port = port;
        }

        public void Start()
        {
            try
            {
                _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _serverSocket.Bind(new IPEndPoint(IPAddress.Parse(_ip), _port));
                _serverSocket.Listen(100);
                _isRunning = true;

                // 表格形式的启动信息
                var startTable = new ConsoleTableFormatter();
                startTable.AddColumn("配置", "值");
                startTable.SetHeaderColor(ConsoleColor.Cyan);
                startTable.AddRow("聊天Socket", "启动成功");
                startTable.AddRow("监听地址", $"{_ip}:{_port}");
                startTable.AddRow("用途", "世界聊天实时推送");
                startTable.AddRow("启动时间", DateTime.Now.ToString("HH:mm:ss"));
                startTable.Render();

                // 异步接受客户端连接
                Thread acceptThread = new Thread(AcceptClients);
                acceptThread.IsBackground = true;
                acceptThread.Start();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ChatSocket] ❌ 启动失败: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        private void AcceptClients()
        {
            while (_isRunning)
            {
                try
                {
                    Socket clientSocket = _serverSocket.Accept();

                    // 异步处理每个客户端
                    Thread clientThread = new Thread(() => HandleClient(clientSocket));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[ChatSocket] Accept error: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }

        private void HandleClient(Socket clientSocket)
        {
            string playerId = null;
            try
            {
                // 接收客户端发送的 PlayerId（认证）
                byte[] buffer = new byte[1024];
                int received = clientSocket.Receive(buffer);
                string data = Encoding.UTF8.GetString(buffer, 0, received);

                var authData = JsonConvert.DeserializeObject<ChatAuthData>(data);
                playerId = authData.PlayerId;

                // 注册客户端
                _clients[playerId] = clientSocket;

                var connTable = new ConsoleTableFormatter();
                connTable.AddColumn("事件", "玩家", "在线", "时间");
                connTable.SetHeaderColor(ConsoleColor.Cyan);
                connTable.AddRow("聊天连接", authData.PlayerName, _clients.Count, DateTime.Now.ToString("HH:mm:ss"));
                connTable.Render();

                // 发送确认
                SendToClient(clientSocket, new ChatMessage
                {
                    Type = "auth_success",
                    Message = "聊天连接成功"
                });

                // 保持连接，等待服务器推送消息
                while (_isRunning && clientSocket.Connected)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[ChatSocket] 客户端断开: {ex.Message}");
                Console.ResetColor();
            }
            finally
            {
                // 移除客户端
                if (!string.IsNullOrEmpty(playerId))
                {
                    _clients.TryRemove(playerId, out _);
                }

                try
                {
                    clientSocket?.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// 广播聊天消息给所有在线玩家
        /// </summary>
        public void BroadcastMessage(string playerName, string companyName, string message)
        {
            var chatMessage = new ChatMessage
            {
                Type = "world_chat",
                PlayerName = playerName,
                CompanyName = companyName,
                Message = message,
                Timestamp = DateTime.Now
            };

            int successCount = 0;
            var deadClients = new System.Collections.Generic.List<string>();

            foreach (var kvp in _clients)
            {
                try
                {
                    SendToClient(kvp.Value, chatMessage);
                    successCount++;
                }
                catch
                {
                    deadClients.Add(kvp.Key);
                }
            }

            // 清理断开的客户端
            foreach (var playerId in deadClients)
            {
                _clients.TryRemove(playerId, out _);
            }

            // 用表格形式显示聊天消息
            var table = new ConsoleTableFormatter();
            table.AddColumn("玩家", "公司", "消息", "推送");
            table.SetHeaderColor(ConsoleColor.Cyan);
            table.AddRow(playerName, companyName, message, $"{successCount}/{_clients.Count} 人");
            table.Render();
        }

        private void SendToClient(Socket clientSocket, ChatMessage message)
        {
            string json = JsonConvert.SerializeObject(message);
            byte[] data = Encoding.UTF8.GetBytes(json);

            // 添加消息长度前缀（4字节）
            byte[] length = BitConverter.GetBytes(data.Length);
            byte[] fullData = new byte[4 + data.Length];
            Array.Copy(length, 0, fullData, 0, 4);
            Array.Copy(data, 0, fullData, 4, data.Length);

            clientSocket.Send(fullData);
        }

        public int GetOnlineCount()
        {
            return _clients.Count;
        }

        public void Stop()
        {
            _isRunning = false;

            // 关闭所有客户端连接
            foreach (var kvp in _clients)
            {
                try
                {
                    kvp.Value?.Close();
                }
                catch { }
            }
            _clients.Clear();

            // 关闭服务器
            try
            {
                _serverSocket?.Close();
            }
            catch { }

            Console.WriteLine("[ChatSocket] 服务器已停止");
        }
    }

    /// <summary>
    /// 聊天认证数据
    /// </summary>
    public class ChatAuthData
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }
        public string Token { get; set; }
    }

    /// <summary>
    /// 聊天消息
    /// </summary>
    public class ChatMessage
    {
        public string Type { get; set; }  // auth_success, world_chat
        public string PlayerName { get; set; }
        public string CompanyName { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
