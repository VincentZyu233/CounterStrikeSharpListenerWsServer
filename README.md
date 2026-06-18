> **[📖 English](README.md)**
> **[📖 简体中文(大陆)](README.zh-cn.md)**

![CounterStrikeSharpListenerWsServer](https://socialify.git.ci/VincentZyu233/CounterStrikeSharpListenerWsServer/image?custom_description=%F0%9F%94%8C%F0%9F%8C%90%F0%9F%94%AB+CS2-to-chat-platform+bridge+via+WebSocket+%E2%80%94+player+join%2Fleave%2Fchat+broadcast+%26+group+message+relay+for+CounterStrikeSharp&description=1&forks=1&issues=1&language=1&logo=https%3A%2F%2Fencrypted-tbn0.gstatic.com%2Fimages%3Fq%3Dtbn%3AANd9GcQOYCNiIzbN_BjO8zQoHB8aaf9Pe6zET1_9aUlP8jt7Xg%26s%3D10&name=1&owner=1&pattern=Plus&pulls=1&stargazers=1&theme=Auto)

[![GitHub](https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer)
[![Gitee](https://img.shields.io/badge/Gitee-C71D23?style=for-the-badge&logo=gitee&logoColor=white)](https://gitee.com/vincent-zyu/CounterStrikeSharpListenerWsServer)

[![CS2](https://img.shields.io/badge/for-CounterStrikeSharp-2B3980?style=for-the-badge&logo=csharp&logoColor=white&labelColor=FCAC19)](https://github.com/roflmuffin/CounterStrikeSharp)
[![NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

[![QQ群](https://img.shields.io/badge/QQ群-1085190201-12B7F5?style=for-the-badge&logo=qq&logoColor=white)](https://qm.qq.com/q/4vjto4V7Di)

<p>💬 Plugin usage / 🐛 Bug reports / 👨‍💻 Plugin dev discussion, join QQ group: <b>1085190201</b> 🎉</p>
<p>💡 Mention me in the group for faster replies ~ ✨</p>

# 🎮 🔌 🌐 CounterStrikeSharpListenerWsServer

A Counter-Strike 2 server plugin that bridges CS2 events to external chat platforms (QQ / Discord / Kook / Telegram) via WebSocket. Works together with [koishi-plugin-mclistener-ws-client](https://github.com/VincentZyuApps/koishi-plugin-mclistener-ws-client) to achieve cross-platform group ↔ server chat interoperability.

Built on [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp), written in C# (.NET 10). **Zero extra NuGet dependencies** — uses only `System.Net.WebSockets` from the BCL.

## ✨ Features

- 🚪 **Player Join/Leave Broadcast** — announces when players enter or exit the CS2 server
- 💬 **Player Chat Forwarding** — forwards in-game chat messages to chat platforms
- 📨 **Group Message Relay** — forwards messages from QQ/Discord/etc. to CS2 in-game chat
- 🔐 **Token Authentication** — optional `?token=xxx` URL parameter for client verification
- 🌍 **Cross-Platform** — runs on both Windows and Linux
- ⚙️ **Auto-Generated Config** — first run creates `config.json` with sensible defaults

## 🏗️ Architecture

```
┌──────────────────┐       WebSocket JSON        ┌──────────────────────────────┐
│   Koishi Bot     │ ◄══════════════════════════► │  CounterStrikeSharpListener  │
│ (QQ/Discord/...) │   ws://host:port?token=xxx   │         WsServer             │
└──────────────────┘                              └─────────────┬────────────────┘
                                                                │
                                                     ┌──────────┴──────────┐
                                                     │    CS2 Server       │
                                                     │ (CounterStrikeSharp) │
                                                     └─────────────────────┘
```

## 🏃 Quick Start

### 📦 Deploy to Server

1. **Install Metamod:Source**  
   See https://cs2.poggu.me/metamod/installation/  
   Download the Linux build and extract to the `csgo/` directory

   Structure after extraction:
   ```
   csgo/addons/
   └── metamod/
   ```

2. **Install CounterStrikeSharp**  
   Download the [with-runtime release](https://github.com/roflmuffin/CounterStrikeSharp/releases)  
   Merge the `addons/` folder into `csgo/`

   Structure after merging:
   ```
   csgo/
   └── addons/
     ├── metamod/
     └── counterstrikesharp/
         ├── api/
         ├── dotnet/
         └── plugins/
   ```

3. **Add the Plugin**
   CSS requires plugins in `plugins/<PluginName>/<PluginName>.dll` (directory name = DLL filename).
   Download the `.dll` from [Releases](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/releases/latest):
   ```bash
   # Default Steam path is usually under ~/.local
   cd "path/to/Steam/steamapps/common/Counter-Strike Global Offensive/game"
   TAG=<latest version tag>
   PLUGIN_DIR=csgo/addons/counterstrikesharp/plugins/CounterStrikeSharpListenerWsServer
   mkdir -p $PLUGIN_DIR
   cd $PLUGIN_DIR
   wget "https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/releases/download/$TAG/CounterStrikeSharpListenerWsServer-$TAG.dll"
   mv CounterStrikeSharpListenerWsServer-$TAG.dll CounterStrikeSharpListenerWsServer.dll
   ```

   Final plugin structure:
   ```
   plugins/
   └── CounterStrikeSharpListenerWsServer/
       └── CounterStrikeSharpListenerWsServer.dll
   ```

4. **Start the Server**
   ```bash
   ./cs2 -dedicated -game csgo +map de_dust2 +sv_lan 1
   ```

   You should see `[Plugin] WS Server started on 0.0.0.0:60618` in the console.
   A default `config.json` will be generated automatically.

5. **Configure the Koishi Client**
   In your Koishi plugin config, set:
   - `wsServerUrl`: `ws://<CS2_SERVER_IP>:60618`
   - `wsToken`: `test12345` (match with `config.json`)

   **Important:** Change the default token for production use!

## ⚙️ Configuration

On first startup, `config.json` is auto-generated at `csgo/addons/counterstrikesharp/plugins/CounterStrikeSharpListenerWsServer/config.json`:

```json
{
  "host": "0.0.0.0",
  "port": 60618,
  "wsToken": "test12345",
  "enablePlayerJoinBroadcast": true,
  "enablePlayerLeaveBroadcast": true,
  "enablePlayerChatBroadcast": true,
  "enableReceiveGroupMessage": true,
  "groupMessageFormat": "[{group_name}]({group_id}) {nickname}: {message}"
}
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `host` | string | `0.0.0.0` | WebSocket server listen address |
| `port` | int | `60618` | WebSocket server listen port |
| `wsToken` | string | `test12345` | Auth token (empty = no auth) |
| `enablePlayerJoinBroadcast` | bool | `true` | Broadcast player join events |
| `enablePlayerLeaveBroadcast` | bool | `true` | Broadcast player leave events |
| `enablePlayerChatBroadcast` | bool | `true` | Broadcast player chat to chat platforms |
| `enableReceiveGroupMessage` | bool | `true` | Forward group messages to in-game chat |
| `groupMessageFormat` | string | `[{group_name}]({group_id}) {nickname}: {message}` | Template for in-game group messages. Available placeholders: `{group_name}`, `{group_id}`, `{nickname}`, `{message}` |

## 🔌 WebSocket Protocol

The plugin follows the same JSON protocol used by [mcdr_listener_ws_server](https://github.com/VincentZyuApps/mcdr_listener_ws_server) and [levilamina-plugin-mclistener-ws-server](https://github.com/VincentZyuApps/levilamina-plugin-mclistener-ws-server), making it fully compatible with the existing [koishi-plugin-mclistener-ws-client](https://github.com/VincentZyuApps/koishi-plugin-mclistener-ws-client).

### Server → Client (broadcast)

```json
{"type": "player_join", "player_name": "Steve"}
{"type": "player_leave", "player_name": "Steve"}
{"type": "player_chat", "player_name": "Steve", "content": "Hello!"}
```

### Client → Server

```json
{"type": "chat_platform_to_server", "group_id": "1085190201", "group_name": "onebot", "nickname": "Alice", "message": "Hi from QQ!"}
```

## 🤖 GitHub Actions

When pushing to GitHub, **keywords in the commit message control the pipeline**.

| Keyword | Build DLL | Publish Release |
|---|---|---|
| `build action` | ✅ | ❌ |
| `build release` | ✅ | ✅ |

### Pipeline Stages

```
check ──→ build ──→ release
  │         │         │
  │         │         └── download artifact → create GitHub Release
  │         │
  │         └── dotnet restore → dotnet build → upload artifact
  │
  └── parse commit message → set control flags
```

### Usage Examples

```bash
# Build only, no release
git commit -m "fix: fixed a bug, build action"

# Build and publish a Release
git commit -m "feat: added a new feature, build release"
```

### Custom Version

Edit the `<Version>` field in `CounterStrikeSharpListenerWsServer.csproj`:
```xml
<Version>x.y.z</Version>
```
Edit the `ModuleVersion` string in `CounterStrikeSharpListenerWsServer.cs`:
```cs
public override string ModuleVersion => "x.y.z";
```

The next Release will automatically use `vx.y.z-{run_number}` as the tag.

[![Last Commit](https://img.shields.io/github/last-commit/VincentZyuApps/CounterStrikeSharpListenerWsServer?style=for-the-badge&label=Last%20Commit&labelColor=181717&color=555555)](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/commits/main)
[![Build Status](https://img.shields.io/github/actions/workflow/status/VincentZyuApps/CounterStrikeSharpListenerWsServer/build.yml?style=for-the-badge&label=Build%20Status&labelColor=181717&logo=github)](https://github.com/VincentZyuApps/CounterStrikeSharpListenerWsServer/actions)

