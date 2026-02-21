using System;
using System.Collections.Concurrent;
using IOCP;
using Word_Sever;
using Word_Sever.Storage;

namespace Word_Sever.Servc
{
    /// <summary>
    /// 游戏服务器 - 处理GameCompany客户端连接和消息
    /// 继承IOCP服务器框架,处理11种客户端命令
    /// </summary>
    public class GameServer : IOCPServer<Pkg>
    {
        // 客户端会话字典 - key: 玩家ID, value: 客户端Token
        private ConcurrentDictionary<string, IOCPToken<Pkg>> _clientSessions;

        // 消息队列 - 在主线程中处理
        private ConcurrentQueue<Action> _messageQueue;

        // 本地数据存储
        private LocalDataStorage _storage;

        // 日志工具
        private LogTool _log;

        // HTTP模式：世界聊天消息队列（最多保留100条）
        private ConcurrentQueue<WorldChatMessage> _worldChatMessages = new ConcurrentQueue<WorldChatMessage>();
        private const int MAX_CHAT_MESSAGES = 100;

        /// <summary>
        /// 公开访问客户端会话字典 - 用于GM命令系统
        /// </summary>
        public ConcurrentDictionary<string, IOCPToken<Pkg>> ClientSessions => _clientSessions;

        /// <summary>
        /// 构造函数（IOCP模式，已废弃）
        /// </summary>
        public GameServer() : base(1024 * 1024) // 1MB缓冲区
        {
            _clientSessions = new ConcurrentDictionary<string, IOCPToken<Pkg>>();
            _messageQueue = new ConcurrentQueue<Action>();
        }

        /// <summary>
        /// 构造函数（HTTP模式）
        /// </summary>
        public GameServer(LocalDataStorage storage) : base(1024 * 1024)
        {
            _clientSessions = new ConcurrentDictionary<string, IOCPToken<Pkg>>();
            _messageQueue = new ConcurrentQueue<Action>();
            _storage = storage;
            _log = OpenProgress.Instance.log;
            _log.LogGreen("[GameServer] HTTP模式初始化完成");
        }

