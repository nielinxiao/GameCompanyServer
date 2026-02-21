using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Word_Sever
{
    /// <summary>
    /// 会话管理器 - 基于 Token 的会话认证
    /// </summary>
    public class SessionManager
    {
        private ConcurrentDictionary<string, PlayerSession> _sessions;

        public class PlayerSession
        {
            public string PlayerId { get; set; }
            public string PlayerName { get; set; }
            public string CompanyName { get; set; }
            public string Token { get; set; }
            public DateTime LastActivity { get; set; }
        }

        public SessionManager()
        {
            _sessions = new ConcurrentDictionary<string, PlayerSession>();
        }

        /// <summary>
        /// 创建新会话并返回 Token
        /// </summary>
        public string CreateSession(string playerId, string playerName, string companyName)
        {
            var token = GenerateToken(playerId);
            var session = new PlayerSession
            {
                PlayerId = playerId,
                PlayerName = playerName,
                CompanyName = companyName,
                Token = token,
                LastActivity = DateTime.Now
            };

            _sessions[playerId] = session;
            // 静默创建会话，避免日志刷屏
            return token;
        }

        /// <summary>
        /// 验证 Token 是否有效
        /// </summary>
        public bool ValidateToken(string playerId, string token)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(playerId))
                return false;

            if (!_sessions.TryGetValue(playerId, out var session))
            {
                return false;
            }

            if (session.Token != token)
            {
                return false;
            }

            // 检查会话是否过期（30分钟）
            if ((DateTime.Now - session.LastActivity).TotalMinutes > 30)
            {
                _sessions.TryRemove(playerId, out _);
                return false;
            }

            // 更新最后活动时间
            session.LastActivity = DateTime.Now;
            return true;
        }

        /// <summary>
        /// 移除会话
        /// </summary>
        public void RemoveSession(string playerId)
        {
            _sessions.TryRemove(playerId, out _);
            // 静默移除会话
        }

        /// <summary>
        /// 获取会话信息
        /// </summary>
        public PlayerSession GetSession(string playerId)
        {
            _sessions.TryGetValue(playerId, out var session);
            return session;
        }

        /// <summary>
        /// 获取所有在线玩家ID
        /// </summary>
        public string[] GetOnlinePlayers()
        {
            return _sessions.Keys.ToArray();
        }

        /// <summary>
        /// 获取在线玩家数量
        /// </summary>
        public int GetOnlineCount()
        {
            return _sessions.Count;
        }

        /// <summary>
        /// 生成安全的 Token
        /// </summary>
        private string GenerateToken(string playerId)
        {
            using (var sha256 = SHA256.Create())
            {
                var input = $"{playerId}_{DateTime.Now.Ticks}_{Guid.NewGuid()}";
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hash);
            }
        }
    }
}
