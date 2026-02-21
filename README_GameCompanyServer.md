# GameCompany 游戏服务器使用指南

## 项目概述

本项目是基于World框架的GameCompany游戏服务器，使用本地JSON文件存储替代Redis，实现了完整的游戏后端功能。

---

## 核心特性

### ✅ 网络通信
- **协议**: TCP + IOCP异步模式
- **端口**: 45677
- **序列化**: Protobuf
- **缓冲区**: 2048字节
- **粘包处理**: 4字节长度头（Big-Endian）

### ✅ 数据存储
- **存储方式**: 本地JSON文件
- **数据目录**: `Data/`
- **自动保存**: 每24小时自动保存一次
- **支持操作**:
  - String键值对: `StringSet/StringGet`
  - Hash表: `HashSet/HashGet`

### ✅ 业务功能
实现了11种客户端命令：
1. **Join** - 玩家加入游戏
2. **Remove** - 玩家离开游戏
3. **GetJson** - 获取JSON数据（支持单键和Hash）
4. **SetJson** - 设置JSON数据（支持单键和Hash）
5. **GetStock** - 获取股票信息
6. **BuyStock** - 购买股票
7. **SellStock** - 出售股票
8. **SearchStock** - 搜索股票
9. **Message** - 聊天消息广播
10. **Donat** - 捐赠功能
11. **CheckPlayerCreatByFirst** - 首次创建检查

---

## 项目结构

```
GameCompanyServer/
├── Data/                           # 数据存储目录
│   ├── string_data.json           # String键值对数据
│   └── hash_data.json             # Hash表数据
├── IOCP/                          # IOCP网络框架
│   ├── IOCP.cs                    # 核心IOCP实现
│   └── IOCP.csproj
├── proto/                         # Protobuf消息定义
│   └── CompanyMessage.cs          # 游戏消息协议
├── World Server/                  # 服务器主体
│   ├── Storage/
│   │   └── LocalDataStorage.cs   # 本地JSON存储系统
│   ├── Servc/
│   │   ├── GameServer.cs          # 游戏服务器核心（29KB）
│   │   └── GameService.cs         # 服务接口实现
│   ├── OpenProgress.cs            # 主循环管理
│   └── PERoot.cs                  # 程序入口
└── README_GameCompanyServer.md    # 本文档
```

---

## 快速开始

### 1. 安装依赖

项目需要以下NuGet包：

```bash
Install-Package protobuf-net -Version 3.2.0
Install-Package Newtonsoft.Json -Version 13.0.3
```

**不需要安装Redis相关包**，本项目使用本地JSON文件存储。

### 2. 编译项目

在Visual Studio中：
1. 打开解决方案文件（.sln）
2. 右键解决方案 → 还原NuGet程序包
3. 按 `Ctrl+Shift+B` 生成解决方案

### 3. 启动服务器

1. 运行项目（按 `F5`）
2. 查看控制台输出确认启动：
   ```
   [LocalStorage] 加载String数据成功: 0 条记录
   [LocalStorage] 加载Hash数据成功: 0 个Hash表
   [GameServer] 初始化完成，监听端口: 45677
   [GameService] 初始化完成
   ```

### 4. 连接测试

Unity客户端配置：
```csharp
// ConfigUtils.cs
public static class IOCP_Config
{
    public static string ip = "127.0.0.1";        // 本地测试
    // public static string ip = "49.233.248.132"; // 生产环境
    public static int port = 45677;
}
```

启动Unity客户端，自动连接到服务器。

---

## 数据存储说明

### 存储结构

#### String数据 (`string_data.json`)
```json
{
  "player:123:money": "10000.5",
  "player:123:level": "5",
  "config:version": "1.0.0"
}
```

#### Hash数据 (`hash_data.json`)
```json
{
  "company": {
    "user123": "{\"CompName\":\"我的公司\",\"Money\":50000}",
    "user456": "{\"CompName\":\"另一家公司\",\"Money\":30000}"
  },
  "player_stock": {
    "user123": "{\"AAPL\":100,\"GOOGL\":50}"
  }
}
```

### 客户端使用示例

#### 保存公司数据
```csharp
// Unity客户端代码
public void SaveCompany()
{
    string json = JsonConvert.SerializeObject(currentCompany);

    ClientMessage msg = new ClientMessage();
    msg.JsonKey = "company";           // Hash表名
    msg.JsonDoubleKey = UserID;        // Hash字段
    msg.JsonValue = json;              // 值

    MessageSend.Send(ClientCMD.SetJson, msg);
}
```

#### 获取公司数据
```csharp
public void GetCompany(Action callback)
{
    ClientMessage msg = new ClientMessage();
    msg.JsonKey = "company";
    msg.JsonDoubleKey = UserID;
    msg.JsonDicKey = UserID;           // 用于回调时识别

    ValueToken token = new ValueToken((pkg) => {
        string json = pkg.Body.serverMessage.JsonValue;
        currentCompany = JsonConvert.DeserializeObject<CompanyClass>(json);
        callback?.Invoke();
    });

    token.AddLisener(UserID, ServerCMD.ReturnJson);
    MessageSend.Send(ClientCMD.GetJson, msg);
}
```

---

## 自动保存机制

### 工作原理

1. **定时检查**: 每次`Tick()`调用时检查距上次保存的时间
2. **自动触发**: 超过24小时自动保存到磁盘
3. **强制保存**: 服务器关闭时强制保存所有数据

### 修改保存间隔

在 `GameService.cs` 中修改：
```csharp
public void Init()
{
    // 第二个参数是保存间隔（小时）
    _storage = new LocalDataStorage(dataPath, 24);  // 改为你需要的小时数
    // ...
}
```

