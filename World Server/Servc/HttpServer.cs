using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Word_Sever.Servc;

namespace Word_Sever
{
    /// <summary>
    /// HTTP 服务器 - 使用 HttpListener 替代 Socket/IOCP
    /// </summary>
    public class HttpServer
    {
        private HttpListener _listener;
        private GameServer _gameServer;
        private SessionManager _sessionManager;
        private ChatSocketServer _chatSocketServer;
        private bool _isRunning;
        private ConsoleTableFormatter _logTable;  // 持久的日志表格
        private bool _isFirstLog = true;  // 是否是第一次记录日志

        /// <summary>
        /// 获取会话管理器（供GM命令使用）
        /// </summary>
        public SessionManager SessionManager => _sessionManager;

        public void Init(GameServer gameServer, ChatSocketServer chatSocketServer, string ip, int port)
        {
            _gameServer = gameServer;
            _chatSocketServer = chatSocketServer;
            _sessionManager = new SessionManager();
            _listener = new HttpListener();

            // HttpListener 需要完整的 URL 格式
            string prefix = $"http://{(ip == "0.0.0.0" ? "+" : ip)}:{port}/";
            _listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            try
            {
                _listener.Start();
                _isRunning = true;

                // 表格形式的启动信息
                var startTable = new ConsoleTableFormatter();
                startTable.AddColumn("配置", "值");
                startTable.SetHeaderColor(ConsoleColor.Green);
                startTable.AddRow("HTTP服务器", "启动成功");
                startTable.AddRow("监听地址", string.Join(", ", _listener.Prefixes));
                startTable.AddRow("通信协议", "HTTP/1.1 + JSON");
                startTable.AddRow("认证方式", "Token-Based Session");
                startTable.AddRow("启动时间", DateTime.Now.ToString("HH:mm:ss"));
                startTable.Render();

                // 初始化持久的日志表格
                _logTable = new ConsoleTableFormatter();
                _logTable.AddColumn("操作", "玩家", "时间");
                _logTable.SetHeaderColor(ConsoleColor.Gray);
                _logTable.SetBorderColor(ConsoleColor.DarkGray);
                _logTable.SetColumnWidths(30, 12, 12);  // 设置固定列宽确保对齐
                _logTable.RenderHeader();  // 只渲染表头

                // 异步接受请求
                Task.Run(() => AcceptRequests());
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[HttpServer] ❌ 启动失败: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }

        private async void AcceptRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    // 异步处理每个请求
                    Task.Run(() => HandleRequest(context));
                }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[HttpServer] Accept error: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
        }

        private async void HandleRequest(HttpListenerContext context)
        {
            HttpResponse response;
            string path = context.Request.Url.AbsolutePath;
            string method = context.Request.HttpMethod;

            try
            {
                var request = await ParseRequest(context.Request);

                // 生成简洁的请求日志（显示关键信息）
                string logInfo = GetRequestLogInfo(path, request);

                // 用表格形式显示请求日志（添加到持久表格中）
                if (!string.IsNullOrEmpty(logInfo))
                {
                    _logTable.AddRow(logInfo, request?.PlayerName ?? "", DateTime.Now.ToString("HH:mm:ss"));
                    _logTable.RenderLastRow();  // 只渲染最后一行
                }

                // Token 验证（除了 join 和 check-first 接口）
                if (path != "/api/player/join" && path != "/api/player/check-first")
                {
                    if (!ValidateToken(request))
                    {
                        _logTable.AddRow("Token验证失败", request?.PlayerName ?? "未知", DateTime.Now.ToString("HH:mm:ss"));
                        _logTable.RenderLastRow();

                        response = new HttpResponse
                        {
                            Success = false,
                            Message = "Token验证失败或会话已过期"
                        };
                        await SendResponse(context.Response, response, 401);
                        return;
                    }
                }

                // 路由到对应的处理方法
                response = RouteRequest(path, method, request);
            }
            catch (Exception ex)
            {
                _logTable.AddRow("处理失败", ex.Message.Length > 30 ? ex.Message.Substring(0, 30) + "..." : ex.Message, DateTime.Now.ToString("HH:mm:ss"));
                _logTable.RenderLastRow();

                response = new HttpResponse
                {
                    Success = false,
                    Message = $"服务器错误: {ex.Message}"
                };
                await SendResponse(context.Response, response, 500);
                return;
            }

            await SendResponse(context.Response, response, 200);
        }

