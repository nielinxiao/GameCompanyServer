# 🎨 控制台美化工具使用指南

## 工具简介

已创建的美化工具位于：`Tools/ConsoleTableFormatter.cs`

包含两个类：
1. **ConsoleTableFormatter** - 表格美化工具
2. **ConsoleKeyValueFormatter** - 键值对美化工具

---

## 📊 表格美化工具

### 基础用法

```csharp
// 创建表格
var table = new ConsoleTableFormatter();

// 添加列
table.AddColumn("玩家ID", "玩家名", "公司名", "在线时长");

// 添加行
table.AddRow("abc123", "张三", "北方公司", "2小时");
table.AddRow("def456", "李四", "南方公司", "1.5小时");
table.AddRow("ghi789", "王五", "东方公司", "3小时");

// 渲染输出
table.Render();
```

### 输出效果

```
┌─────────┬────────┬──────────┬──────────┐
│ 玩家ID  │ 玩家名 │ 公司名   │ 在线时长 │
├─────────┼────────┼──────────┼──────────┤
│ abc123  │ 张三   │ 北方公司 │ 2小时    │
│ def456  │ 李四   │ 南方公司 │ 1.5小时  │
│ ghi789  │ 王五   │ 东方公司 │ 3小时    │
└─────────┴────────┴──────────┴──────────┘
```

### 快速方法

```csharp
// 一行代码创建表格
ConsoleTableFormatter.QuickRender(
    new string[] { "键", "值", "类型" },
    new List<object[]>
    {
        new object[] { "jsonDicKey", "UPgCmpna6s...", "玩家ID" },
        new object[] { "jsonDoubleKey", "block", "建筑数据" },
        new object[] { "数据大小", "4523字节", "JSON" }
    },
    "Building数据详情"
);
```

### 自定义样式

```csharp
var table = new ConsoleTableFormatter()
    .AddColumn("列1", "列2", "列3")
    .SetHeaderColor(ConsoleColor.Yellow)     // 表头颜色
    .SetBorderColor(ConsoleColor.DarkGray)   // 边框颜色
    .ShowBorder(true);                        // 显示边框

table.AddRow("值1", "值2", "值3");
table.Render();
```

---

## 🔑 键值对美化工具

### 基础用法

```csharp
var formatter = new ConsoleKeyValueFormatter();

formatter
    .Add("玩家ID", "UPgCmpna6seyUOJFkbo+Fw==")
    .Add("玩家名", "张三")
    .Add("公司名", "北方公司")
    .Add("数据类型", "block")
    .Add("建筑数量", 12)
    .Add("数据大小", "4523字节");

formatter.Render();
```

### 输出效果

```
  玩家ID  : UPgCmpna6seyUOJFkbo+Fw==
  玩家名  : 张三
  公司名  : 北方公司
  数据类型: block
  建筑数量: 12
  数据大小: 4523字节
```

### 快速方法

```csharp
ConsoleKeyValueFormatter.QuickRender(
    new Dictionary<string, object>
    {
        { "玩家ID", "abc123..." },
        { "操作", "保存Building数据" },
        { "建筑数量", 15 },
        { "数据大小", "4523字节" },
        { "状态", "成功" }
    },
    "✓ Building数据保存"
);
```

### 输出效果

```
✓ Building数据保存
  玩家ID  : abc123...
  操作    : 保存Building数据
  建筑数量: 15
  数据大小: 4523字节
  状态    : 成功
```

### 自定义样式

```csharp
var formatter = new ConsoleKeyValueFormatter()
    .SetKeyColor(ConsoleColor.Cyan)      // 键的颜色
    .SetValueColor(ConsoleColor.White)   // 值的颜色
    .SetSeparator(" => ");                // 分隔符

formatter.Add("状态", "成功");
formatter.Render();
```

---

## 💡 实际应用场景

### 场景1：显示Building保存信息

**修改前（GameServer.cs第1434行）:**
```csharp
_log.LogGreen($"[GameServer] 保存Building: {effectiveJsonDicKey}.{jsonDoubleKey} ({valueLength}字节)");
```

**修改后:**
```csharp
try
{
    // 解析JSON获取建筑数量
    var buildData = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonValue);
    int buildCount = buildData?.builds != null ?
        ((Newtonsoft.Json.Linq.JArray)buildData.builds).Count : 0;

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n╔═══ Building数据保存 ═══");
    Console.ResetColor();

    ConsoleKeyValueFormatter.QuickRender(new Dictionary<string, object>
    {
        { "玩家ID", effectiveJsonDicKey.Substring(0, Math.Min(16, effectiveJsonDicKey.Length)) + "..." },
        { "数据类型", jsonDoubleKey },
        { "建筑数量", buildCount },
        { "数据大小", $"{valueLength:N0} 字节" },
        { "压缩率", valueLength > 1000 ? $"~{(valueLength / 1024.0):F1} KB" : "< 1 KB" },
        { "时间", DateTime.Now.ToString("HH:mm:ss") }
    });
}
catch
{
    // 如果JSON解析失败，使用简单格式
    _log.LogGreen($"[GameServer] 保存Building: {effectiveJsonDicKey}.{jsonDoubleKey} ({valueLength}字节)");
}
```

**输出效果:**
```
╔═══ Building数据保存 ═══
  玩家ID  : UPgCmpna6seyUO...
  数据类型: block
  建筑数量: 12
  数据大小: 4,523 字节
  压缩率  : ~4.4 KB
  时间    : 19:45:32
```

