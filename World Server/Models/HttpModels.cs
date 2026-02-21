using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Word_Sever
{
    /// <summary>
    /// HTTP 请求统一格式
    /// </summary>
    public class HttpRequest
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("playerName")]
        public string PlayerName { get; set; }

        [JsonProperty("companyName")]
        public string CompanyName { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; }
    }

    /// <summary>
    /// HTTP 响应统一格式
    /// </summary>
    public class HttpResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public JObject Data { get; set; }
    }

    /// <summary>
    /// GetJson 操作的返回结果
    /// </summary>
    public class GetJsonResult
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; }
        public object Data { get; set; }
    }

    /// <summary>
    /// 世界聊天消息
    /// </summary>
    public class WorldChatMessage
    {
        [JsonProperty("playerId")]
        public string PlayerId { get; set; }

        [JsonProperty("playerName")]
        public string PlayerName { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("timestamp")]
        public System.DateTime Timestamp { get; set; }
    }
}