        private async Task<HttpRequest> ParseRequest(HttpListenerRequest request)
        {
            string body = "";
            // 强制使用UTF-8编码，避免中文乱码
            using (var reader = new StreamReader(request.InputStream, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            // 精简日志：仅在Debug模式下输出完整请求（已禁用，保持日志清爽）
            // #if DEBUG
            // if (body.Length < 500)
            // {
            //     Console.ForegroundColor = ConsoleColor.DarkGray;
            //     Console.WriteLine($"  ↪ {body}");
            //     Console.ResetColor();
            // }
            // #endif

            if (string.IsNullOrEmpty(body))
            {
                return new HttpRequest { Data = new JObject() };
            }

            try
            {
                var httpRequest = JsonConvert.DeserializeObject<HttpRequest>(body);
                if (httpRequest.Data == null)
                {
                    httpRequest.Data = new JObject();
                }
                return httpRequest;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[HttpServer] JSON解析失败: {ex.Message}");
                Console.WriteLine($"[HttpServer] 问题JSON: {body}");
                Console.ResetColor();
                throw;
            }
        }

        private bool ValidateToken(HttpRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Token))
            {
                return false;
            }

            return _sessionManager.ValidateToken(request.PlayerId, request.Token);
        }

        private HttpResponse RouteRequest(string path, string method, HttpRequest request)
        {
            switch (path)
            {
                case "/api/player/join":
                    return HandleJoinRequest(request);

                case "/api/player/remove":
                    return HandleRemoveRequest(request);

                case "/api/data/get":
                    return HandleGetJsonRequest(request);

                case "/api/data/set":
                    return HandleSetJsonRequest(request);

                case "/api/stock/buy":
                    return HandleBuyStockRequest(request);

                case "/api/stock/sell":
                    return HandleSellStockRequest(request);

                case "/api/message/send":
                    return HandleMessageRequest(request);

                case "/api/message/get":
                    return HandleGetMessagesRequest(request);

                case "/api/donation/submit":
                    return HandleDonatRequest(request);

                case "/api/player/check-first":
                    return HandleCheckFirstRequest(request);

                default:
                    // 处理动态路由
                    if (path.StartsWith("/api/stock/") && method == "GET")
                    {
                        var stockId = path.Replace("/api/stock/", "").Trim('/');
                        return HandleGetStockRequest(request, stockId);
                    }
                    if (path.StartsWith("/api/stock/search"))
                    {
                        return HandleSearchStockRequest(request);
                    }

                    return new HttpResponse
                    {
                        Success = false,
                        Message = $"未知的API端点: {path}"
                    };
            }
        }

        // ============= Handler 方法 =============

        private HttpResponse HandleJoinRequest(HttpRequest request)
        {
            // 检查玩家是否已在线（避免重复加入日志）
            var existingSession = _sessionManager.GetSession(request.PlayerId);
            bool isReconnect = existingSession != null;

            var token = _sessionManager.CreateSession(request.PlayerId, request.PlayerName, request.CompanyName);
            _gameServer.HandleJoinHttp(request);

            // 只在首次加入或完整信息时打印日志
            bool hasCompleteInfo = !string.IsNullOrEmpty(request.PlayerName) && !string.IsNullOrEmpty(request.CompanyName);

            if (!isReconnect && hasCompleteInfo)
            {
                // 玩家加入日志
                _logTable.AddRow($"玩家加入({_sessionManager.GetOnlineCount()}在线)", request.PlayerName, DateTime.Now.ToString("HH:mm:ss"));
                _logTable.RenderLastRow();
            }
            else if (isReconnect)
            {
                _logTable.AddRow("玩家重连", request.PlayerName, DateTime.Now.ToString("HH:mm:ss"));
                _logTable.RenderLastRow();
            }

            return new HttpResponse
            {
                Success = true,
                Message = "加入成功",
                Data = JObject.FromObject(new { token })
            };
        }

        private HttpResponse HandleRemoveRequest(HttpRequest request)
        {
            _sessionManager.RemoveSession(request.PlayerId);
            _gameServer.HandleRemoveHttp(request);

            _logTable.AddRow("玩家退出", request.PlayerName, DateTime.Now.ToString("HH:mm:ss"));
            _logTable.RenderLastRow();

            return new HttpResponse
            {
                Success = true,
                Message = "退出成功"
            };
        }

        private HttpResponse HandleGetJsonRequest(HttpRequest request)
        {
            var result = _gameServer.HandleGetJsonHttp(request);
            return new HttpResponse
            {
                Success = true,
                Message = result.Message,
                Data = JObject.FromObject(result.Data)
            };
        }

        private HttpResponse HandleSetJsonRequest(HttpRequest request)
        {
            _gameServer.HandleSetJsonHttp(request);
            return new HttpResponse
            {
                Success = true,
                Message = "保存成功"
            };
        }