---

### 场景2：显示玩家加入信息

**修改 HttpServer.cs HandleJoinRequest方法:**
```csharp
ConsoleKeyValueFormatter.QuickRender(new Dictionary<string, object>
{
    { "玩家", request.PlayerName },
    { "公司", request.CompanyName },
    { "用户ID", request.PlayerId.Substring(0, Math.Min(16, request.PlayerId.Length)) + "..." },
    { "在线人数", _sessionManager.GetOnlineCount() },
    { "时间", DateTime.Now.ToString("HH:mm:ss") }
}, "✓ 玩家加入");
```

**输出效果:**
```
✓ 玩家加入
  玩家    : 张三
  公司    : 北方公司
  用户ID  : UPgCmpna6seyUO...
  在线人数: 3
  时间    : 19:45:32
```

---

### 场景3：显示在线玩家列表

**新增方法（可在GameServer.cs中添加）:**
```csharp
public void ShowOnlinePlayers()
{
    var sessions = _sessionManager.GetAllSessions(); // 需要在SessionManager添加此方法

    var table = new ConsoleTableFormatter();
    table.AddColumn("玩家名", "公司名", "在线时长", "最后活动");

    foreach (var session in sessions)
    {
        var onlineTime = DateTime.Now - session.LastActivity;
        table.AddRow(
            session.PlayerName,
            session.CompanyName,
            $"{(int)onlineTime.TotalMinutes}分钟",
            session.LastActivity.ToString("HH:mm:ss")
        );
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n═══ 在线玩家列表 ({sessions.Length}) ═══");
    Console.ResetColor();

    table.Render();
}
```

**输出效果:**
```
═══ 在线玩家列表 (3) ═══
┌────────┬──────────┬──────────┬──────────┐
│ 玩家名 │ 公司名   │ 在线时长 │ 最后活动 │
├────────┼──────────┼──────────┼──────────┤
│ 张三   │ 北方公司 │ 15分钟   │ 19:45:32 │
│ 李四   │ 南方公司 │ 8分钟    │ 19:52:15 │
│ 王五   │ 东方公司 │ 23分钟   │ 19:37:20 │
└────────┴──────────┴──────────┴──────────┘
```

---

### 场景4：显示Hash数据详情

**替换所有"HTTP SetJson参数"日志:**
```csharp
// 使用表格显示参数映射
ConsoleTableFormatter.QuickRender(
    new string[] { "参数", "值", "来源字段" },
    new List<object[]>
    {
        new object[] { "jsonKey", jsonKey ?? "(空)", jsonKeySource ?? "(未命中)" },
        new object[] { "jsonDicKey", jsonDicKey ?? "(空)", jsonDicKeySource ?? "(未命中)" },
        new object[] { "jsonDoubleKey", jsonDoubleKey ?? "(空)", jsonDoubleKeySource ?? "(未命中)" },
        new object[] { "jsonValue", $"{valueLength}字节", jsonValueSource ?? "(未命中)" }
    },
    "HTTP SetJson参数"
);
```

**输出效果:**
```
HTTP SetJson参数
┌───────────────┬──────────────┬────────────┐
│ 参数          │ 值           │ 来源字段   │
├───────────────┼──────────────┼────────────┤
│ jsonKey       │ (空)         │ (未命中)   │
│ jsonDicKey    │ UPgCmpna6... │ jsonDicKey │
│ jsonDoubleKey │ block        │ JsonDouble │
│ jsonValue     │ 4523字节     │ jsonValue  │
└───────────────┴──────────────┴────────────┘
```

---

## 🎯 推荐替换的日志位置

### 高优先级替换
1. **Building数据保存** - `GameServer.cs:1434`
2. **玩家加入** - `HttpServer.cs:251`
3. **HTTP SetJson参数** - `GameServer.cs:1423-1424`

### 中优先级替换
4. **GetJson Hash查询** - `GameServer.cs:1269-1272`
5. **在线玩家列表** - 新增功能

### 低优先级替换
6. **Session管理日志** - `SessionManager.cs:46,60,90`

---

## 🔧 额外工具方法

### 添加到SessionManager.cs

```csharp
/// <summary>
/// 获取所有会话（用于显示在线玩家列表）
/// </summary>
public PlayerSession[] GetAllSessions()
{
    return _sessions.Values
        .Where(s => (DateTime.Now - s.LastActivity).TotalMinutes < 30)
        .OrderByDescending(s => s.LastActivity)
        .ToArray();
}

/// <summary>
/// 显示在线玩家表格
/// </summary>
public void ShowOnlinePlayersTable()
{
    var sessions = GetAllSessions();

    var table = new ConsoleTableFormatter();
    table.AddColumn("玩家名", "公司名", "在线时长", "最后活动");

    foreach (var session in sessions)
    {
        var onlineTime = DateTime.Now - session.LastActivity;
        table.AddRow(
            session.PlayerName,
            session.CompanyName,
            $"{(int)onlineTime.TotalMinutes}分钟",
            session.LastActivity.ToString("HH:mm:ss")
        );
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n═══ 在线玩家 ({sessions.Length}) ═══");
    Console.ResetColor();

    table.Render();
}
```

---

## 📝 总结

使用这些美化工具，你可以：
- ✅ 让日志更易读
- ✅ 快速定位关键信息
- ✅ 提升调试效率
- ✅ 专业的控制台输出

建议优先替换Building数据保存和玩家加入的日志，这两个是最频繁出现的。
