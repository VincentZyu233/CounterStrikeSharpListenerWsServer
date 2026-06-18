<div align=center>

[![Downloads](https://img.shields.io/github/downloads/__REPO__/v__VERSION__/total?style=flat-square&logo=github)](https://github.com/__REPO__/releases/tag/v__VERSION__)
[![CS2](https://img.shields.io/badge/for-CounterStrikeSharp-FCAC19?style=flat-square&logo=csharp&logoColor=white&labelColor=2B3980)](https://github.com/roflmuffin/CounterStrikeSharp)
[![NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

</div>

---

### 🎯 版本亮点

__COMMIT_LOG__

---

### ⬇️ 下载 Release 产物

| 文件 | 说明 |
|------|------|
| [📦 `CounterStrikeSharpListenerWsServer-v__VERSION__.dll`](__BASE_URL__/CounterStrikeSharpListenerWsServer-v__VERSION__.dll) | 🔌 插件主程序 |
| [📦 `CounterStrikeSharpListenerWsServer-v__VERSION__.pdb`](__BASE_URL__/CounterStrikeSharpListenerWsServer-v__VERSION__.pdb) | 🐛 调试符号文件（可选，保留方便报错定位行号） |

### 📥 安装方法

```bash
# 1. 下载到插件子目录（目录名必须等于 DLL 文件名）
cd csgo/addons/counterstrikesharp/plugins/
mkdir -p CounterStrikeSharpListenerWsServer
cd CounterStrikeSharpListenerWsServer
wget "__BASE_URL__/CounterStrikeSharpListenerWsServer-v__VERSION__.dll"
wget "__BASE_URL__/CounterStrikeSharpListenerWsServer-v__VERSION__.pdb"
```

### 📋 部署流程

1. ✅ 服务器已安装 Metamod:Source + CounterStrikeSharp
2. ✅ 下载 DLL 放入 `plugins/CounterStrikeSharpListenerWsServer/`
3. ✅ 首次启动自动生成 `config.json`，按需修改 WebSocket 端口和 token
4. ✅ 在 Koishi 客户端配置 `wsServerUrl` 指向 `ws://<CS2服务器IP>:60606?token=test12345`
5. ✅ 重启服务端

### 📋 功能说明

- 🚪 玩家进出 CS2 服务端广播
- 💬 CS2 玩家聊天消息转发到群聊
- 📨 群聊消息转发到 CS2 游戏内

---

### 📊 构建信息

- **构建日期**: __BUILD_DATE__
- **提交**: __COMMIT_HASH__
- **完整 Changelog**: __CHANGELOG_URL__