### 手动触发保存

如需手动保存数据：
```csharp
OpenProgress.Instance.gameService.GetStorage().ForceSave();
```

---

## 服务器配置

### 修改监听端口

在 `GameService.cs` 中：
```csharp
public void Init()
{
    _gameServer = new GameServer(_storage);
    _gameServer.InitIOCPServer(
        OpenProgress.Instance.log.Log,
        1000,           // 最大连接数
        true,           // 是否允许超出最大连接数
        "0.0.0.0",      // 监听地址
        45677           // 端口号 - 在这里修改
    );
}
```

### 修改数据目录

在 `GameService.cs` 中：
```csharp
public void Init()
{
    string dataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "Data"  // 修改为你的目录名
    );
    // ...
}
```

---

## 命令详细说明

### 1. Join - 玩家加入
**客户端发送**:
```csharp
ClientMessage msg = new ClientMessage {
    Id = userId,
    Name = userName,
    companyName = companyName
};
MessageSend.Send(ClientCMD.Join, msg);
```

**服务端处理**:
- 添加到在线玩家列表
- 广播上线消息给所有玩家

### 2. GetJson/SetJson - 数据读写

**单键操作**:
```csharp
// 写入
SetJson: JsonKey = "player:money", JsonValue = "10000"

// 读取
GetJson: JsonKey = "player:money", JsonDicKey = "player:money"
```

**Hash操作**:
```csharp
// 写入Hash表
SetJson: JsonKey = "company", JsonDoubleKey = userId, JsonValue = json

// 读取Hash表
GetJson: JsonKey = "company", JsonDoubleKey = userId, JsonDicKey = userId
```

### 3. 股票系统

**购买股票**:
```csharp
ClientMessage msg = new ClientMessage {
    Id = userId,
    stockID = "AAPL",
    stockCompany = "Apple Inc",
    StockMuch = 100
};
MessageSend.Send(ClientCMD.BuyStock, msg);
```

**服务端数据结构**:
- 股票信息: `stock:{stockId}` → JSON
- 玩家持股: `player_stock:{userId}` → Hash {stockId: 数量}

---

## 性能优化建议

### 1. 调整保存频率
- 开发环境: 1小时保存一次（便于调试）
- 生产环境: 24小时保存一次（减少IO）

### 2. 消息队列大小
如果消息处理延迟，可以在`GameServer.cs`中调整：
```csharp
private const int MAX_MESSAGE_PER_TICK = 100;  // 每帧处理的最大消息数
```

### 3. 连接数限制
在`GameService.cs`的`Init()`中调整：
```csharp
_gameServer.InitIOCPServer(log, 1000, true, "0.0.0.0", 45677);
//                                ↑ 最大连接数
```

---

## 日志说明

### 日志级别
- **绿色**: 成功信息（连接、保存等）
- **黄色**: 警告信息（数据不存在等）
- **红色**: 错误信息（异常、失败等）
- **白色**: 一般信息

### 常见日志

```
[LocalStorage] 加载String数据成功: 123 条记录
[GameServer] 客户端连接: user123 (我的公司)
[GameServer] 处理命令 [Join] from user123
[LocalStorage] 触发定时保存 (距上次保存: 24.1小时)
[GameService] 在线玩家: 5, 存储: String=123, Hash=45
```

---

## 故障排查

### 问题1: 客户端连接失败
**检查**:
1. 端口是否被占用：`netstat -ano | findstr 45677`
2. 防火墙是否开放端口
3. 服务器是否正常启动

### 问题2: 数据丢失
**原因**:
- 服务器未正常关闭（强制结束进程）
- 距上次保存时间不足24小时

**解决**:
- 使用`Ctrl+C`正常关闭服务器
- 或缩短自动保存间隔

### 问题3: 性能下降
**可能原因**:
- 在线玩家过多
- 消息队列堆积
- 数据文件过大

**优化方案**:
- 增大`MAX_MESSAGE_PER_TICK`
- 定期清理无用数据
- 分表存储（修改LocalDataStorage）

---

## 扩展开发

### 添加新命令

1. 在`CompanyMessage.cs`中添加枚举：
```csharp
public enum ClientCMD
{
    // ... 现有命令
    NewCommand = 12,  // 新命令
}
```

2. 在`GameServer.cs`中添加处理方法：
```csharp
private void HandleNewCommand(IOCPToken<Pkg> client, ClientMessage msg)
{
    try
    {
        // 处理逻辑

        // 发送响应
        SendResponse(client, ServerCMD.NewResponse, ...);
    }
    catch (Exception ex)
    {
        LogError($"处理NewCommand失败: {ex.Message}");
    }
}
```

3. 在`DispatchMessage`中添加分发：
```csharp
case ClientCMD.NewCommand:
    _messageQueue.Enqueue(() => HandleNewCommand(client, msg));
    break;
```

---

## 技术支持

### 文档位置
- 客户端技术文档: `C:\Users\Nie\Desktop\GameCompany\ServerImplementationGuide.md`
- 服务端使用文档: 本文档

### 关键文件
- **网络层**: `IOCP/IOCP.cs`
- **消息定义**: `proto/CompanyMessage.cs`
- **存储层**: `World Server/Storage/LocalDataStorage.cs`
- **业务层**: `World Server/Servc/GameServer.cs`
- **服务接口**: `World Server/Servc/GameService.cs`

---

## 版本信息

- **框架版本**: World Server Framework
- **协议版本**: GameCompany v1.0
- **存储方式**: 本地JSON文件
- **自动保存**: 24小时/次

---

## 许可说明

本项目基于World框架开发，遵循原框架的使用规则。

---

**祝您使用愉快！**