        /// <summary>
        /// 初始化游戏服务器（IOCP模式，已废弃）
        /// </summary>
        public void Init(LocalDataStorage storage)
        {
            _storage = storage;
            _log = OpenProgress.Instance.log;

            try
            {
                var result = InitIOCPServer(
                    log_action: (str) => _log.LogGreen(str),
                    maxCount: 1000,
                    allowMorePlayerEnter: true,
                    ip: "0.0.0.0",
                    port: 45677
                );

                if (result == IOCPState.Successful)
                {
                    _log.LogGreen("[GameServer] 服务器启动成功 - 端口: 45677");
                }
                else
                {
                    _log.LogError("[GameServer] 服务器启动失败");
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] 初始化异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理新客户端接入
        /// </summary>
        public override void AcceptClient(IOCPToken<Pkg> client)
        {
            try
            {
                _log.LogGreen($"[GameServer] 新客户端连接");
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] AcceptClient异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理客户端断开连接
        /// </summary>
        public override void OnCloseAccpet(IOCPToken<Pkg> client)
        {
            try
            {
                // 查找并移除断开的客户端
                string disconnectedPlayerId = null;
                foreach (var session in _clientSessions)
                {
                    if (session.Value == client)
                    {
                        disconnectedPlayerId = session.Key;
                        break;
                    }
                }

                if (disconnectedPlayerId != null)
                {
                    _clientSessions.TryRemove(disconnectedPlayerId, out _);
                    _log.LogYellow($"[GameServer] 玩家断开连接: {disconnectedPlayerId}, 当前在线: {_clientSessions.Count}");

                    // 广播玩家离线消息
                    BroadcastMessage($"玩家 {disconnectedPlayerId} 已离线", disconnectedPlayerId);
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] OnCloseAccpet异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 接收客户端消息 - 放入消息队列异步处理
        /// </summary>
        public override void OnReceiveMessage(IOCPToken<Pkg> client, Pkg message)
        {
            try
            {
                // 将消息处理放入队列,在主线程中执行
                _messageQueue.Enqueue(() => ProcessMessage(client, message));
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] OnReceiveMessage异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理消息队列 - 在主线程中调用
        /// </summary>
        public void ProcessMessageQueue()
        {
            try
            {
                int processedCount = 0;
                while (_messageQueue.TryDequeue(out var action) && processedCount < 100)
                {
                    action?.Invoke();
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] ProcessMessageQueue异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理客户端消息 - 根据ClientCMD分发
        /// </summary>
        private void ProcessMessage(IOCPToken<Pkg> client, Pkg pkg)
        {
            try
            {
                if (pkg?.Head == null || pkg?.Body?.clientMessage == null)
                {
                    _log.LogError("[GameServer] 收到空消息包");
                    return;
                }

                var cmd = pkg.Head.ClientCmd;
                var clientMsg = pkg.Body.clientMessage;

                _log.LogGreen($"[GameServer] >>> 收到命令: {cmd}, 玩家: {clientMsg.Id} ({clientMsg.Name})");

                switch (cmd)
                {
                    case ClientCMD.Join:
                        HandleJoin(client, clientMsg);
                        break;
                    case ClientCMD.Remove:
                        HandleRemove(client, clientMsg);
                        break;
                    case ClientCMD.GetJson:
                        HandleGetJson(client, clientMsg);
                        break;
                    case ClientCMD.SetJson:
                        HandleSetJson(client, clientMsg);
                        break;
                    case ClientCMD.GetStock:
                        HandleGetStock(client, clientMsg);
                        break;
                    case ClientCMD.BuyStock:
                        HandleBuyStock(client, clientMsg);
                        break;
                    case ClientCMD.SellStock:
                        HandleSellStock(client, clientMsg);
                        break;
                    case ClientCMD.SearchStock:
                        HandleSearchStock(client, clientMsg);
                        break;
                    case ClientCMD.Message:
                        HandleMessage(client, clientMsg);
                        break;
                    case ClientCMD.Donat:
                        HandleDonat(client, clientMsg);
                        break;
                    case ClientCMD.CheckPlayerCreatByFirst:
                        HandleCheckPlayerCreatByFirst(client, clientMsg);
                        break;
                    default:
                        _log.LogError($"[GameServer] 未知命令: {cmd}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] ProcessMessage异常: {ex.Message}");
            }
        }

        #region 命令处理器

        /// <summary>
        /// 处理玩家加入
        /// </summary>
        private void HandleJoin(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var playerId = msg.Id;
                var playerName = msg.Name;
                var companyName = msg.companyName;

                // 添加到会话字典
                _clientSessions.AddOrUpdate(playerId, client, (key, old) => client);

                _log.LogGreen($"[GameServer] 玩家加入: {playerName}({playerId}), 公司: {companyName}, 在线人数: {_clientSessions.Count}");

                // 回复加入成功
                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.ServerMessage },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = playerId,
                            clientName = playerName,
                            companyName = companyName,
                            Message = "加入成功"
                        }
                    }
                };
                client.Send(response);

                // 广播玩家上线
                BroadcastMessage($"玩家 {playerName} 已上线", playerId);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleJoin异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleJoin堆栈: {ex.StackTrace}");
                SendErrorResponse(client, ServerCMD.ServerMessage, msg, $"加入失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理玩家离开
        /// </summary>
        private void HandleRemove(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var playerId = msg.Id;
                _clientSessions.TryRemove(playerId, out _);

                _log.LogYellow($"[GameServer] 玩家离开: {playerId}, 在线人数: {_clientSessions.Count}");

                // 广播玩家离线
                BroadcastMessage($"玩家 {msg.Name} 已离线", playerId);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleRemove异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleRemove堆栈: {ex.StackTrace}");
                SendErrorResponse(client, ServerCMD.ServerMessage, msg, $"离线失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理获取JSON数据
        /// </summary>
        private void HandleGetJson(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                _log.LogGreen($"[GameServer] ========== HandleGetJson v2.0 (含JsonDoubleKey) ==========");
                _log.LogGreen($"[GameServer] 玩家: {msg.Id} ({msg.Name})");
                _log.LogGreen($"[GameServer] JsonKey: {msg.JsonKey}, JsonDicKey: {msg.JsonDicKey}, JsonDoubleKey: {msg.JsonDoubleKey}");
                int requestValueLength = msg.JsonValue?.Length ?? 0;
                _log.LogGreen($"[GameServer] GetJson请求参数 - Id: {msg.Id}, Name: {msg.Name}, Company: {msg.companyName}, JsonKey: {msg.JsonKey ?? "(空)"}, JsonDicKey: {msg.JsonDicKey ?? "(空)"}, JsonDoubleKey: {msg.JsonDoubleKey ?? "(空)"}, JsonValueLength: {requestValueLength}");
                bool isBuildingBlockRequest = string.Equals(msg.JsonDoubleKey, "block", StringComparison.OrdinalIgnoreCase);
                string effectiveJsonDicKey = msg.JsonDicKey;
                if (string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(msg.JsonDoubleKey) && !string.IsNullOrEmpty(msg.JsonKey))
                {
                    effectiveJsonDicKey = msg.JsonKey;
                    _log.LogYellow($"[GameServer] GetJson 参数容错: JsonDicKey为空，已回退使用JsonKey作为Hash Key -> {effectiveJsonDicKey}");
                }
                bool buildingDiskCheckExecuted = false;
                bool buildingPersistedFound = false;
                bool buildingDiskMatchMemory = false;
                string buildingVerifyDetail = "未执行";
                string hashDataFilePath = _storage.GetHashDataFilePath();
                string hashDataFileStatusBeforeRead = _storage.GetHashDataFileStatus();
                _log.LogGreen($"[GameServer] JsonValueLength(Req): {requestValueLength}, IsBuildingBlockRequest: {isBuildingBlockRequest}, EffectiveJsonDicKey: {effectiveJsonDicKey ?? "(空)"}, HashDataFile: {hashDataFilePath}");
                _log.LogGreen($"[GameServer] hash_data.json状态(读取前): {hashDataFileStatusBeforeRead}");
                _log.LogGreen($"[GameServer] 持久化状态(读取前): {_storage.GetPersistenceDebugInfo()}");

                string value = null;
                string logMsg;

                // 判断是使用String还是Hash
                if (!string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(msg.JsonDoubleKey))
                {
                    // Hash操作: HashGet(JsonDicKey, JsonDoubleKey)
                    value = _storage.HashGet(effectiveJsonDicKey, msg.JsonDoubleKey);
                    int valueLength = value?.Length ?? 0;
                    logMsg = $"[GameServer] GetJson Hash - Key: {effectiveJsonDicKey}, Field: {msg.JsonDoubleKey}, Length: {valueLength}, IsNull: {value == null}";
                    _log.LogGreen(logMsg);

                    if (isBuildingBlockRequest)
                    {
                        string persistedValue;
                        string verifyDetail;
                        bool persistedFound = _storage.TryVerifyHashPersisted(effectiveJsonDicKey, msg.JsonDoubleKey, out persistedValue, out verifyDetail);
                        bool diskMatchMemory = persistedFound && string.Equals(persistedValue ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal);
                        buildingDiskCheckExecuted = true;
                        buildingPersistedFound = persistedFound;
                        buildingDiskMatchMemory = diskMatchMemory;
                        buildingVerifyDetail = verifyDetail ?? "(空)";
                        _log.LogGreen($"[GameServer] Building GetJson disk-check - Found: {persistedFound}, IsMatchMemory: {diskMatchMemory}, Detail: {verifyDetail}, HashDataFile: {hashDataFilePath}");
                    }
                }
                else if (!string.IsNullOrEmpty(msg.JsonKey))
                {
                    // String操作: StringGet(JsonKey)
                    value = _storage.StringGet(msg.JsonKey);
                    int valueLength = value?.Length ?? 0;
                    logMsg = $"[GameServer] GetJson String - Key: {msg.JsonKey}, Length: {valueLength}, IsNull: {value == null}";
                    _log.LogGreen(logMsg);

                    // 如果返回null，记录警告
                    if (value == null)
                    {
                        _log.LogYellow($"[GameServer] 警告：Key '{msg.JsonKey}' 不存在，将返回空字符串给客户端！");
                    }
                }
                else
                {
                    _log.LogYellow("[GameServer] GetJson参数不完整 - 未提供JsonKey，也未提供JsonDicKey+JsonDoubleKey");
                }

                // 回复数据
                string responseValue = value ?? "";
                string responseDicKey = !string.IsNullOrEmpty(effectiveJsonDicKey) ? effectiveJsonDicKey : msg.JsonKey ?? "";
                string responsePreview = responseValue.Length > 120 ? responseValue.Substring(0, 120) + "..." : responseValue;

                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.ReturnJson },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            JsonValue = responseValue,
                            JsonDicKey = responseDicKey ?? "",
                            JsonDoubleKey = msg.JsonDoubleKey ?? "",  // 返回字段名以便客户端匹配
                            Message = value != null ? "获取成功" : "数据不存在"
                        }
                    }
                };
                _log.LogGreen($"[GameServer] 返回数据 - JsonDicKey: {responseDicKey}, JsonDoubleKey: {msg.JsonDoubleKey ?? "(空)"}, ResponseValue长度: {responseValue.Length}, Message: {response.Body.serverMessage.Message}");
                _log.LogGreen($"[GameServer] GetJson返回值 - JsonDicKey: {responseDicKey}, JsonDoubleKey: {msg.JsonDoubleKey ?? "(空)"}, JsonValueLength: {responseValue.Length}, JsonValue预览: {(responseValue.Length > 0 ? responsePreview : "(空)")}");
                _log.LogGreen($"[GameServer] hash_data.json状态(读取后): {_storage.GetHashDataFileStatus()}");
                _log.LogGreen($"[GameServer] 持久化状态(读取后): {_storage.GetPersistenceDebugInfo()}");
                if (isBuildingBlockRequest)
                {
                    _log.LogGreen($"[GameServer] Building GetJson链路汇总 - ReqJsonDicKey: {effectiveJsonDicKey ?? "(空)"}, ReqJsonDoubleKey: {msg.JsonDoubleKey ?? "(空)"}, ReqJsonValueLength: {requestValueLength}, DiskCheckExecuted: {buildingDiskCheckExecuted}, PersistedFound: {buildingPersistedFound}, DiskMatchMemory: {buildingDiskMatchMemory}, VerifyDetail: {buildingVerifyDetail}, ReturnValueLength: {responseValue.Length}, HashDataFile: {hashDataFilePath}");
                }
                client.Send(response);
                _log.LogGreen($"[GameServer] === HandleGetJson 结束 ===");
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleGetJson异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleGetJson堆栈: {ex.StackTrace}");

                string errorDicKey = !string.IsNullOrEmpty(msg?.JsonDicKey) ? msg.JsonDicKey : msg?.JsonKey ?? "";
                SendErrorResponse(
                    client,
                    ServerCMD.ReturnJson,
                    msg,
                    $"获取失败: {ex.Message}",
                    serverMessage =>
                    {
                        serverMessage.JsonDicKey = errorDicKey;
                        serverMessage.JsonDoubleKey = msg?.JsonDoubleKey ?? "";
                        serverMessage.JsonValue = "";
                    });
            }
        }

        /// <summary>
        /// 处理设置JSON数据
        /// </summary>
        private void HandleSetJson(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                _log.LogGreen($"[GameServer] === HandleSetJson 开始 ===");
                _log.LogGreen($"[GameServer] 玩家: {msg.Id} ({msg.Name})");
                _log.LogGreen($"[GameServer] JsonKey: {msg.JsonKey}, JsonDicKey: {msg.JsonDicKey}, JsonDoubleKey: {msg.JsonDoubleKey}");
                string normalizedJsonValue = msg.JsonValue ?? string.Empty;
                if (msg.JsonValue == null)
                {
                    _log.LogYellow("[GameServer] SetJson收到null值，已转换为空字符串后写入");
                }
                int requestValueLength = normalizedJsonValue.Length;
                bool isBuildingBlockRequest = string.Equals(msg.JsonDoubleKey, "block", StringComparison.OrdinalIgnoreCase);
                string effectiveJsonDicKey = msg.JsonDicKey;
                if (string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(msg.JsonDoubleKey) && !string.IsNullOrEmpty(msg.JsonKey))
                {
                    effectiveJsonDicKey = msg.JsonKey;
                    _log.LogYellow($"[GameServer] SetJson 参数容错: JsonDicKey为空，已回退使用JsonKey作为Hash Key -> {effectiveJsonDicKey}");
                }
                string hashDataFilePath = _storage.GetHashDataFilePath();
                string hashDataFileStatusBeforeSet = _storage.GetHashDataFileStatus();
                _log.LogGreen($"[GameServer] JsonValueLength: {requestValueLength}, IsBuildingBlockRequest: {isBuildingBlockRequest}, EffectiveJsonDicKey: {effectiveJsonDicKey ?? "(空)"}, SaveMode: {_storage.GetSaveMode()}, HashDataFile: {hashDataFilePath}");
                _log.LogGreen($"[GameServer] SetJson请求参数 - Id: {msg.Id}, Name: {msg.Name}, Company: {msg.companyName}, JsonKey: {msg.JsonKey ?? "(空)"}, JsonDicKey: {msg.JsonDicKey ?? "(空)"}, JsonDoubleKey: {msg.JsonDoubleKey ?? "(空)"}, JsonValueLength: {requestValueLength}");
                _log.LogGreen($"[GameServer] hash_data.json状态(写入前): {hashDataFileStatusBeforeSet}");
                _log.LogGreen($"[GameServer] 持久化状态(写入前): {_storage.GetPersistenceDebugInfo()}");
                bool saveSucceeded = false;
                string responseDicKey = !string.IsNullOrEmpty(effectiveJsonDicKey) ? effectiveJsonDicKey : msg.JsonKey;
                bool hashSetInvoked = false;
                bool hashSetResultForSummary = false;
                bool diskVerifyAttempted = false;
                bool diskPersistedFound = false;
                bool diskMatchRequest = false;
                bool forceSaveTriggered = false;
                bool forceSaveResultForSummary = true;
                string diskVerifyDetail = "未执行";

                // 判断是使用String还是Hash
                if (!string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(msg.JsonDoubleKey))
                {
                    // Hash操作: HashSet(JsonDicKey, JsonDoubleKey, JsonValue)
                    int valueLength = normalizedJsonValue.Length;
                    _log.LogGreen($"[GameServer] SetJson Hash操作 - Key: {effectiveJsonDicKey}, Field: {msg.JsonDoubleKey}, Value长度: {valueLength}");

                    if (valueLength > 0)
                    {
                        // 显示前100个字符用于调试
                        string preview = normalizedJsonValue.Length > 100 ? normalizedJsonValue.Substring(0, 100) + "..." : normalizedJsonValue;
                        _log.LogGreen($"[GameServer] Value预览: {preview}");
                    }

                    _log.LogGreen($"[GameServer] 调用 _storage.HashSet()...");
                    bool hashSetResult = _storage.HashSet(effectiveJsonDicKey, msg.JsonDoubleKey, normalizedJsonValue);
                    hashSetInvoked = true;
                    hashSetResultForSummary = hashSetResult;
                    _log.LogGreen($"[GameServer] _storage.HashSet() 返回: {hashSetResult}");
                    bool saveTriggeredByHashSet = _storage.GetSaveMode() == SaveMode.Instant;
                    string saveToDiskResult = saveTriggeredByHashSet ? hashSetResult.ToString() : "NotTriggered(IntervalMode)";
                    _log.LogGreen($"[GameServer] HashSet调用参数和返回值 - JsonDicKey: {effectiveJsonDicKey}, JsonDoubleKey: {msg.JsonDoubleKey}, JsonValueLength: {requestValueLength}, HashSetResult: {hashSetResult}, SaveMode: {_storage.GetSaveMode()}, SaveToDiskResult: {saveToDiskResult}");
                    _log.LogGreen($"[GameServer] hash_data.json状态(HashSet后): {_storage.GetHashDataFileStatus()}");
                    _log.LogGreen($"[GameServer] 持久化状态(HashSet后): {_storage.GetPersistenceDebugInfo()}");
                    if (!hashSetResult)
                    {
                        _log.LogError($"[GameServer] HashSet执行失败 - Key: {effectiveJsonDicKey}, Field: {msg.JsonDoubleKey}");
                        SendErrorResponse(
                            client,
                            ServerCMD.ReturnJson,
                            msg,
                            "保存失败: HashSet写入未成功",
                            serverMessage =>
                            {
                                serverMessage.JsonDicKey = responseDicKey ?? "";
                                serverMessage.JsonDoubleKey = msg.JsonDoubleKey ?? "";
                            });
                        return;
                    }

                    string memoryValue = _storage.HashGet(effectiveJsonDicKey, msg.JsonDoubleKey);
                    int memoryValueLength = memoryValue?.Length ?? 0;
                    bool memoryMatch = string.Equals(memoryValue ?? string.Empty, normalizedJsonValue, StringComparison.Ordinal);
                    _log.LogGreen($"[GameServer] HashSet写后内存校验 - Key: {effectiveJsonDicKey}, Field: {msg.JsonDoubleKey}, Value长度: {memoryValueLength}, IsMatchRequest: {memoryMatch}");
                    if (!memoryMatch)
                    {
                        _log.LogYellow($"[GameServer] 警告：HashSet写后内存值与请求值不一致，Key: {effectiveJsonDicKey}, Field: {msg.JsonDoubleKey}");
                    }

                    string persistedValue;
                    string verifyDetail;
                    bool persistedFound = _storage.TryVerifyHashPersisted(effectiveJsonDicKey, msg.JsonDoubleKey, out persistedValue, out verifyDetail);
                    bool diskMatch = persistedFound && string.Equals(persistedValue ?? string.Empty, normalizedJsonValue, StringComparison.Ordinal);
                    diskVerifyAttempted = true;
                    diskPersistedFound = persistedFound;
                    diskMatchRequest = diskMatch;
                    diskVerifyDetail = verifyDetail ?? "(空)";
                    _log.LogGreen($"[GameServer] HashSet写后磁盘校验 - Found: {persistedFound}, IsMatchRequest: {diskMatch}, Detail: {verifyDetail}, HashDataFile: {hashDataFilePath}");
                    if (!persistedFound || !diskMatch)
                    {
                        _log.LogYellow($"[GameServer] 警告：hash_data.json校验未通过，Key: {effectiveJsonDicKey}, Field: {msg.JsonDoubleKey}");

                        if (isBuildingBlockRequest)
                        {
                            _log.LogYellow("[GameServer] Building SetJson disk-check failed, forcing SaveToDisk retry...");
                            _log.LogGreen($"[GameServer] Building SetJson force SaveToDisk前状态: {_storage.GetPersistenceDebugInfo()}");
                            forceSaveTriggered = true;
                            bool forceSaveResult = _storage.SaveToDisk();
                            forceSaveResultForSummary = forceSaveResult;
                            _log.LogGreen($"[GameServer] Building SetJson force SaveToDisk result: {forceSaveResult}");
                            _log.LogGreen($"[GameServer] Building SetJson force SaveToDisk后状态: {_storage.GetPersistenceDebugInfo()}");

                            if (forceSaveResult)
                            {
                                persistedFound = _storage.TryVerifyHashPersisted(effectiveJsonDicKey, msg.JsonDoubleKey, out persistedValue, out verifyDetail);
                                diskMatch = persistedFound && string.Equals(persistedValue ?? string.Empty, normalizedJsonValue, StringComparison.Ordinal);
                                diskPersistedFound = persistedFound;
                                diskMatchRequest = diskMatch;
                                diskVerifyDetail = verifyDetail ?? "(空)";
                                _log.LogGreen($"[GameServer] Building SetJson disk-check retry - Found: {persistedFound}, IsMatchRequest: {diskMatch}, Detail: {verifyDetail}, HashDataFile: {hashDataFilePath}");
                            }

                            if (!forceSaveResult || !persistedFound || !diskMatch)
                            {
                                SendErrorResponse(
                                    client,
                                    ServerCMD.ReturnJson,
                                    msg,
                                    "保存失败: 建筑数据未能写入hash_data.json",
                                    serverMessage =>
                                    {
                                        serverMessage.JsonDicKey = responseDicKey ?? "";
                                        serverMessage.JsonDoubleKey = msg.JsonDoubleKey ?? "";
                                    });
                                return;
                            }
                        }
                    }

                    saveSucceeded = true;
                }
                else if (!string.IsNullOrEmpty(msg.JsonKey))
                {
                    // String操作: StringSet(JsonKey, JsonValue)
                    int valueLength = normalizedJsonValue.Length;
                    _log.LogGreen($"[GameServer] SetJson String操作 - Key: {msg.JsonKey}, Value长度: {valueLength}");

                    if (valueLength > 0)
                    {
                        string preview = normalizedJsonValue.Length > 100 ? normalizedJsonValue.Substring(0, 100) + "..." : normalizedJsonValue;
                        _log.LogGreen($"[GameServer] Value预览: {preview}");
                    }

                    _log.LogGreen($"[GameServer] 调用 _storage.StringSet()...");
                    _storage.StringSet(msg.JsonKey, normalizedJsonValue);
                    _log.LogGreen($"[GameServer] _storage.StringSet() 返回");
                    saveSucceeded = true;
                }
                else
                {
                    _log.LogError($"[GameServer] SetJson参数不完整 - 无法确定操作类型");
                }

                if (!saveSucceeded)
                {
                    SendErrorResponse(
                        client,
                        ServerCMD.ReturnJson,
                        msg,
                        "保存失败: 参数不完整，必须提供JsonKey或(JsonDicKey+JsonDoubleKey)",
                        serverMessage =>
                        {
                            serverMessage.JsonDicKey = responseDicKey ?? "";
                            serverMessage.JsonDoubleKey = msg?.JsonDoubleKey ?? "";
                        });
                    return;
                }

                // 回复设置成功
                _log.LogGreen($"[GameServer] 发送响应给客户端...");
                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.ReturnJson },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            JsonDicKey = responseDicKey ?? "",
                            JsonDoubleKey = msg.JsonDoubleKey ?? "",
                            Message = "保存成功"
                        }
                    }
                };
                client.Send(response);
                _log.LogGreen($"[GameServer] 响应已发送");
                _log.LogGreen($"[GameServer] hash_data.json状态(写入后): {_storage.GetHashDataFileStatus()}");
                _log.LogGreen($"[GameServer] HandleSetJson汇总 - SaveSucceeded: {saveSucceeded}, SaveMode: {_storage.GetSaveMode()}, JsonDicKey: {responseDicKey ?? "(空)"}, JsonDoubleKey: {msg.JsonDoubleKey ?? "(空)"}, JsonValueLength: {requestValueLength}, PersistenceState: {_storage.GetPersistenceDebugInfo()}");
                if (isBuildingBlockRequest)
                {
                    string forceSaveResultText = forceSaveTriggered ? forceSaveResultForSummary.ToString() : "NotTriggered";
                    _log.LogGreen($"[GameServer] Building SetJson链路汇总 - ReqJsonDicKey: {effectiveJsonDicKey ?? "(空)"}, ReqJsonDoubleKey: {msg.JsonDoubleKey ?? "(空)"}, ReqJsonValueLength: {requestValueLength}, HashSetInvoked: {hashSetInvoked}, HashSetResult: {hashSetResultForSummary}, DiskVerifyAttempted: {diskVerifyAttempted}, PersistedFound: {diskPersistedFound}, DiskMatchRequest: {diskMatchRequest}, ForceSaveTriggered: {forceSaveTriggered}, ForceSaveResult: {forceSaveResultText}, VerifyDetail: {diskVerifyDetail}, HashDataFile: {hashDataFilePath}");
                }
                _log.LogGreen($"[GameServer] === HandleSetJson 结束 ===");
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleSetJson异常: {ex.Message}");
                _log.LogError($"[GameServer] 堆栈跟踪: {ex.StackTrace}");

                string errorDicKey = !string.IsNullOrEmpty(msg?.JsonDicKey) ? msg.JsonDicKey : msg?.JsonKey ?? "";
                SendErrorResponse(
                    client,
                    ServerCMD.ReturnJson,
                    msg,
                    $"保存失败: {ex.Message}",
                    serverMessage =>
                    {
                        serverMessage.JsonDicKey = errorDicKey;
                        serverMessage.JsonDoubleKey = msg?.JsonDoubleKey ?? "";
                    });
            }
        }

