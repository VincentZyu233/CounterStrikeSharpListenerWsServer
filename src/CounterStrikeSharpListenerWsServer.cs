using System.Net.WebSockets;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;

namespace CounterStrikeSharpListenerWsServer;

// Main plugin: bridges CS2 game events ↔ WebSocket ↔ chat platforms
public class CounterStrikeSharpListenerWsServer : BasePlugin {
    public override string ModuleName => "CounterStrikeSharp Listener WS Server";
    public override string ModuleVersion => "0.3.1";

    private WsServer? _wsServer;
    private RconClient? _rcon;
    private PluginConfig _config = new();
    private PluginLogger? _log;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    // Load config, init logger, start WS server, register CS2 event handlers
    public override void Load(bool hotReload) {
        _config = PluginConfig.Load(Path.Combine(ModuleDirectory, "config.json"));
        _log = new PluginLogger(Logger, _config.logLevel);
        _log.Info($"[Plugin] Starting v{ModuleVersion}, log level: {_config.logLevel}");

        _wsServer = new WsServer(_log);
        _wsServer.OnMessageReceived += HandleWsMessage;
        _ = _wsServer.StartAsync(_config.Host, _config.Port, _config.WsToken);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
        _log.Info($"[Plugin] WS Server started on {_config.Host}:{_config.Port}");

        // Initialize RCON client if relay mode is configured
        if (_config.ExecCommandMode == "rcon-relay") {
            _rcon = new RconClient();
            _log.Info($"[Plugin] RCON mode enabled, target: {_config.RconHost}:{_config.RconPort}");
        }

        _log?.Trace("[Plugin] Load done");
    }

    // Shutdown: stop WS server, dispose RCON client
    public override void Unload(bool hotReload) {
        _wsServer?.StopAsync().Wait();
        _rcon?.Dispose();
        _log?.Info("[Plugin] WS Server stopped");
    }

