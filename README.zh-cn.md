> **[📖 English](README.md)**
> **[📖 简体中文(大陆)](README.zh-cn.md)**

[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer)
[![Gitee](https://img.shields.io/badge/Gitee-C71D23?style=for-the-badge&logo=gitee&logoColor=white)](https://gitee.com/vincent-zyu/CounterStrikeSharpListenerWsServer)

[![CS2](https://img.shields.io/badge/for-CounterStrikeSharp-2B3980?style=for-the-badge&logo=csharp&logoColor=white&labelColor=FCAC19)](https://github.com/roflmuffin/CounterStrikeSharp)
[![NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

[![QQ群](https://img.shields.io/badge/QQ群-1085190201-12B7F5?style=for-the-badge&logo=qq&logoColor=white)](https://qm.qq.com/q/4vjto4V7Di)

<p>💬 插件使用问题 / 🐛 Bug反馈 / 👨‍💻 插件开发交流，欢迎加入QQ群：<b>1085190201</b> 🎉</p>
<p>💡 在群里直接艾特我，回复的更快哦~ ✨</p>

# 🎮 🔌 🌐 CounterStrikeSharpListenerWsServer

一个 Counter-Strike 2 服务端插件，通过 WebSocket 将 CS2 服务器事件（玩家进出、聊天）桥接到外部聊天平台（QQ / Discord / Kook / Telegram）。配合 [koishi-plugin-mclistener-ws-client](https://github.com/VincentZyuApps/koishi-plugin-mclistener-ws-client) 实现群服互通。

基于 [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) 框架，使用 C# (.NET 10) 编写。**零额外 NuGet 依赖** — 仅使用 .NET BCL 内置的 `System.Net.WebSockets`。

## ✨ 功能

- 🚪 **玩家进出广播** — 玩家进入/离开 CS2 服务器时自动通知群聊
- 💬 **玩家聊天转发** — 游戏内玩家的聊天消息实时转发到群聊
- 📨 **群消息转发** — QQ/Discord 等平台的群消息转发到 CS2 游戏内聊天
- 🔐 **Token 验证** — 可选 `?token=xxx` URL 参数，验证客户端身份
- 🌍 **跨平台** — 同时支持 Windows 和 Linux
- ⚙️ **自动生成配置** — 首次启动自动创建 `config.json`，开箱即用

## 🏗️ 架构

```
┌──────────────────┐       WebSocket JSON        ┌──────────────────────────────┐
│   Koishi 机器人   │ ◄══════════════════════════► │  CounterStrikeSharpListener  │
│ (QQ/Discord/...) │   ws://主机:端口?token=xxx   │         WsServer             │
└──────────────────┘                              └─────────────┬────────────────┘
                                                                │
                                                     ┌──────────┴──────────┐
                                                     │    CS2 服务器        │
                                                     │ (CounterStrikeSharp) │
                                                     └─────────────────────┘
```

## 🏃 快速开始

### 📦 部署到服务器

1. **安装 Metamod:Source**  
   参考 https://cs2.poggu.me/metamod/installation/  
   下载对应的发行版，比如 Linux 版，解压到 `csgo/` 目录

   解压后结构：
   ```
   csgo/addons/
   └── metamod/
   ```

2. **安装 CounterStrikeSharp**  
   下载 [with-runtime 版本](https://github.com/roflmuffin/CounterStrikeSharp/releases)  
   解压 `addons/` 合并到 `csgo/` 目录

   合并后结构：
   ```
   csgo/
   └── addons/
     ├── metamod/
     └── counterstrikesharp/
         ├── api/
         ├── dotnet/
         └── plugins/
   ```

3. **放入插件**
   CSS 要求插件放在 `plugins/<插件名>/<插件名>.dll` 子目录中（目录名 = DLL 文件名）。
   从 [Releases](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/releases/latest) 下载 `.dll`：
   ```bash
   # 一般默认的steam路径在 ~/.local 里面
   cd "path/to/Steam/steamapps/common/Counter-Strike Global Offensive/game"
   TAG=<最新版本号>
   PLUGIN_DIR=csgo/addons/counterstrikesharp/plugins/CounterStrikeSharpListenerWsServer
   mkdir -p $PLUGIN_DIR
   cd $PLUGIN_DIR
   wget "https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/releases/download/$TAG/CounterStrikeSharpListenerWsServer-$TAG.dll"
   mv CounterStrikeSharpListenerWsServer-$TAG.dll CounterStrikeSharpListenerWsServer.dll
   ```

   最终插件结构：
   ```
   plugins/
   └── CounterStrikeSharpListenerWsServer/
       └── CounterStrikeSharpListenerWsServer.dll
   ```

4. **启动服务器**
   ```bash
   ./cs2 -dedicated -game csgo +map de_dust2 +sv_lan 1
   ```

   控制台看到 `[Plugin] WS Server started on 0.0.0.0:60606` 即启动成功。
   首次启动会自动生成默认的 `config.json`。

5. **配置 Koishi 客户端**
   在 Koishi 插件配置中设置：
   - `wsServerUrl`：`ws://<CS2服务器IP>:60606`
   - `wsToken`：`test12345`（与 `config.json` 中保持一致）

   **重要：** 生产环境请务必修改默认 token！

## ⚙️ 配置说明

首次启动后，`config.json` 自动生成在 `csgo/addons/counterstrikesharp/plugins/CounterStrikeSharpListenerWsServer/config.json`：

```json
{
  "host": "0.0.0.0",
  "port": 60606,
  "wsToken": "test12345",
  "enablePlayerJoinBroadcast": true,
  "enablePlayerLeaveBroadcast": true,
  "enablePlayerChatBroadcast": true,
  "enableReceiveGroupMessage": true,
  "groupMessageFormat": "[{group_name}]({group_id}) {nickname}: {message}"
}
```

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `host` | string | `0.0.0.0` | WebSocket 服务器监听地址 |
| `port` | int | `60606` | WebSocket 服务器监听端口 |
| `wsToken` | string | `test12345` | 客户端连接令牌（空字符串 = 不验证） |
| `enablePlayerJoinBroadcast` | bool | `true` | 广播玩家进入事件 |
| `enablePlayerLeaveBroadcast` | bool | `true` | 广播玩家离开事件 |
| `enablePlayerChatBroadcast` | bool | `true` | 转发玩家聊天到群聊 |
| `enableReceiveGroupMessage` | bool | `true` | 接收群消息转发到游戏内 |
| `groupMessageFormat` | string | `[{group_name}]({group_id}) {nickname}: {message}` | 游戏内群消息显示模板。可用占位符：`{group_name}` `{group_id}` `{nickname}` `{message}` |

## 🔌 WebSocket 协议

本插件遵循与 [mcdr_listener_ws_server](https://github.com/VincentZyuApps/mcdr_listener_ws_server) 和 [levilamina-plugin-mclistener-ws-server](https://github.com/VincentZyuApps/levilamina-plugin-mclistener-ws-server) 相同的 JSON 协议，可直接配合现有的 [koishi-plugin-mclistener-ws-client](https://github.com/VincentZyuApps/koishi-plugin-mclistener-ws-client) 使用。

### 服务端 → 客户端（广播）

```json
{"type": "player_join", "player_name": "Steve"}
{"type": "player_leave", "player_name": "Steve"}
{"type": "player_chat", "player_name": "Steve", "content": "大家好！"}
```

### 客户端 → 服务端

```json
{"type": "chat_platform_to_server", "group_id": "1085190201", "group_name": "onebot", "nickname": "Alice", "message": "这条消息来自QQ群！"}
```

## 🤖 GitHub Actions

推送到 GitHub 时，**commit 信息中的关键词控制流水线行为**。

| 关键词 | 构建 DLL | 发布 Release |
|---|---|---|
| `build action` | ✅ | ❌ |
| `build release` | ✅ | ✅ |

### 流水线阶段

```
check ──→ build ──→ release
  │         │         │
  │         │         └── 下载 artifact → 创建 GitHub Release
  │         │
  │         └── dotnet restore → dotnet build → 上传 artifact
  │
  └── 解析 commit 信息 → 输出控制 flag
```

### 使用示例

```bash
# 只构建，不发布
git commit -m "fix: 修了个bug, build action"

# 构建并发布 Release
git commit -m "feat: 加了个新功能, build release"
```

### 自定义版本号

编辑 `CounterStrikeSharpListenerWsServer.csproj` 中的 `<Version>` 字段：
```xml
<Version>x.y.z</Version>
```
编辑 `CounterStrikeSharpListenerWsServer.cs` 中的类属性字符串 `ModuleVersion` 的值：
```cs
public override string ModuleVersion => "x.y.z";
```

下次触发 Release 时，标签会自动变成 `vx.y.z-{run_number}`。

[![最后提交](https://img.shields.io/github/last-commit/VincentZyuApps/CounterStrikeSharpListenerWsServer?style=for-the-badge&label=最后提交&labelColor=181717&color=555555)](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/commits/main)
[![构建状态](https://img.shields.io/github/actions/workflow/status/VincentZyuApps/CounterStrikeSharpListenerWsServer/build.yml?style=for-the-badge&label=构建状态&labelColor=181717&logo=github)](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/actions)