        /// <summary>
        /// 处理获取股票信息
        /// </summary>
        private void HandleGetStock(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var stockId = msg.stockID;
                var stockKey = $"stock:{stockId}";

                // 从存储中获取股票数据
                var stockJson = _storage.StringGet(stockKey);

                // 确保返回 JSON 数组格式，不是对象
                if (string.IsNullOrEmpty(stockJson) || stockJson == "null" || stockJson == "{}")
                {
                    stockJson = "[]"; // 返回空数组，不是空对象
                }

                _log.LogGreen($"[GameServer] GetStock - StockID: {stockId}, Length: {stockJson.Length}");

                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.GetStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            jsonStock = stockJson,
                            Message = stockJson != "[]" ? "获取成功" : "股票不存在"
                        }
                    }
                };
                client.Send(response);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleGetStock异常: {ex.Message}");

                // 发送错误响应
                var errorResponse = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.GetStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            jsonStock = "[]",
                            Message = $"获取失败: {ex.Message}"
                        }
                    }
                };
                client.Send(errorResponse);
            }
        }

        /// <summary>
        /// 处理购买股票
        /// </summary>
        private void HandleBuyStock(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var playerId = msg.Id;
                var stockId = msg.stockID;
                var stockCompany = msg.stockCompany;
                var stockAmount = msg.StockMuch;

                _log.LogGreen($"[GameServer] BuyStock - 玩家: {playerId}, 股票: {stockCompany}({stockId}), 数量: {stockAmount}");

                // 保存玩家持股信息 - 使用Hash存储
                var playerStockKey = $"player_stock:{playerId}";
                var currentStockJson = _storage.HashGet(playerStockKey, stockId);

                int currentAmount = 0;
                if (!string.IsNullOrEmpty(currentStockJson))
                {
                    int.TryParse(currentStockJson, out currentAmount);
                }

                int newAmount = currentAmount + stockAmount;
                _storage.HashSet(playerStockKey, stockId, newAmount.ToString());

                _log.LogGreen($"[GameServer] 购买成功 - 原持股: {currentAmount}, 新持股: {newAmount}");

                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.BuyStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = playerId,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            AllowBuyStock = true,
                            Message = $"购买成功! 当前持有{stockCompany}股票: {newAmount}股"
                        }
                    }
                };
                client.Send(response);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleBuyStock异常: {ex.Message}");

                var errorResponse = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.BuyStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            AllowBuyStock = false,
                            Message = $"购买失败: {ex.Message}"
                        }
                    }
                };
                client.Send(errorResponse);
            }
        }

        /// <summary>
        /// 处理出售股票
        /// </summary>
        private void HandleSellStock(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var playerId = msg.Id;
                var stockId = msg.stockID;
                var stockCompany = msg.stockCompany;
                var stockAmount = msg.StockMuch;

                _log.LogGreen($"[GameServer] SellStock - 玩家: {playerId}, 股票: {stockCompany}({stockId}), 数量: {stockAmount}");

                // 获取玩家持股信息
                var playerStockKey = $"player_stock:{playerId}";
                var currentStockJson = _storage.HashGet(playerStockKey, stockId);

                int currentAmount = 0;
                if (!string.IsNullOrEmpty(currentStockJson))
                {
                    int.TryParse(currentStockJson, out currentAmount);
                }

                bool allowSell = currentAmount >= stockAmount;

                if (allowSell)
                {
                    int newAmount = currentAmount - stockAmount;
                    _storage.HashSet(playerStockKey, stockId, newAmount.ToString());

                    _log.LogGreen($"[GameServer] 出售成功 - 原持股: {currentAmount}, 新持股: {newAmount}");
                }
                else
                {
                    _log.LogYellow($"[GameServer] 出售失败 - 持股不足: {currentAmount} < {stockAmount}");
                }

                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.BuyStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = playerId,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            AllowBuyStock = allowSell,
                            Message = allowSell
                                ? $"出售成功! 当前持有{stockCompany}股票: {currentAmount - stockAmount}股"
                                : "出售失败! 持股数量不足"
                        }
                    }
                };
                client.Send(response);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleSellStock异常: {ex.Message}");

                // 发送错误响应
                var errorResponse = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.BuyStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            AllowBuyStock = false,
                            Message = $"出售失败: {ex.Message}"
                        }
                    }
                };
                client.Send(errorResponse);
            }
        }

        /// <summary>
        /// 处理搜索股票
        /// </summary>
        private void HandleSearchStock(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var searchKeyword = msg.stockCompany;

                _log.LogGreen($"[GameServer] SearchStock - 关键词: {searchKeyword}");

                // 这里可以实现更复杂的搜索逻辑
                // 简单实现: 返回匹配的股票列表JSON
                var stockListKey = "stock_list";
                var stockListJson = _storage.StringGet(stockListKey);

                // 确保返回 JSON 数组格式
                if (string.IsNullOrEmpty(stockListJson) || stockListJson == "null" || stockListJson == "{}")
                {
                    stockListJson = "[]";
                }

                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.SearchStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            jsonStock = stockListJson,
                            Message = "搜索完成"
                        }
                    }
                };
                client.Send(response);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleSearchStock异常: {ex.Message}");

                // 发送错误响应
                var errorResponse = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.SearchStock },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = msg.Id,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            jsonStock = "[]",
                            Message = $"搜索失败: {ex.Message}"
                        }
                    }
                };
                client.Send(errorResponse);
            }
        }

        /// <summary>
        /// 处理消息广播
        /// </summary>
        private void HandleMessage(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var playerId = msg.Id;
                var playerName = msg.Name;
                var message = msg.Message;

                _log.LogGreen($"[GameServer] Message - {playerName}: {message}");

                // 广播给所有在线玩家
                BroadcastMessage(message, playerId, playerName);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleMessage异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleMessage堆栈: {ex.StackTrace}");
                SendErrorResponse(client, ServerCMD.ClientMessage, msg, $"消息发送失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理捐赠
        /// </summary>
        private void HandleDonat(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var playerId = msg.Id;
                var playerName = msg.Name;
                var donatTarget = msg.Donat;
                var donatMoney = msg.DonatMoney;

                _log.LogGreen($"[GameServer] Donat - {playerName} 捐赠给 {donatTarget}: {donatMoney}");

                // 保存捐赠记录
                var donatKey = $"donat:{donatTarget}";
                var currentDonatJson = _storage.StringGet(donatKey);

                float currentTotal = 0;
                if (!string.IsNullOrEmpty(currentDonatJson))
                {
                    float.TryParse(currentDonatJson, out currentTotal);
                }

                float newTotal = currentTotal + donatMoney;
                _storage.StringSet(donatKey, newTotal.ToString());

                _log.LogGreen($"[GameServer] 捐赠成功 - {donatTarget} 总捐赠: {currentTotal} -> {newTotal}");

                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.ServerMessage },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = playerId,
                            clientName = playerName,
                            companyName = msg.companyName,
                            Donat = donatTarget,
                            DonatMoney = newTotal,
                            Message = $"捐赠成功! {donatTarget}当前总捐赠: {newTotal}"
                        }
                    }
                };
                client.Send(response);

                // 广播捐赠信息
                BroadcastMessage($"{playerName} 向 {donatTarget} 捐赠了 {donatMoney}", playerId);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleDonat异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleDonat堆栈: {ex.StackTrace}");
                SendErrorResponse(
                    client,
                    ServerCMD.ServerMessage,
                    msg,
                    $"捐赠失败: {ex.Message}",
                    serverMessage =>
                    {
                        serverMessage.Donat = msg.Donat;
                        // 保留原始请求金额,便于客户端恢复UI状态
                        serverMessage.DonatMoney = msg.DonatMoney;
                    });
            }
        }

        /// <summary>
        /// 检查玩家是否首次创建
        /// </summary>
        private void HandleCheckPlayerCreatByFirst(IOCPToken<Pkg> client, ClientMessage msg)
        {
            try
            {
                var playerId = msg.Id;
                var playerKey = $"player:{playerId}";

                // 检查玩家是否存在
                bool isFirstCreate = !_storage.KeyExists(playerKey);

                _log.LogGreen($"[GameServer] CheckPlayerCreatByFirst - 玩家: {playerId}, 首次创建: {isFirstCreate}");

                if (isFirstCreate)
                {
                    // 首次创建,保存玩家基本信息
                    _storage.HashSet(playerKey, "name", msg.Name);
                    _storage.HashSet(playerKey, "company", msg.companyName);
                    _storage.HashSet(playerKey, "createTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                var response = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.CheckPlayerCreatByFirst },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = playerId,
                            clientName = msg.Name,
                            companyName = msg.companyName,
                            FirstCreat = isFirstCreate,
                            Message = isFirstCreate ? "欢迎新玩家!" : "欢迎回来!",
                            JsonDicKey = playerId  // 添加此字段，客户端需要用它来匹配回调
                        }
                    }
                };
                client.Send(response);
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleCheckPlayerCreatByFirst异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleCheckPlayerCreatByFirst堆栈: {ex.StackTrace}");
                SendErrorResponse(
                    client,
                    ServerCMD.CheckPlayerCreatByFirst,
                    msg,
                    $"检查玩家失败: {ex.Message}",
                    serverMessage =>
                    {
                        serverMessage.FirstCreat = false;
                        serverMessage.JsonDicKey = msg?.Id ?? "";
                    });
            }
        }

        /// <summary>
        /// 统一发送错误响应,避免遗漏必要字段
        /// </summary>
        private void SendErrorResponse(
            IOCPToken<Pkg> client,
            ServerCMD serverCmd,
            ClientMessage request,
            string errorMessage,
            Action<ServerMessage> enrichMessage = null)
        {
            try
            {
                if (client == null)
                {
                    _log.LogError($"[GameServer] SendErrorResponse失败: client为空, Cmd: {serverCmd}");
                    return;
                }

                var serverMessage = new ServerMessage
                {
                    Id = request?.Id ?? "",
                    clientName = request?.Name ?? "",
                    companyName = request?.companyName ?? "",
                    Message = errorMessage ?? "操作失败"
                };

                enrichMessage?.Invoke(serverMessage);

                var errorResponse = new Pkg
                {
                    Head = new Head { ServerCmd = serverCmd },
                    Body = new Body
                    {
                        serverMessage = serverMessage
                    }
                };
                client.Send(errorResponse);
            }
            catch (Exception sendEx)
            {
                _log.LogError($"[GameServer] SendErrorResponse异常: {sendEx.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 广播消息给所有在线玩家
        /// </summary>
        private void BroadcastMessage(string message, string fromPlayerId, string fromPlayerName = "")
        {
            try
            {
                var pkg = new Pkg
                {
                    Head = new Head { ServerCmd = ServerCMD.ClientMessage },
                    Body = new Body
                    {
                        serverMessage = new ServerMessage
                        {
                            Id = fromPlayerId,
                            clientName = fromPlayerName,
                            companyName = "",
                            Message = message
                        }
                    }
                };

                foreach (var session in _clientSessions)
                {
                    try
                    {
                        // 不发送给自己
                        if (session.Key != fromPlayerId)
                        {
                            session.Value.Send(pkg);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError($"[GameServer] 广播消息失败 - 玩家: {session.Key}, 错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] BroadcastMessage异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取当前在线玩家数
        /// </summary>
        public int GetOnlinePlayerCount()
        {
            return _clientSessions.Count;
        }

        /// <summary>
        /// 关闭服务器
        /// </summary>
        public void Shutdown()
        {
            try
            {
                _log.LogYellow("[GameServer] 正在关闭服务器...");
                CloseServer();
                _clientSessions.Clear();
                _log.LogGreen("[GameServer] 服务器已关闭");
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] Shutdown异常: {ex.Message}");
            }
        }

        #region HTTP Handler Methods

        /// <summary>
        /// HTTP: 处理玩家加入
        /// </summary>
        public void HandleJoinHttp(HttpRequest request)
        {
            try
            {
                // 日志已在HttpServer中输出，此处静默
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleJoinHttp异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 处理玩家离开
        /// </summary>
        public void HandleRemoveHttp(HttpRequest request)
        {
            try
            {
                // 日志已在HttpServer中输出，此处静默
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleRemoveHttp异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 处理获取JSON数据
        /// </summary>
        private static string GetHttpDataString(Newtonsoft.Json.Linq.JObject data, string key)
        {
            string matchedKey;
            return GetHttpDataString(data, out matchedKey, key);
        }

        private static string GetHttpDataString(Newtonsoft.Json.Linq.JObject data, out string matchedKey, params string[] keys)
        {
            matchedKey = null;
            if (data == null || keys == null || keys.Length == 0)
            {
                return null;
            }

            foreach (var key in keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                Newtonsoft.Json.Linq.JToken token;
                string currentMatchedKey;
                if (!TryGetHttpDataToken(data, key, out token, out currentMatchedKey))
                {
                    continue;
                }

                matchedKey = currentMatchedKey;
                if (token.Type == Newtonsoft.Json.Linq.JTokenType.Null || token.Type == Newtonsoft.Json.Linq.JTokenType.Undefined)
                {
                    return null;
                }

                return token.ToString();
            }

            return null;
        }

        private static bool TryGetHttpDataToken(Newtonsoft.Json.Linq.JObject data, string key, out Newtonsoft.Json.Linq.JToken token, out string matchedKey)
        {
            token = null;
            matchedKey = null;

            if (data == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            if (data.TryGetValue(key, out token))
            {
                matchedKey = key;
                return true;
            }

            if (!data.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out token))
            {
                return false;
            }

            foreach (var property in data.Properties())
            {
                if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
                {
                    matchedKey = property.Name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(matchedKey))
            {
                matchedKey = key;
            }

            return true;
        }

        /// <summary>
        /// HTTP: 处理获取JSON数据
        /// </summary>
        public GetJsonResult HandleGetJsonHttp(HttpRequest request)
        {
            try
            {
                var data = request?.Data;
                string jsonKeySource;
                string jsonDicKeySource;
                string jsonDoubleKeySource;
                string jsonKey = GetHttpDataString(data, out jsonKeySource, "jsonKey", "JsonKey");
                string jsonDicKey = GetHttpDataString(data, out jsonDicKeySource, "jsonDicKey", "JsonDicKey");
                string jsonDoubleKey = GetHttpDataString(data, out jsonDoubleKeySource, "jsonDoubleKey", "JsonDoubleKey");
                bool isBuildingBlockRequest = string.Equals(jsonDoubleKey, "block", StringComparison.OrdinalIgnoreCase);
                string effectiveJsonDicKey = jsonDicKey;
                if (string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(jsonDoubleKey) && !string.IsNullOrEmpty(jsonKey))
                {
                    effectiveJsonDicKey = jsonKey;
                    _log.LogYellow($"[GameServer] HTTP GetJson 参数容错: jsonDicKey为空，已回退使用jsonKey作为Hash Key -> {effectiveJsonDicKey}");
                }

                string hashDataFilePath = _storage.GetHashDataFilePath();
                string value = null;

                if (!string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(jsonDoubleKey))
                {
                    // Hash操作: HashGet(jsonDicKey, jsonDoubleKey)
                    value = _storage.HashGet(effectiveJsonDicKey, jsonDoubleKey);
                    return new GetJsonResult
                    {
                        Message = value != null ? "获取成功" : "数据不存在",
                        Data = new
                        {
                            jsonValue = value ?? "",
                            jsonDicKey = effectiveJsonDicKey,
                            jsonDoubleKey = jsonDoubleKey
                        }
                    };
                }
                else if (!string.IsNullOrEmpty(jsonKey))
                {
                    // String操作: StringGet(jsonKey)
                    value = _storage.StringGet(jsonKey);

                    return new GetJsonResult
                    {
                        Message = value != null ? "获取成功" : "数据不存在",
                        Data = new
                        {
                            jsonValue = value ?? "",
                            jsonKey = jsonKey,
                            jsonDicKey = jsonKey
                        }
                    };
                }
                else
                {
                    _log.LogError("[GameServer] GetJson参数不完整");
                    return new GetJsonResult
                    {
                        Message = "参数不完整",
                        Data = new
                        {
                            jsonValue = "",
                            jsonKey = jsonKey ?? "",
                            jsonDicKey = jsonDicKey ?? "",
                            jsonDoubleKey = jsonDoubleKey ?? ""
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleGetJsonHttp异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleGetJsonHttp堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 处理设置JSON数据
        /// </summary>
        public void HandleSetJsonHttp(HttpRequest request)
        {
            try
            {
                var data = request?.Data;
                string jsonKeySource;
                string jsonDicKeySource;
                string jsonDoubleKeySource;
                string jsonValueSource;
                string jsonKey = GetHttpDataString(data, out jsonKeySource, "jsonKey", "JsonKey");
                string jsonDicKey = GetHttpDataString(data, out jsonDicKeySource, "jsonDicKey", "JsonDicKey");
                string jsonDoubleKey = GetHttpDataString(data, out jsonDoubleKeySource, "jsonDoubleKey", "JsonDoubleKey");
                string jsonValue = GetHttpDataString(data, out jsonValueSource, "jsonValue", "JsonValue") ?? string.Empty;
                int valueLength = jsonValue.Length;
                string valuePreview = valueLength > 120 ? jsonValue.Substring(0, 120) + "..." : jsonValue;
                bool isBuildingBlockRequest = string.Equals(jsonDoubleKey, "block", StringComparison.OrdinalIgnoreCase);
                string effectiveJsonDicKey = jsonDicKey;
                if (string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(jsonDoubleKey) && !string.IsNullOrEmpty(jsonKey))
                {
                    effectiveJsonDicKey = jsonKey;
                    _log.LogYellow($"[GameServer] HTTP SetJson 参数容错: jsonDicKey为空，已回退使用jsonKey作为Hash Key -> {effectiveJsonDicKey}");
                }
                string hashDataFilePath = _storage.GetHashDataFilePath();

                // 只在Building数据时输出美化日志
                if (isBuildingBlockRequest && valueLength > 0)
                {
                    try
                    {
                        // 解析JSON以显示Building统计
                        var buildData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonValue);
                        int buildCount = buildData?.builds != null ?
                            ((Newtonsoft.Json.Linq.JArray)buildData.builds).Count : 0;

                        var buildTable = new ConsoleTableFormatter();
                        buildTable.AddColumn("事件", "玩家ID", "类型", "建筑数", "大小", "时间");
                        buildTable.SetHeaderColor(ConsoleColor.Yellow);
                        buildTable.AddRow(
                            "Building保存",
                            effectiveJsonDicKey.Substring(0, Math.Min(16, effectiveJsonDicKey.Length)) + "...",
                            jsonDoubleKey,
                            buildCount,
                            valueLength > 1000 ? $"{(valueLength / 1024.0):F1}KB" : $"{valueLength}B",
                            DateTime.Now.ToString("HH:mm:ss")
                        );
                        buildTable.Render();
                    }
                    catch
                    {
                        // 如果JSON解析失败，使用简单格式
                        _log.LogGreen($"[GameServer] 保存Building: {effectiveJsonDicKey}.{jsonDoubleKey} ({valueLength}字节)");
                    }
                }

                if (!string.IsNullOrEmpty(effectiveJsonDicKey) && !string.IsNullOrEmpty(jsonDoubleKey))
                {
                    // Hash操作: HashSet(jsonDicKey, jsonDoubleKey, jsonValue)
                    bool hashSetResult = _storage.HashSet(effectiveJsonDicKey, jsonDoubleKey, jsonValue);
                    if (!hashSetResult)
                    {
                        _log.LogError($"[GameServer] HashSet失败: {effectiveJsonDicKey}.{jsonDoubleKey}");
                        throw new InvalidOperationException($"HashSet写入失败: {effectiveJsonDicKey}.{jsonDoubleKey}");
                    }

                    string memoryValue = _storage.HashGet(effectiveJsonDicKey, jsonDoubleKey);
                    bool memoryMatch = string.Equals(memoryValue ?? string.Empty, jsonValue ?? string.Empty, StringComparison.Ordinal);

                    string persistedValue;
                    string verifyDetail;
                    bool persistedFound = _storage.TryVerifyHashPersisted(effectiveJsonDicKey, jsonDoubleKey, out persistedValue, out verifyDetail);
                    bool diskMatch = persistedFound && string.Equals(persistedValue ?? string.Empty, jsonValue ?? string.Empty, StringComparison.Ordinal);

                    bool forceSaveAttempted = false;
                    bool forceSaveResult = true;
                    if (isBuildingBlockRequest && !diskMatch)
                    {
                        forceSaveAttempted = true;
                        _log.LogYellow("[GameServer] HTTP Building SetJson disk-check failed, forcing SaveToDisk retry...");
                        forceSaveResult = _storage.SaveToDisk();

                        persistedFound = _storage.TryVerifyHashPersisted(effectiveJsonDicKey, jsonDoubleKey, out persistedValue, out verifyDetail);
                        diskMatch = persistedFound && string.Equals(persistedValue ?? string.Empty, jsonValue ?? string.Empty, StringComparison.Ordinal);

                        if (!forceSaveResult || !persistedFound || !diskMatch)
                        {
                            throw new InvalidOperationException(
                                $"建筑数据未写入hash_data.json: {effectiveJsonDicKey}.{jsonDoubleKey}, " +
                                $"SaveToDisk={forceSaveResult}, PersistedFound={persistedFound}, DiskMatch={diskMatch}, VerifyDetail={verifyDetail}");
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(jsonKey))
                {
                    // String操作: StringSet(jsonKey, jsonValue)
                    _storage.StringSet(jsonKey, jsonValue);
                }
                else
                {
                    _log.LogError("[GameServer] SetJson参数不完整");
                    throw new ArgumentException("参数不完整：必须提供jsonKey或(jsonDicKey+jsonDoubleKey)");
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleSetJsonHttp异常: {ex.Message}");
                _log.LogError($"[GameServer] HandleSetJsonHttp堆栈: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 处理获取股票信息
        /// </summary>
        public GetJsonResult HandleGetStockHttp(HttpRequest request, string stockId)
        {
            try
            {
                var stockKey = $"stock:{stockId}";
                var stockJson = _storage.StringGet(stockKey);

                if (string.IsNullOrEmpty(stockJson) || stockJson == "null" || stockJson == "{}")
                {
                    stockJson = "[]";
                }

                _log.LogGreen($"[GameServer] HTTP GetStock - StockID: {stockId}, Length: {stockJson.Length}");

                return new GetJsonResult
                {
                    Message = stockJson != "[]" ? "获取成功" : "股票不存在",
                    Data = new
                    {
                        jsonStock = stockJson
                    }
                };
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleGetStockHttp异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 处理购买股票
        /// </summary>
        public GetJsonResult HandleBuyStockHttp(HttpRequest request)
        {
            try
            {
                var data = request.Data;
                var playerId = request.PlayerId;
                var stockId = data["stockID"].ToString();
                var stockCompany = data["stockCompany"].ToString();
                var stockAmount = (int)data["stockMuch"];

                _log.LogGreen($"[GameServer] HTTP BuyStock - 玩家: {playerId}, 股票: {stockCompany}({stockId}), 数量: {stockAmount}");

                var playerStockKey = $"player_stock:{playerId}";
                var currentStockJson = _storage.HashGet(playerStockKey, stockId);

                int currentAmount = 0;
                if (!string.IsNullOrEmpty(currentStockJson))
                {
                    int.TryParse(currentStockJson, out currentAmount);
                }

                int newAmount = currentAmount + stockAmount;
                _storage.HashSet(playerStockKey, stockId, newAmount.ToString());

                _log.LogGreen($"[GameServer] 购买成功 - 原持股: {currentAmount}, 新持股: {newAmount}");

                return new GetJsonResult
                {
                    Message = $"购买成功! 当前持有{stockCompany}股票: {newAmount}股",
                    Data = new
                    {
                        allowBuyStock = true,
                        newAmount = newAmount
                    }
                };
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleBuyStockHttp异常: {ex.Message}");
                return new GetJsonResult
                {
                    Success = false,
                    Message = $"购买失败: {ex.Message}",
                    Data = new { allowBuyStock = false }
                };
            }
        }

        /// <summary>
        /// HTTP: 处理出售股票
        /// </summary>
        public GetJsonResult HandleSellStockHttp(HttpRequest request)
        {
            try
            {
                var data = request.Data;
                var playerId = request.PlayerId;
                var stockId = data["stockID"].ToString();
                var stockCompany = data["stockCompany"].ToString();
                var stockAmount = (int)data["stockMuch"];

                _log.LogGreen($"[GameServer] HTTP SellStock - 玩家: {playerId}, 股票: {stockCompany}({stockId}), 数量: {stockAmount}");

                var playerStockKey = $"player_stock:{playerId}";
                var currentStockJson = _storage.HashGet(playerStockKey, stockId);

                int currentAmount = 0;
                if (!string.IsNullOrEmpty(currentStockJson))
                {
                    int.TryParse(currentStockJson, out currentAmount);
                }

                bool allowSell = currentAmount >= stockAmount;

                if (allowSell)
                {
                    int newAmount = currentAmount - stockAmount;
                    _storage.HashSet(playerStockKey, stockId, newAmount.ToString());
                    _log.LogGreen($"[GameServer] 出售成功 - 原持股: {currentAmount}, 新持股: {newAmount}");

                    return new GetJsonResult
                    {
                        Message = $"出售成功! 当前持有{stockCompany}股票: {newAmount}股",
                        Data = new
                        {
                            allowBuyStock = true,
                            newAmount = newAmount
                        }
                    };
                }
                else
                {
                    _log.LogYellow($"[GameServer] 出售失败 - 持股不足: {currentAmount} < {stockAmount}");

                    return new GetJsonResult
                    {
                        Success = false,
                        Message = "出售失败! 持股数量不足",
                        Data = new
                        {
                            allowBuyStock = false,
                            currentAmount = currentAmount
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleSellStockHttp异常: {ex.Message}");
                return new GetJsonResult
                {
                    Success = false,
                    Message = $"出售失败: {ex.Message}",
                    Data = new { allowBuyStock = false }
                };
            }
        }

        /// <summary>
        /// HTTP: 处理搜索股票
        /// </summary>
        public GetJsonResult HandleSearchStockHttp(HttpRequest request)
        {
            try
            {
                var searchKeyword = request.Data.ContainsKey("stockCompany")
                    ? request.Data["stockCompany"].ToString()
                    : "";

                _log.LogGreen($"[GameServer] HTTP SearchStock - 关键词: {searchKeyword}");

                var stockListKey = "stock_list";
                var stockListJson = _storage.StringGet(stockListKey);

                if (string.IsNullOrEmpty(stockListJson) || stockListJson == "null" || stockListJson == "{}")
                {
                    stockListJson = "[]";
                }

                return new GetJsonResult
                {
                    Message = "搜索完成",
                    Data = new
                    {
                        jsonStock = stockListJson
                    }
                };
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleSearchStockHttp异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 处理消息广播
        /// </summary>
        public void HandleMessageHttp(HttpRequest request, string[] onlinePlayers)
        {
            try
            {
                var playerId = request.PlayerId;
                var playerName = request.PlayerName;
                var message = request.Data.ContainsKey("message")
                    ? request.Data["message"].ToString()
                    : "";

                if (string.IsNullOrEmpty(message))
                {
                    _log.LogYellow($"[GameServer] HTTP Message - 消息为空，忽略");
                    return;
                }

                _log.LogGreen($"[GameServer] HTTP Message - {playerName}: {message}");

                // 存储消息到队列
                var chatMessage = new WorldChatMessage
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    Message = message,
                    Timestamp = DateTime.Now
                };

                _worldChatMessages.Enqueue(chatMessage);

                // 保持队列大小不超过100条
                while (_worldChatMessages.Count > MAX_CHAT_MESSAGES)
                {
                    _worldChatMessages.TryDequeue(out _);
                }

                _log.LogGreen($"[GameServer] 消息已存储，队列长度: {_worldChatMessages.Count}");
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleMessageHttp异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 获取最近的聊天消息
        /// </summary>
        public GetJsonResult HandleGetMessagesHttp(HttpRequest request)
        {
            try
            {
                var messages = _worldChatMessages.ToArray();
                _log.LogGreen($"[GameServer] HTTP GetMessages - 返回 {messages.Length} 条消息");

                return new GetJsonResult
                {
                    Success = true,
                    Message = "获取成功",
                    Data = new
                    {
                        messages = messages
                    }
                };
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleGetMessagesHttp异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 处理捐赠
        /// </summary>
        public GetJsonResult HandleDonatHttp(HttpRequest request)
        {
            try
            {
                var data = request.Data;
                var playerId = request.PlayerId;
                var playerName = request.PlayerName;
                var donatTarget = data["donat"].ToString();
                var donatMoney = (float)data["donatMoney"];

                _log.LogGreen($"[GameServer] HTTP Donat - {playerName} 捐赠给 {donatTarget}: {donatMoney}");

                var donatKey = $"donat:{donatTarget}";
                var currentDonatJson = _storage.StringGet(donatKey);

                float currentTotal = 0;
                if (!string.IsNullOrEmpty(currentDonatJson))
                {
                    float.TryParse(currentDonatJson, out currentTotal);
                }

                float newTotal = currentTotal + donatMoney;
                _storage.StringSet(donatKey, newTotal.ToString());

                _log.LogGreen($"[GameServer] 捐赠成功 - {donatTarget} 总捐赠: {currentTotal} -> {newTotal}");

                return new GetJsonResult
                {
                    Message = $"捐赠成功! {donatTarget}当前总捐赠: {newTotal}",
                    Data = new
                    {
                        donat = donatTarget,
                        donatMoney = newTotal
                    }
                };
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleDonatHttp异常: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// HTTP: 检查玩家是否首次创建
        /// </summary>
        public GetJsonResult HandleCheckFirstHttp(HttpRequest request)
        {
            try
            {
                var playerId = request.PlayerId;
                var playerKey = $"player:{playerId}";

                bool isFirstCreate = !_storage.KeyExists(playerKey);

                _log.LogGreen($"[GameServer] HTTP CheckPlayerCreatByFirst - 玩家: {playerId}, 首次创建: {isFirstCreate}");

                if (isFirstCreate)
                {
                    _storage.HashSet(playerKey, "name", request.PlayerName);
                    _storage.HashSet(playerKey, "company", request.CompanyName);
                    _storage.HashSet(playerKey, "createTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                return new GetJsonResult
                {
                    Message = isFirstCreate ? "欢迎新玩家!" : "欢迎回来!",
                    Data = new
                    {
                        firstCreat = isFirstCreate,
                        jsonDicKey = playerId
                    }
                };
            }
            catch (Exception ex)
            {
                _log.LogError($"[GameServer] HandleCheckFirstHttp异常: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