        private HttpResponse HandleBuyStockRequest(HttpRequest request)
        {
            var result = _gameServer.HandleBuyStockHttp(request);
            return new HttpResponse
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data != null ? JObject.FromObject(result.Data) : null
            };
        }

        private HttpResponse HandleSellStockRequest(HttpRequest request)
        {
            var result = _gameServer.HandleSellStockHttp(request);
            return new HttpResponse
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data != null ? JObject.FromObject(result.Data) : null
            };
        }

        private HttpResponse HandleGetStockRequest(HttpRequest request, string stockId)
        {
            var result = _gameServer.HandleGetStockHttp(request, stockId);
            return new HttpResponse
            {
                Success = true,
                Message = result.Message,
                Data = result.Data != null ? JObject.FromObject(result.Data) : null
            };
        }

        private HttpResponse HandleSearchStockRequest(HttpRequest request)
        {
            var result = _gameServer.HandleSearchStockHttp(request);
            return new HttpResponse
            {
                Success = true,
                Message = result.Message,
                Data = result.Data != null ? JObject.FromObject(result.Data) : null
            };
        }

        private HttpResponse HandleMessageRequest(HttpRequest request)
        {
            // HTTP + Socket 混合模式：通过 Socket 实时推送聊天消息
            var message = request.Data["message"]?.ToString();

            if (string.IsNullOrEmpty(message))
            {
                return new HttpResponse
                {
                    Success = false,
                    Message = "消息内容不能为空"
                };
            }

            // 通过 ChatSocketServer 实时广播
            _chatSocketServer.BroadcastMessage(
                request.PlayerName,
                request.CompanyName,
                message
            );

            return new HttpResponse
            {
                Success = true,
                Message = "消息已发送"
            };
        }

        private HttpResponse HandleGetMessagesRequest(HttpRequest request)
        {
            var result = _gameServer.HandleGetMessagesHttp(request);
            return new HttpResponse
            {
                Success = result.Success,
                Message = result.Message,
                Data = result.Data != null ? JObject.FromObject(result.Data) : null
            };
        }

        private HttpResponse HandleDonatRequest(HttpRequest request)
        {
            var result = _gameServer.HandleDonatHttp(request);
            return new HttpResponse
            {
                Success = true,
                Message = result.Message,
                Data = result.Data != null ? JObject.FromObject(result.Data) : null
            };
        }

        private HttpResponse HandleCheckFirstRequest(HttpRequest request)
        {
            var result = _gameServer.HandleCheckFirstHttp(request);
            return new HttpResponse
            {
                Success = true,
                Message = result.Message,
                Data = result.Data != null ? JObject.FromObject(result.Data) : null
            };
        }

        // ============= 请求日志处理 =============

        private string GetRequestLogInfo(string path, HttpRequest request)
        {
            // 根据路径返回简洁的描述
            switch (path)
            {
                case "/api/player/join":
                    return null; // join 有专门的美化日志
                case "/api/player/remove":
                    return $"玩家退出";
                case "/api/data/get":
                    string getKey = request.Data?.ContainsKey("jsonDoubleKey") == true
                        ? request.Data["jsonDoubleKey"].ToString()
                        : request.Data?.ContainsKey("jsonDicKey") == true
                        ? request.Data["jsonDicKey"].ToString()
                        : request.Data?.ContainsKey("jsonKey")== true
                        ? request.Data["jsonKey"].ToString()
                        : "unknown";
                    return $"获取数据: {getKey}";
                case "/api/data/set":
                    string setKey = request.Data?.ContainsKey("jsonDoubleKey") == true
                        ? request.Data["jsonDoubleKey"].ToString()
                        : request.Data?.ContainsKey("jsonKey") == true
                        ? request.Data["jsonKey"].ToString()
                        : "unknown";
                    // 如果是 Building 数据，不显示（有专门的美化日志）
                    if (setKey == "block")
                        return null;
                    return $"保存数据: {setKey}";
                case "/api/stock/buy":
                    return "购买股票";
                case "/api/stock/sell":
                    return "出售股票";
                case "/api/message/send":
                    return null; // 聊天消息由ChatSocketServer的表格日志显示
                case "/api/message/get":
                    return null; // 消息轮询太频繁，不显示
                case "/api/player/check-first":
                    return null; // 登录检查，不显示
                default:
                    if (path.StartsWith("/api/stock/"))
                        return null; // 股票查询太频繁，不显示
                    return path;
            }
        }

        // ============= 响应处理 =============

        private async Task SendResponse(HttpListenerResponse response, HttpResponse data, int statusCode)
        {
            try
            {
                response.StatusCode = statusCode;
                response.ContentType = "application/json; charset=utf-8";

                // 添加 CORS 头（如果需要跨域）
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                var json = JsonConvert.SerializeObject(data);
                var buffer = Encoding.UTF8.GetBytes(json);

                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                // 只输出错误响应，成功响应静默
                if (statusCode != 200)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[HttpServer] Response {statusCode}: {data.Message}");
                    Console.ResetColor();
                }
            }
            finally
            {
                response.Close();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            Console.WriteLine("[HttpServer] 服务器已停止");
        }
    }
}