    // Build player join/leave JSON, apply Bot/Player suffix, broadcast
    private HookResult BroadcastPlayerEvent(string playerName, string type, bool enabled, bool isBot) {
        if (!enabled || string.IsNullOrEmpty(playerName)) return HookResult.Continue;
        var name = playerName + (isBot ? _config.BotSuffix : _config.PlayerSuffix);
        _log?.Debug($"[Plugin] Broadcast {type}: {name}");
        var json = JsonSerializer.Serialize(new PlayerEventMessage(type, name), JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));
        return HookResult.Continue;
    }

    // CS2 event: player fully connected → player_join (IsBot from CCSPlayerController)
    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info) =>
        BroadcastPlayerEvent(@event.Userid?.PlayerName ?? "", "player_join", _config.EnablePlayerJoinBroadcast, @event.Userid?.IsBot ?? false);

    // CS2 event: player disconnected → player_leave (bot = empty or BOT networkid)
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info) =>
        BroadcastPlayerEvent(@event.Name, "player_leave", _config.EnablePlayerLeaveBroadcast, string.IsNullOrEmpty(@event.Networkid) || @event.Networkid == "BOT");

    // CS2 event: player chat message → player_chat
    private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info) {
        if (!_config.EnablePlayerChatBroadcast) return HookResult.Continue;
        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid || string.IsNullOrEmpty(player.PlayerName)) return HookResult.Continue;
        _log?.Debug($"[Plugin] Broadcast chat from {player.PlayerName}: {@event.Text}");
        var json = JsonSerializer.Serialize(new PlayerChatMessage("player_chat", player.PlayerName, @event.Text), JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));
        return HookResult.Continue;
    }

    // Route incoming WS message by JSON type field
    private void HandleWsMessage(WebSocket ws, string json) {
        _log?.Debug($"[Plugin] HandleWsMessage: {json[..Math.Min(json.Length, 120)]}...");
        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp)) { _log?.Warn("[Plugin] WS message has no type field"); return; }
            var type = typeProp.GetString();
            _log?.Info($"[Plugin] WS message type: {type}");

            // Dispatch to chat / command handler via Server.NextFrame (main thread)
            switch (type) {
                case "chat_platform_to_server":
                    _log?.Trace("[Plugin] Scheduling HandleChatMessage via NextFrame...");
                    Server.NextFrame(() => {
                        _log?.Trace("[Plugin] NextFrame executing HandleChatMessage...");
                        HandleChatMessage(json);
                        _log?.Trace("[Plugin] NextFrame HandleChatMessage done");
                    });
                    break;
                case "external_command_to_server":
                    _log?.Trace("[Plugin] Scheduling HandleCommandRequest via NextFrame...");
                    Server.NextFrame(() => {
                        _log?.Trace("[Plugin] NextFrame executing HandleCommandRequest...");
                        HandleCommandRequest(ws, json);
                        _log?.Trace("[Plugin] NextFrame HandleCommandRequest done");
                    });
                    break;
                default:
                    _log?.Warn($"[Plugin] Unknown WS message type: {type}");
                    break;
            }
        } catch (Exception ex) { _log?.Error($"[Plugin] Failed to handle WS message: {ex.Message}"); }
    }

    // Forward group message to all in-game players via PrintToChat
    private void HandleChatMessage(string json) {
        if (!_config.EnableReceiveGroupMessage) { _log?.Debug("[Plugin] HandleChatMessage: disabled"); return; }
        var msg = JsonSerializer.Deserialize<GroupMessage>(json, JsonOptions);
        if (msg == null) { _log?.Warn("[Plugin] HandleChatMessage: deserialize failed"); return; }
        _log?.Debug($"[Plugin] HandleChatMessage: from {msg.Nickname} in {msg.GroupName}: {msg.Message[..Math.Min(msg.Message.Length, 60)]}");
        var formatted = _config.GroupMessageFormat
            .Replace("{group_name}", msg.GroupName)
            .Replace("{group_id}", msg.GroupId)
            .Replace("{nickname}", msg.Nickname)
            .Replace("{message}", msg.Message);
        foreach (var player in Utilities.GetPlayers())
            if (player.IsValid) player.PrintToChat($" {ChatColors.Default}{formatted}");
    }

    // Handle remote command: validate whitelist → try built-in → dispatch by mode
    private void HandleCommandRequest(WebSocket ws, string json) {
        _log?.Trace("[Plugin] HandleCommandRequest: entering...");

        if (!_config.EnableRemoteExecCommand) {
            _log?.Warn("[Plugin] HandleCommandRequest: EnableRemoteExecCommand is false, skipping");
            return;
        }

        _log?.Trace("[Plugin] HandleCommandRequest: deserializing message...");
        var msg = JsonSerializer.Deserialize<CommandRequestMessage>(json, JsonOptions);
        if (msg == null) { _log?.Warn("[Plugin] HandleCommandRequest: failed to deserialize"); return; }

        var cmd = msg.Command.Trim();
        _log?.Info($"[Plugin] HandleCommandRequest: cmd={cmd}, requestId={msg.RequestId}");

        // Prefix whitelist check (empty = allow all)
        _log?.Trace($"[Plugin] HandleCommandRequest: whitelist check (len={_config.RemoteExecCommandWhitelist.Length})");
        if (_config.RemoteExecCommandWhitelist.Length > 0) {
            if (!_config.RemoteExecCommandWhitelist.Any(p => cmd.StartsWith(p, StringComparison.OrdinalIgnoreCase))) {
                _log?.Warn($"[Plugin] Command not in whitelist: {cmd}");
                SendCommandResult(ws, msg.RequestId, msg.Command, false, null, "Command not in whitelist");
                return;
            }
        }
        _log?.Trace("[Plugin] HandleCommandRequest: whitelist check passed");

        // Built-in handlers (list, players): use CSSharp API directly, have return values
        _log?.Trace("[Plugin] HandleCommandRequest: trying built-in handler...");
        var builtInResult = ExecuteBuiltInCommand(cmd);
        if (builtInResult != null) {
            _log?.Info($"[Plugin] Built-in command result: {builtInResult[..Math.Min(builtInResult.Length, 80)]}");
            SendCommandResult(ws, msg.RequestId, msg.Command, true, builtInResult, null);
            return;
        }
        _log?.Trace("[Plugin] HandleCommandRequest: no built-in handler matched");

        // Route to configured execution mode
        switch (_config.ExecCommandMode) {
            case "rcon-relay":
                ExecuteRconCommand(ws, msg, cmd);
                break;
            case "csharp-native":
            default:
                ExecuteNativeCommand(ws, msg, cmd);
                break;
        }

        _log?.Trace("[Plugin] HandleCommandRequest: done");
    }

    // Mode csharp-native: Server.ExecuteCommand — fire and forget, no output
    private void ExecuteNativeCommand(WebSocket ws, CommandRequestMessage msg, string cmd) {
        _log?.Trace("[Plugin] ExecuteNativeCommand: entering...");
        try {
            _log?.Info($"[Plugin] Native exec: {cmd}");
            Server.ExecuteCommand(cmd);
            _log?.Info($"[Plugin] Native exec done, sending result (requestId={msg.RequestId})");
            var result = _config.RemoteCommandReturnEmptyResult ? "" : null;
            SendCommandResult(ws, msg.RequestId, msg.Command, true, result, null);
        } catch (Exception ex) {
            _log?.Error($"[Plugin] Native exec failed: {cmd}, error: {ex.Message}");
            SendCommandResult(ws, msg.RequestId, msg.Command, false, null, ex.Message);
        }
    }

    // Mode rcon-relay: connect game server RCON → execute → return text output
    private async void ExecuteRconCommand(WebSocket ws, CommandRequestMessage msg, string cmd) {
        _log?.Trace("[Plugin] ExecuteRconCommand: entering...");
        if (_rcon == null) {
            _log?.Error("[Plugin] RCON client not initialized");
            SendCommandResult(ws, msg.RequestId, msg.Command, false, null, "RCON client not initialized");
            return;
        }
        try {
            _log?.Info($"[Plugin] RCON exec: {cmd}, connecting to {_config.RconHost}:{_config.RconPort}");
            await _rcon.ConnectAsync(_config.RconHost, _config.RconPort, _config.RconTimeoutMs);

            _log?.Trace("[Plugin] RCON: authenticating...");
            var authOk = await _rcon.AuthenticateAsync(_config.RconPassword, _config.RconTimeoutMs);
            if (!authOk) {
                _log?.Error($"[Plugin] RCON auth failed for {_config.RconHost}:{_config.RconPort}");
                SendCommandResult(ws, msg.RequestId, msg.Command, false, null, "RCON auth failed, check rcon password");
                return;
            }
            _log?.Trace("[Plugin] RCON: auth OK");

            _log?.Info($"[Plugin] RCON: executing {cmd}");
            var output = await _rcon.ExecuteCommandAsync(cmd, _config.RconTimeoutMs);
            _log?.Info($"[Plugin] RCON result ({output.Length} chars): {output[..Math.Min(output.Length, 80)]}");
            SendCommandResult(ws, msg.RequestId, msg.Command, true, output, null);
        } catch (Exception ex) {
            _log?.Error($"[Plugin] RCON exec failed: {cmd}, error: {ex.Message}");
            SendCommandResult(ws, msg.RequestId, msg.Command, false, null, $"RCON error: {ex.Message}");
        } finally {
            try { _rcon?.Dispose(); } catch { }
        }
    }

    // Build command_result JSON and send back to requesting WS client
    private void SendCommandResult(WebSocket ws, string requestId, string command, bool ok, string? result, string? error) {
        _log?.Trace($"[Plugin] SendCommandResult: building message (requestId={requestId})");
        var msg = new CommandResultMessage { RequestId = requestId, Command = command, Ok = ok, Result = result, Error = error };
        var json = JsonSerializer.Serialize(msg, JsonOptions);
        _log?.Debug($"[Plugin] SendCommandResult: serialized json (requestId={requestId}, ok={ok})");
        _log?.Trace($"[Plugin] SendCommandResult: json={json}");
        _ = Task.Run(() => {
            _log?.Trace($"[Plugin] SendCommandResult: Task.Run started (requestId={requestId})");
            _wsServer?.SendAsync(ws, json);
            _log?.Info($"[Plugin] SendCommandResult: sent via WebSocket (requestId={requestId}, ok={ok}, resultLen={result?.Length ?? 0})");
        });
        _log?.Trace($"[Plugin] SendCommandResult: Task.Run queued, returning");
    }

    // Built-in commands that bypass the engine, using CSSharp API for return values
    private static string? ExecuteBuiltInCommand(string cmd) {
        var parts = cmd.Trim().Split(' ', 2);
        switch (parts[0].ToLower()) {
            case "list": case "players":
                var players = Utilities.GetPlayers();
                return string.Join(", ", players.Where(p => p.IsValid).Select(p => p.PlayerName));
        }
        return null;
    }
}

// === JSON message types matching cross-server WS protocol ===

public record PlayerEventMessage(string Type, string PlayerName);
public record PlayerChatMessage(string Type, string PlayerName, string Content);

public class GroupMessage {
    public string Type { get; set; } = "";
    public string GroupId { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string Message { get; set; } = "";
}

public class CommandRequestMessage {
    public string Type { get; set; } = "";
    public string RequestId { get; set; } = "";
    public string Command { get; set; } = "";
}

// command_result response: type, request_id, command, ok, result/error
public class CommandResultMessage {
    public string Type { get; set; } = "command_result";
    public string RequestId { get; set; } = "";
    public string Command { get; set; } = "";
    public bool Ok { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}
