<div align=center>

[![Downloads](https://img.shields.io/github/downloads/__REPO__/__VERSION__/total?style=flat-square&logo=github)](https://github.com/__REPO__/releases/tag/__VERSION__)
[![CS2](https://img.shields.io/badge/for-CounterStrikeSharp-FCAC19?style=flat-square&logo=cplusplus&logoColor=white&labelColor=2B3980)](https://github.com/roflmuffin/CounterStrikeSharp)
[![NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

</div>

---

### ⬇️ Download Release Assets

| File | Description |
|------|------------|
| [📦 `CounterStrikeSharpListenerWsServer-__VERSION__.dll`](__BASE_URL__/CounterStrikeSharpListenerWsServer-__VERSION__.dll) | 🔌 Plugin main DLL |
| [📦 `CounterStrikeSharpListenerWsServer-__VERSION__.pdb`](__BASE_URL__/CounterStrikeSharpListenerWsServer-__VERSION__.pdb) | 🐛 Debug symbols (optional, kept for error line numbers) |

### 📥 Installation

```bash
# 1. Download to plugin subdirectory (dir name must match DLL filename)
cd csgo/addons/counterstrikesharp/plugins/
mkdir -p CounterStrikeSharpListenerWsServer
cd CounterStrikeSharpListenerWsServer
wget "__BASE_URL__/CounterStrikeSharpListenerWsServer-__VERSION__.dll"
wget "__BASE_URL__/CounterStrikeSharpListenerWsServer-__VERSION__.pdb"
mv CounterStrikeSharpListenerWsServer-__VERSION__.dll CounterStrikeSharpListenerWsServer.dll
mv CounterStrikeSharpListenerWsServer-__VERSION__.pdb CounterStrikeSharpListenerWsServer.pdb
```

### 📋 Deployment Steps

1. ✅ Server has Metamod:Source + CounterStrikeSharp installed
2. ✅ Download DLL into `plugins/CounterStrikeSharpListenerWsServer/`
3. ✅ `config.json` generated on first startup — edit WebSocket port & token as needed
4. ✅ In Koishi client, set `wsServerUrl` to `ws://<CS2_Server_IP>:60606?token=test12345`
5. ✅ Restart the server

### 📋 Features

- 🚪 Player join/leave broadcast
- 💬 In-game chat messages forwarded to group chat
- 📨 Group chat messages forwarded to in-game chat
