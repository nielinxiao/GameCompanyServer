using System;
using System.IO;
using System.Text;
using System.Collections.Concurrent;
using System.Threading;

namespace Word_Sever.Storage
{
    /// <summary>
    /// 文件日志记录器 - 详细记录所有操作到文件
    /// </summary>
    public class FileLogger
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

        /// <summary>
        /// 记录普通信息
        /// </summary>
        public void LogInfo(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// 记录警告信息
        /// </summary>
        public void LogWarning(string message)
        {
            Log("WARN", message);
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        public void LogError(string message)
        {
            Log("ERROR", message);
        }

        /// <summary>
        /// 记录日志
        /// </summary>
        private void Log(string level, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logLine = $"[{timestamp}] [{level}] {message}";
            _logQueue.Enqueue(logLine);
        }

        /// <summary>
        /// 后台写入循环
        /// </summary>
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
                        Thread.Sleep(10); // 等待新日志
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FileLogger] 写入异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 关闭日志记录器
        /// </summary>
        public void Close()
        {
            LogInfo("");
            LogInfo("========================================");
            LogInfo($"结束时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            LogInfo("========================================");

            _isRunning = false;
            if (_writerThread != null && _writerThread.IsAlive)
            {
                _writerThread.Join(1000); // 等待最多1秒
            }
        }

        /// <summary>
        /// 获取日志文件路径
        /// </summary>
        public string GetLogFilePath()
        {
            return _logFilePath;
        }
    }
}
