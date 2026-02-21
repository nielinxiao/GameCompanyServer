using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Word_Sever;

namespace Word_Sever.Storage
{
    /// <summary>
    /// 文件日志记录器 - 详细记录所有操作到文件
    /// </summary>
    internal class FileLogger
    {
        private readonly string _logFilePath;
        private readonly object _writeLock = new object();
        private readonly ConcurrentQueue<string> _logQueue;
        private readonly Thread _writerThread;
        private bool _isRunning;

        public FileLogger(string logDirectory, string logFileName)
        {
            // 确保日志目录存在
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            // 日志文件名包含日期时间
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileNameWithTime = $"{Path.GetFileNameWithoutExtension(logFileName)}_{timestamp}.txt";
            _logFilePath = Path.Combine(logDirectory, fileNameWithTime);

            _logQueue = new ConcurrentQueue<string>();
            _isRunning = true;

            // 启动后台写入线程
            _writerThread = new Thread(WriteLoop);
            _writerThread.IsBackground = true;
            _writerThread.Start();

            // 写入日志头
            LogInfo("========================================");
            LogInfo("GameCompany 服务端详细日志");
            LogInfo($"日志文件: {_logFilePath}");
            LogInfo($"开始时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo("========================================");
            LogInfo("");
        }

        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public void LogWarning(string message)
        {
            Log("WARN", message);
        }

        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        private void Log(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = $"[{timestamp}] [{level}] {message}";
            _logQueue.Enqueue(logLine);
        }

        private void WriteLoop()
        {
            try
            {
                while (_isRunning || !_logQueue.IsEmpty)
                {
                    if (_logQueue.TryDequeue(out string logLine))
                    {
                        lock (_writeLock)
                        {
                            File.AppendAllText(_logFilePath, logLine + Environment.NewLine, Encoding.UTF8);
                        }
                    }
                    else
                    {
                        Thread.Sleep(10);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileLogger] 写入异常: {ex.Message}");
            }
        }

        public void Close()
        {
            LogInfo("");
            LogInfo("========================================");
            LogInfo($"结束时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo("========================================");

            _isRunning = false;
            if (_writerThread != null && _writerThread.IsAlive)
            {
                _writerThread.Join(1000);
            }
        }

        public string GetLogFilePath()
        {
            return _logFilePath;
        }
    }

    /// <summary>
    /// 保存模式枚举
    /// </summary>
    public enum SaveMode
    {
        /// <summary>
        /// 立即保存 - 每次客户端请求都立即保存到磁盘
        /// 优点: 数据安全，不会丢失
        /// 缺点: 频繁IO，性能较低
        /// </summary>
        Instant,

        /// <summary>
        /// 定时保存 - 按照指定间隔保存到磁盘
        /// 优点: 性能好，减少IO
        /// 缺点: 可能丢失部分数据（最多丢失一个间隔的数据）
        /// </summary>
        Interval
    }

    /// <summary>
    /// 本地JSON文件存储系统 - 替代Redis
    /// 支持键值对存储和Hash表操作
    /// 所有数据优先在内存中操作，根据模式定时或立即保存到磁盘
    /// </summary>
    public class LocalDataStorage
    {
        // 单键存储 - Dictionary<key, value> (内存中)
        private ConcurrentDictionary<string, string> _stringData;

        // Hash表存储 - Dictionary<key, Dictionary<field, value>> (内存中)
        private ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _hashData;

        private readonly string _dataDirectory;
        private readonly string _stringDataFile;
        private readonly string _hashDataFile;

        private DateTime _lastSaveTime;
        private readonly int _saveIntervalHours;
        private readonly SaveMode _saveMode;

        private readonly object _saveLock = new object();
        private bool _isDirty = false; // 标记数据是否被修改
        private FileLogger _fileLogger; // 文件日志记录器

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dataDirectory">数据目录</param>
        /// <param name="saveMode">保存模式：Instant立即保存 / Interval定时保存</param>
        /// <param name="saveIntervalHours">定时保存间隔（小时），仅在Interval模式下有效</param>
        public LocalDataStorage(string dataDirectory, SaveMode saveMode = SaveMode.Interval, int saveIntervalHours = 24)
        {
            _dataDirectory = dataDirectory;
            _stringDataFile = Path.Combine(dataDirectory, "string_data.json");
            _hashDataFile = Path.Combine(dataDirectory, "hash_data.json");
            _saveMode = saveMode;
            _saveIntervalHours = saveIntervalHours;
            _lastSaveTime = DateTime.Now;

            _stringData = new ConcurrentDictionary<string, string>();
            _hashData = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

            // 确保目录存在
            if (!Directory.Exists(dataDirectory))
            {
                Directory.CreateDirectory(dataDirectory);
            }

            // 初始化文件日志记录器
            _fileLogger = new FileLogger("Logs", "GameServer_Detail");
            _fileLogger.LogInfo($"LocalDataStorage 初始化开始");
            _fileLogger.LogInfo($"数据目录: {_dataDirectory}");
            _fileLogger.LogInfo($"保存模式: {_saveMode}");
            _fileLogger.LogInfo($"保存间隔: {_saveIntervalHours}小时");

            // 启动时从磁盘加载数据到内存
            LoadFromDisk();

            // 初始化默认值（如果不存在）
            InitializeDefaultValues();

            // 输出当前保存模式和时间信息
            string modeDesc = _saveMode == SaveMode.Instant
                ? "立即保存模式（每次请求都保存）"
                : $"定时保存模式（每{_saveIntervalHours}小时保存一次）";
            // 表格形式显示存储配置
            var configTable = new ConsoleTableFormatter();
            configTable.AddColumn("配置项", "值");
            configTable.SetHeaderColor(ConsoleColor.Cyan);
            configTable.AddRow("保存模式", modeDesc);
            configTable.AddRow("保存间隔", $"{_saveIntervalHours}小时");
            configTable.AddRow("数据目录", _dataDirectory);
            configTable.AddRow("String文件", _stringDataFile);
            configTable.AddRow("Hash文件", _hashDataFile);
            configTable.Render();
        }

        #region 单键操作 (String Operations)

        /// <summary>
        /// 设置键值对（立即保存到内存，根据模式决定是否立即写入磁盘）
        /// </summary>
        public void StringSet(string key, string value)
        {
            string logMessage = $"StringSet 调用 - Key: {key}, Value长度: {value?.Length ?? 0}";
            LogInfo($"[LocalStorage] {logMessage}");
            _fileLogger?.LogInfo($"[LocalStorage] {logMessage}");
            _fileLogger?.LogInfo($"  Value内容: {(value?.Length > 200 ? value.Substring(0, 200) + "..." : value)}");

            // 1. 立即保存到内存
            _stringData[key] = value;
            _isDirty = true; // 标记数据已修改

            logMessage = $"StringSet 写入内存成功 - Key: {key}, 内存中共有 {_stringData.Count} 个String键";
            LogInfo($"[LocalStorage] {logMessage}");
            _fileLogger?.LogInfo($"[LocalStorage] {logMessage}");

            // 2. 如果是立即保存模式，立即写入磁盘
            if (_saveMode == SaveMode.Instant)
            {
                logMessage = "立即保存模式 - 准备调用 SaveToDisk()";
                _fileLogger?.LogInfo($"[LocalStorage] {logMessage}");
                bool saveResult = SaveToDisk();
                logMessage = $"立即保存模式 - SaveToDisk() 返回: {saveResult}";
                _fileLogger?.LogInfo($"[LocalStorage] {logMessage}");
            }
            else
            {
                logMessage = "定时保存模式 - 已设置脏标记，等待定时保存";
                LogInfo($"[LocalStorage] {logMessage}");
                _fileLogger?.LogInfo($"[LocalStorage] {logMessage}");
            }
        }

        /// <summary>
        /// 获取键的值（优先从内存中读取）
        /// </summary>
        public string StringGet(string key)
        {
            // 直接从内存中读取，不访问磁盘
            return _stringData.TryGetValue(key, out var value) ? value : null;
        }

        /// <summary>
        /// 检查键是否存在
        /// </summary>
        public bool KeyExists(string key)
        {
            return _stringData.ContainsKey(key) || _hashData.ContainsKey(key);
        }

        /// <summary>
        /// 删除键
        /// </summary>
        public bool KeyDelete(string key)
        {
            bool result1 = _stringData.TryRemove(key, out _);
            bool result2 = _hashData.TryRemove(key, out _);
            return result1 || result2;
        }

        #endregion

        #region Hash操作 (Hash Operations)

        /// <summary>
        /// 设置Hash表的字段值（立即保存到内存，根据模式决定是否立即写入磁盘）
        /// 返回值表示本次调用是否完成预期写入（立即保存模式下包含落盘结果）
        /// </summary>
        public bool HashSet(string key, string field, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(field))
            {
                string invalidMsg = $"[LocalStorage] HashSet 参数非法 - Key: {key ?? "(null)"}, Field: {field ?? "(null)"}";
                LogError(invalidMsg);
                _fileLogger?.LogError(invalidMsg);
                return false;
            }

            string normalizedValue = value ?? string.Empty;
            if (value == null)
            {
                string nullValueMsg = $"[LocalStorage] HashSet 收到 null 值，已转换为空字符串 - Key: {key}, Field: {field}";
                LogWarning(nullValueMsg);
                _fileLogger?.LogWarning(nullValueMsg);
            }

            string logMsg = $"[LocalStorage] HashSet 调用 - Key: {key}, Field: {field}, Value长度: {normalizedValue.Length}";
            _fileLogger?.LogInfo(logMsg);
            if (!string.IsNullOrEmpty(normalizedValue))
            {
                string preview = normalizedValue.Length > 120 ? normalizedValue.Substring(0, 120) + "..." : normalizedValue;
                _fileLogger?.LogInfo($"[LocalStorage] HashSet Value预览: {preview}");
            }

            // 1. 立即保存到内存
            var hash = _hashData.GetOrAdd(key, k => new ConcurrentDictionary<string, string>());
            hash[field] = normalizedValue;
            _isDirty = true; // 标记数据已修改

            int fieldCount = hash.Count;
            logMsg = $"[LocalStorage] HashSet 写入内存成功 - Key: {key}, Field: {field}, 该Hash表有 {fieldCount} 个字段, 内存中共有 {_hashData.Count} 个Hash表";
            _fileLogger?.LogInfo(logMsg);

            bool saveResult = true;
            bool saveTriggered = _saveMode == SaveMode.Instant;

            // 2. 如果是立即保存模式，立即写入磁盘
            if (_saveMode == SaveMode.Instant)
            {
                logMsg = "[LocalStorage] 立即保存模式 - 准备调用 SaveToDisk()";
                _fileLogger?.LogInfo(logMsg);
                saveResult = SaveToDisk();
                logMsg = $"[LocalStorage] 立即保存模式 - SaveToDisk() 返回: {saveResult}";
                _fileLogger?.LogInfo(logMsg);
            }
            else
            {
                logMsg = "[LocalStorage] 定时保存模式 - 已设置脏标记，等待定时保存";
                LogInfo(logMsg);
                _fileLogger?.LogInfo(logMsg);
            }

            string saveToDiskResult = saveTriggered ? saveResult.ToString() : "NotTriggered(IntervalMode)";
            logMsg = $"[LocalStorage] HashSet调用参数和返回值 - Key: {key}, Field: {field}, Value长度: {normalizedValue.Length}, SaveMode: {_saveMode}, SaveTriggered: {saveTriggered}, SaveToDiskResult: {saveToDiskResult}, Success: {saveResult}";
            _fileLogger?.LogInfo(logMsg);
            return saveResult;
        }

        /// <summary>
        /// 获取Hash表的字段值（优先从内存中读取）
        /// </summary>
        public string HashGet(string key, string field)
        {

            // 直接从内存中读取，不访问磁盘
            if (_hashData.TryGetValue(key, out var hash))
            {
                if (hash.TryGetValue(field, out var value))
                {
                    return value;
                }
                return null;
            }
            return null;
        }

        /// <summary>
        /// 获取Hash表的所有字段和值
        /// </summary>
        public Dictionary<string, string> HashGetAll(string key)
        {
            if (_hashData.TryGetValue(key, out var hash))
            {
                return new Dictionary<string, string>(hash);
            }
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// 删除Hash表的字段
        /// </summary>
        public bool HashDelete(string key, string field)
        {
            if (_hashData.TryGetValue(key, out var hash))
            {
                return hash.TryRemove(field, out _);
            }
            return false;
        }

        /// <summary>
        /// 检查Hash表的字段是否存在
        /// </summary>
        public bool HashExists(string key, string field)
        {
            if (_hashData.TryGetValue(key, out var hash))
            {
                return hash.ContainsKey(field);
            }
            return false;
        }

        /// <summary>
        /// 校验指定Hash字段是否已落盘到hash_data.json
        /// </summary>
        public bool TryVerifyHashPersisted(string key, string field, out string persistedValue, out string verifyDetail)
        {
            persistedValue = null;
            verifyDetail = string.Empty;

            try
            {
                if (!File.Exists(_hashDataFile))
                {
                    verifyDetail = $"Hash文件不存在: {_hashDataFile}";
                    LogWarning($"[LocalStorage] {verifyDetail}");
                    _fileLogger?.LogWarning($"[LocalStorage] {verifyDetail}");
                    return false;
                }

                string json = File.ReadAllText(_hashDataFile, Encoding.UTF8);
                JObject root = JsonConvert.DeserializeObject<JObject>(json);
                if (root == null)
                {
                    verifyDetail = "Hash文件反序列化失败: root为null";
                    LogError($"[LocalStorage] {verifyDetail}");
                    _fileLogger?.LogError($"[LocalStorage] {verifyDetail}");
                    return false;
                }

                if (!root.TryGetValue(key, out JToken hashToken))
                {
                    verifyDetail = $"Hash文件中未找到Key: {key}";
                    LogWarning($"[LocalStorage] {verifyDetail}");
                    _fileLogger?.LogWarning($"[LocalStorage] {verifyDetail}");
                    return false;
                }

                if (hashToken.Type != JTokenType.Object)
                {
                    verifyDetail = $"Hash文件中Key对应值不是对象: {key}, TokenType: {hashToken.Type}";
                    LogWarning($"[LocalStorage] {verifyDetail}");
                    _fileLogger?.LogWarning($"[LocalStorage] {verifyDetail}");
                    return false;
                }

                JObject hashObject = (JObject)hashToken;
                if (!hashObject.TryGetValue(field, out JToken fieldToken))
                {
                    verifyDetail = $"Hash文件中未找到Field: {key}.{field}";
                    LogWarning($"[LocalStorage] {verifyDetail}");
                    _fileLogger?.LogWarning($"[LocalStorage] {verifyDetail}");
                    return false;
                }

                if (fieldToken.Type == JTokenType.Null || fieldToken.Type == JTokenType.Undefined)
                {
                    persistedValue = string.Empty;
                }
                else if (fieldToken.Type == JTokenType.String)
                {
                    persistedValue = fieldToken.Value<string>();
                }
                else
                {
                    persistedValue = fieldToken.ToString(Formatting.None);
                }

                string persistedPreview = string.IsNullOrEmpty(persistedValue)
                    ? "(空)"
                    : (persistedValue.Length > 120 ? persistedValue.Substring(0, 120) + "..." : persistedValue);
                verifyDetail = $"Hash文件校验成功 - Key: {key}, Field: {field}, Value长度: {persistedValue?.Length ?? 0}, Value预览: {persistedPreview}, 文件: {_hashDataFile}";
                _fileLogger?.LogInfo($"[LocalStorage] {verifyDetail}");
                return true;
            }
            catch (Exception ex)
            {
                verifyDetail = $"Hash文件校验异常: {ex.Message}";
                LogError($"[LocalStorage] {verifyDetail}");
                _fileLogger?.LogError($"[LocalStorage] {verifyDetail}");
                return false;
            }
        }

        #endregion

        #region 持久化操作 (Persistence)

        /// <summary>
        /// 从磁盘加载数据
        /// </summary>
        private void LoadFromDisk()
        {
            try
            {
                string logMsg;

                // 加载String数据（使用UTF-8编码）
                if (File.Exists(_stringDataFile))
                {
                    var json = File.ReadAllText(_stringDataFile, Encoding.UTF8);
                    var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (data != null)
                    {
                        _stringData = new ConcurrentDictionary<string, string>(data);
                        logMsg = $"[LocalStorage] 加载String数据成功: {_stringData.Count} 条记录";
                        LogInfo(logMsg);
                        _fileLogger?.LogInfo(logMsg);

                        
                    }
                }
                else
                {
                    logMsg = "[LocalStorage] String数据文件不存在，创建新的数据";
                    LogInfo(logMsg);
                    _fileLogger?.LogInfo(logMsg);
                }

                // 加载Hash数据（使用UTF-8编码）
                if (File.Exists(_hashDataFile))
                {
                    var json = File.ReadAllText(_hashDataFile, Encoding.UTF8);
                    logMsg = $"[LocalStorage] Hash文件读取成功，JSON长度: {json.Length}字符";
                    LogInfo(logMsg);
                    _fileLogger?.LogInfo(logMsg);

                    var root = JsonConvert.DeserializeObject<JObject>(json);
                    _hashData = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();

                    if (root != null)
                    {
                        logMsg = $"[LocalStorage] JSON反序列化成功，发现 {root.Count} 个顶层键";
                        _fileLogger?.LogInfo(logMsg);

                        int skippedCount = 0;
                        int loadedBlockFieldCount = 0;
                        var loadedBlockOwners = new List<string>();

                        foreach (var hashProperty in root.Properties())
                        {
                            if (hashProperty.Value.Type != JTokenType.Object)
                            {
                                skippedCount++;
                                logMsg = $"[LocalStorage]   [跳过] 顶层键: {hashProperty.Name}, 原因: 类型为 {hashProperty.Value.Type}（不是Hash对象）";
                                _fileLogger?.LogWarning(logMsg);
                                continue;
                            }

                            var fieldDict = new ConcurrentDictionary<string, string>();
                            bool hasBlockField = false;
                            int blockLength = 0;
                            string blockPreview = string.Empty;
                            foreach (var fieldProperty in ((JObject)hashProperty.Value).Properties())
                            {
                                string fieldValue;
                                if (fieldProperty.Value.Type == JTokenType.Null || fieldProperty.Value.Type == JTokenType.Undefined)
                                {
                                    fieldValue = string.Empty;
                                }
                                else if (fieldProperty.Value.Type == JTokenType.String)
                                {
                                    fieldValue = fieldProperty.Value.Value<string>();
                                }
                                else
                                {
                                    fieldValue = fieldProperty.Value.ToString(Formatting.None);
                                    logMsg = $"[LocalStorage]   [转换] Hash表: {hashProperty.Name}, 字段: {fieldProperty.Name}, 类型: {fieldProperty.Value.Type} -> JSON字符串";
                                    _fileLogger?.LogWarning(logMsg);
                                }

                                fieldDict[fieldProperty.Name] = fieldValue;
                                if (string.Equals(fieldProperty.Name, "block", StringComparison.OrdinalIgnoreCase))
                                {
                                    hasBlockField = true;
                                    blockLength = fieldValue?.Length ?? 0;
                                    blockPreview = string.IsNullOrEmpty(fieldValue)
                                        ? "(空)"
                                        : (fieldValue.Length > 120 ? fieldValue.Substring(0, 120) + "..." : fieldValue);
                                }
                            }

                            _hashData[hashProperty.Name] = fieldDict;
                            logMsg = $"[LocalStorage]   [已加载] Hash表: {hashProperty.Name}, 字段数: {fieldDict.Count}, 包含block: {hasBlockField}";
                            _fileLogger?.LogInfo(logMsg);

                            if (hasBlockField)
                            {
                                loadedBlockFieldCount++;
                                if (loadedBlockOwners.Count < 5)
                                {
                                    loadedBlockOwners.Add($"{hashProperty.Name}(Length={blockLength})");
                                }

                                logMsg = $"[LocalStorage]   [建筑字段] Hash表: {hashProperty.Name}, block长度: {blockLength}, block预览: {blockPreview}";
                                _fileLogger?.LogInfo(logMsg);
                            }
                        }

                        // 表格形式的数据加载统计
                        var loadTable = new ConsoleTableFormatter();
                        loadTable.AddColumn("统计项", "Hash表", "顶层键", "跳过", "含block", "时间");
                        loadTable.SetHeaderColor(ConsoleColor.Green);
                        loadTable.AddRow("数据加载", _hashData.Count, root.Count, skippedCount, loadedBlockFieldCount, DateTime.Now.ToString("HH:mm:ss"));
                        loadTable.Render();

                        logMsg = $"[LocalStorage] 加载Hash数据成功: {_hashData.Count} 个Hash表（JSON顶层键: {root.Count}, 跳过: {skippedCount}）";
                        _fileLogger?.LogInfo(logMsg);
                        string loadedBlockOwnersPreview = loadedBlockOwners.Count > 0 ? string.Join(", ", loadedBlockOwners) : "(无)";
                        logMsg = $"[LocalStorage] Hash加载建筑统计 - 含block字段Hash表: {loadedBlockFieldCount}, 样例: {loadedBlockOwnersPreview}";
                        _fileLogger?.LogInfo(logMsg);

                        
                    }
                    else
                    {
                        logMsg = "[LocalStorage] JSON反序列化失败: root为null";
                        LogError(logMsg);
                        _fileLogger?.LogError(logMsg);
                    }
                }
                else
                {
                    logMsg = "[LocalStorage] Hash数据文件不存在，创建新的数据";
                    LogInfo(logMsg);
                    _fileLogger?.LogInfo(logMsg);
                }
            }
            catch (Exception ex)
            {
                string errMsg = $"[LocalStorage] 加载数据失败: {ex.Message}";
                LogError(errMsg);
                _fileLogger?.LogError(errMsg);
            }
        }

        /// <summary>
        /// 保存数据到磁盘
        /// </summary>
        public bool SaveToDisk()
        {
            _fileLogger?.LogInfo("[LocalStorage] ====================================");
            _fileLogger?.LogInfo("[LocalStorage] SaveToDisk 开始");
            _fileLogger?.LogInfo($"[LocalStorage]   脏标记: {_isDirty}");
            _fileLogger?.LogInfo($"[LocalStorage]   保存模式: {_saveMode}");
            _fileLogger?.LogInfo($"[LocalStorage]   String键数: {_stringData.Count}");
            _fileLogger?.LogInfo($"[LocalStorage]   Hash表数: {_hashData.Count}");

            lock (_saveLock)
            {
                try
                {

                    // JSON序列化设置：保持中文字符不转义
                    var jsonSettings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        StringEscapeHandling = StringEscapeHandling.Default
                    };

                    // 保存String数据（使用UTF-8编码，不带BOM）
                    var stringJson = JsonConvert.SerializeObject(_stringData, jsonSettings);
                    File.WriteAllText(_stringDataFile, stringJson, new UTF8Encoding(false));

                    // 验证文件是否真的被写入
                    if (!File.Exists(_stringDataFile))
                    {
                        LogError($"[LocalStorage] String文件写入后不存在！");
                    }

                    // 保存Hash数据（使用UTF-8编码，不带BOM）
                    var hashDict = new Dictionary<string, Dictionary<string, string>>();
                    int blockFieldCount = 0;
                    var blockOwners = new List<string>();
                    foreach (var kvp in _hashData)
                    {
                        hashDict[kvp.Key] = new Dictionary<string, string>(kvp.Value);

                        if (kvp.Value.TryGetValue("block", out var blockValue))
                        {
                            blockFieldCount++;
                            if (blockOwners.Count < 5)
                            {
                                int blockLength = blockValue?.Length ?? 0;
                                blockOwners.Add($"{kvp.Key}(Length={blockLength})");
                            }
                        }
                    }
                    string blockOwnersPreview = blockOwners.Count > 0 ? string.Join(", ", blockOwners) : "(无)";
                    _fileLogger?.LogInfo($"[LocalStorage] 建筑数据统计 - 含block字段的Hash表数量: {blockFieldCount}, 样例: {blockOwnersPreview}");
                    var hashJson = JsonConvert.SerializeObject(hashDict, jsonSettings);

                    File.WriteAllText(_hashDataFile, hashJson, new UTF8Encoding(false));

                    // 验证文件是否真的被写入
                    if (File.Exists(_hashDataFile))
                    {
                        var fileInfo = new FileInfo(_hashDataFile);
                    }
                    else
                    {
                        LogError($"[LocalStorage] Hash文件写入后不存在！");
                    }

                    _lastSaveTime = DateTime.Now;
                    _isDirty = false; // 清除脏标记

                    _fileLogger?.LogInfo($"[LocalStorage]   >>> 数据保存成功 <<<");
                    _fileLogger?.LogInfo($"[LocalStorage]   更新保存时间: {_lastSaveTime}");
                    _fileLogger?.LogInfo("[LocalStorage] ====================================");
                    return true;
                }
                catch (Exception ex)
                {
                    LogError($"[LocalStorage] !!! 保存数据失败 !!!");
                    LogError($"[LocalStorage] 异常类型: {ex.GetType().Name}");
                    LogError($"[LocalStorage] 异常消息: {ex.Message}");
                    LogError($"[LocalStorage] 堆栈跟踪: {ex.StackTrace}");

                    _fileLogger?.LogError($"[LocalStorage] !!! 保存数据失败 !!!");
                    _fileLogger?.LogError($"[LocalStorage]   异常类型: {ex.GetType().Name}");
                    _fileLogger?.LogError($"[LocalStorage]   异常消息: {ex.Message}");
                    _fileLogger?.LogError($"[LocalStorage]   堆栈跟踪: {ex.StackTrace}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 检查是否需要自动保存（仅在Interval模式下有效）
        /// 如果数据被修改，按照指定间隔保存
        /// 如果数据未修改，不保存
        /// </summary>
        public void CheckAutoSave()
        {
            // 立即保存模式不需要检查，因为已经在Set时立即保存了
            if (_saveMode == SaveMode.Instant)
            {
                // 不打印日志，避免每帧都输出
                return;
            }

            var now = DateTime.Now;
            var timeSinceLastSave = now - _lastSaveTime;

            // 数据未修改，不需要保存
            if (!_isDirty)
            {
                // 每10秒打印一次状态，避免日志过多
                if (timeSinceLastSave.TotalSeconds >= 10 && (int)timeSinceLastSave.TotalSeconds % 10 == 0)
                {
                    LogInfo($"[LocalStorage] CheckAutoSave - 数据未修改，不需要保存 (距上次保存: {timeSinceLastSave.TotalHours:F2}小时)");
                }
                return;
            }

            // 打印详细的时间调试信息
            LogInfo($"[LocalStorage] --- CheckAutoSave 时间检查 ---");
            LogInfo($"[LocalStorage] 当前时间: {now}");
            LogInfo($"[LocalStorage] 上次保存: {_lastSaveTime}");
            LogInfo($"[LocalStorage] 距上次保存: {timeSinceLastSave.TotalHours:F4}小时 ({timeSinceLastSave.TotalMinutes:F2}分钟, {timeSinceLastSave.TotalSeconds:F1}秒)");
            LogInfo($"[LocalStorage] 保存间隔配置: {_saveIntervalHours}小时");
            LogInfo($"[LocalStorage] 脏标记: {_isDirty}");
            LogInfo($"[LocalStorage] 是否需要保存: {timeSinceLastSave.TotalHours >= _saveIntervalHours}");

            // 按照配置的间隔保存（小时）
            if (timeSinceLastSave.TotalHours >= _saveIntervalHours)
            {
                bool saveResult = SaveToDisk();
            }
        }

        /// <summary>
        /// 强制立即保存
        /// </summary>
        public void ForceSave()
        {
            _fileLogger?.LogInfo("[LocalStorage] 强制保存数据 - 服务器关闭");
            bool saveResult = SaveToDisk();
            _fileLogger?.LogInfo($"[LocalStorage] 强制保存执行完成, SaveToDisk返回: {saveResult}");

            // 关闭文件日志
            if (_fileLogger != null)
            {
                _fileLogger.Close();
                string logPath = _fileLogger.GetLogFilePath();
                LogInfo($"[LocalStorage] 详细日志已保存到: {logPath}");
            }
        }

        #endregion

        #region 默认值初始化

        /// <summary>
        /// 初始化系统默认值
        /// </summary>
        private void InitializeDefaultValues()
        {
            try
            {
                // 初始化利率（如果不存在）- 默认5%
                if (StringGet("money") == null)
                {
                    StringSet("money", "0.05");
                    LogInfo("[LocalStorage] 初始化默认利率: 0.05 (5%)");
                }

                // 可以在这里添加更多默认值初始化
                // 例如：股票列表、游戏配置等
            }
            catch (Exception ex)
            {
                LogError($"[LocalStorage] 初始化默认值失败: {ex.Message}");
            }
        }

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取String键数量
        /// </summary>
        public int GetStringKeyCount()
        {
            return _stringData.Count;
        }

        /// <summary>
        /// 获取Hash表数量
        /// </summary>
        public int GetHashKeyCount()
        {
            return _hashData.Count;
        }

        /// <summary>
        /// 获取所有String键
        /// </summary>
        public List<string> GetAllStringKeys()
        {
            return new List<string>(_stringData.Keys);
        }

        /// <summary>
        /// 获取所有Hash键
        /// </summary>
        public List<string> GetAllHashKeys()
        {
            return new List<string>(_hashData.Keys);
        }

        /// <summary>
        /// 获取当前保存模式
        /// </summary>
        public SaveMode GetSaveMode()
        {
            return _saveMode;
        }

        /// <summary>
        /// 获取保存间隔（小时）
        /// </summary>
        public int GetSaveIntervalHours()
        {
            return _saveIntervalHours;
        }

        /// <summary>
        /// 获取Hash持久化文件路径
        /// </summary>
        public string GetHashDataFilePath()
        {
            return _hashDataFile;
        }

        /// <summary>
        /// 获取Hash持久化文件状态（用于定位建筑数据是否实际写入磁盘）
        /// </summary>
        public string GetHashDataFileStatus()
        {
            try
            {
                if (!File.Exists(_hashDataFile))
                {
                    return $"Exists: false, Path: {_hashDataFile}";
                }

                var fileInfo = new FileInfo(_hashDataFile);
                return $"Exists: true, Size: {fileInfo.Length} bytes, LastWrite: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}, Path: {_hashDataFile}";
            }
            catch (Exception ex)
            {
                return $"ReadStatusError: {ex.Message}, Path: {_hashDataFile}";
            }
        }

        /// <summary>
        /// 获取持久化调试信息（用于Set/Get链路日志排查）
        /// </summary>
        public string GetPersistenceDebugInfo()
        {
            string hashFileStatus = GetHashDataFileStatus();
            return $"SaveMode: {_saveMode}, IsDirty: {_isDirty}, LastSave: {_lastSaveTime:yyyy-MM-dd HH:mm:ss}, SaveIntervalHours: {_saveIntervalHours}, HashFileStatus: [{hashFileStatus}]";
        }

        #endregion

        #region 日志方法

        private void LogInfo(string message)
        {
            if (OpenProgress.Instance?.log != null)
            {
                OpenProgress.Instance.log.LogGreen(message);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                Console.ResetColor();
            }
        }

        private void LogWarning(string message)
        {
            if (OpenProgress.Instance?.log != null)
            {
                OpenProgress.Instance.log.LogYellow(message);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                Console.ResetColor();
            }
        }

        private void LogError(string message)
        {
            if (OpenProgress.Instance?.log != null)
            {
                OpenProgress.Instance.log.LogError(message);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                Console.ResetColor();
            }
        }

        #endregion
    }
}

