# 🎨 Windows Terminal 配置指南

## 安装步骤

### 方法1：Microsoft Store安装（推荐）
1. 打开 **Microsoft Store**
2. 搜索 "Windows Terminal"
3. 点击 "获取" 进行安装

### 方法2：直接下载
访问：https://github.com/microsoft/terminal/releases
下载最新的 `.msixbundle` 文件并双击安装

## 美化配置

安装完成后，按照以下步骤配置：

### 1. 打开设置
- 启动 Windows Terminal
- 按 `Ctrl + ,` 打开设置
- 或点击右上角 `˅` → `设置`

### 2. 应用美化主题

在设置界面左侧点击 `打开 JSON 文件`，将以下配置复制到 `schemes` 数组中：

```json
{
    "name": "GameServer Theme",
    "background": "#0C0C0C",
    "foreground": "#CCCCCC",
    "black": "#0C0C0C",
    "red": "#E74856",
    "green": "#16C60C",
    "yellow": "#F9F1A5",
    "blue": "#3B78FF",
    "purple": "#B4009E",
    "cyan": "#61D6D6",
    "white": "#CCCCCC",
    "brightBlack": "#767676",
    "brightRed": "#FF6B68",
    "brightGreen": "#35D17C",
    "brightYellow": "#F9F1A5",
    "brightBlue": "#5EAEFF",
    "brightPurple": "#D670D6",
    "brightCyan": "#6FFFFF",
    "brightWhite": "#F2F2F2"
}
```

### 3. 配置 Command Prompt 配置文件

在 `profiles.list` 中找到 Command Prompt 配置，修改为：

```json
{
    "guid": "{0caa0dad-35be-5f56-a8ff-afceeeaa6101}",
    "name": "GameServer CMD",
    "commandline": "%SystemRoot%\\System32\\cmd.exe",
    "colorScheme": "GameServer Theme",
    "font":
    {
        "face": "Cascadia Code",
        "size": 11
    },
    "opacity": 95,
    "useAcrylic": true,
    "cursorShape": "bar",
    "startingDirectory": "C:\\Users\\Nie\\Desktop\\GameCompanyServer\\World Server\\bin\\Debug"
}
```

### 4. 字体安装（可选）

**推荐字体：Cascadia Code**（Windows Terminal自带）

或安装其他编程字体：
- **JetBrains Mono**: https://www.jetbrains.com/lp/mono/
- **Fira Code**: https://github.com/tonsky/FiraCode
- **Source Code Pro**: https://adobe-fonts.github.io/source-code-pro/

下载后双击 `.ttf` 文件安装，然后在 Windows Terminal 设置中修改 `font.face` 字段。

## 快速启动服务器

### 创建快捷启动方式

1. 在 Windows Terminal 设置中点击 `启动`
2. 将 `默认配置文件` 设置为 "GameServer CMD"
3. 开启 `启动时最大化`

### 使用批处理文件

创建 `StartServer.bat` 文件：

```bat
@echo off
cd /d "C:\Users\Nie\Desktop\GameCompanyServer\World Server\bin\Debug"
wt -w 0 -p "GameServer CMD" cmd /k "Word Server.exe"
```

双击即可在 Windows Terminal 中启动服务器。

## 常用快捷键

- **新标签页**: `Ctrl + Shift + T`
- **关闭标签页**: `Ctrl + Shift + W`
- **切换标签**: `Ctrl + Tab`
- **分屏（垂直）**: `Alt + Shift + +`
- **分屏（水平）**: `Alt + Shift + -`
- **复制**: `Ctrl + Shift + C`
- **粘贴**: `Ctrl + Shift + V`
- **清屏**: 输入 `cls`

## 效果预览

配置完成后，你的服务器日志将显示为：

```
✓ 日志美化完成
➤ POST /api/player/join
  ✓ 玩家加入: 张三
➤ POST /api/data/set
  ✓ 保存Building数据: xxxxx.block (1234字节)
```

## 进阶配置

### 透明度调整
在配置文件中修改：
```json
"opacity": 90,        // 透明度 (0-100)
"useAcrylic": true    // 毛玻璃效果
```

### 背景图片
```json
"backgroundImage": "C:\\path\\to\\image.png",
"backgroundImageOpacity": 0.3
```

### 光标样式
```json
"cursorShape": "bar",      // bar, vintage, underscore, filledBox, emptyBox
"cursorColor": "#FFFFFF"
```

---

**提示**: 配置保存后会自动生效，无需重启。
