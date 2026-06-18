using System.Net.WebSockets;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CounterStrikeSharpListenerWsServer;

public class CounterStrikeSharpListenerWsServer : BasePlugin {
    public override string ModuleName => "CounterStrikeSharp Listener WS Server";
    public override string ModuleVersion => "0.3.1";

    private WsServer? _wsServer;
    private PluginConfig _config = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public override void Load(bool hotReload) {
        _config = PluginConfig.Load(Path.Combine(ModuleDirectory, "config.json"));
        _wsServer = new WsServer(Logger);
        _wsServer.OnMessageReceived += HandleWsMessage;
        _ = _wsServer.StartAsync(_config.Host, _config.Port, _config.WsToken);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat);
        Logger.LogInformation($"[Plugin] WS Server started on {_config.Host}:{_config.Port}");
    }

    public override void Unload(bool hotReload) {
        _wsServer?.StopAsync().Wait();
        Logger.LogInformation("[Plugin] WS Server stopped");
    }

    private HookResult BroadcastPlayerEvent(string playerName, string type, bool enabled, bool isBot) {
        if (!enabled || string.IsNullOrEmpty(playerName)) return HookResult.Continue;
        var name = playerName + (isBot ? _config.BotSuffix : _config.PlayerSuffix);
        var json = JsonSerializer.Serialize(new PlayerEventMessage(type, name), JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));
        return HookResult.Continue;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info) =>
        BroadcastPlayerEvent(@event.Userid?.PlayerName ?? "", "player_join", _config.EnablePlayerJoinBroadcast, @event.Userid?.IsBot ?? false);

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info) =>
        BroadcastPlayerEvent(@event.Name, "player_leave", _config.EnablePlayerLeaveBroadcast, string.IsNullOrEmpty(@event.Networkid) || @event.Networkid == "BOT");

    private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info) {
        if (!_config.EnablePlayerChatBroadcast) return HookResult.Continue;
        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid || string.IsNullOrEmpty(player.PlayerName)) return HookResult.Continue;
        var json = JsonSerializer.Serialize(new PlayerChatMessage("player_chat", player.PlayerName, @event.Text), JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));
        return HookResult.Continue;
    }

    private void HandleWsMessage(WebSocket ws, string json) {
        try {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("type", out var typeProp)) return;
            var type = typeProp.GetString();
            switch (type) {
                case "chat_platform_to_server": Server.NextFrame(() => HandleChatMessage(json)); break;
                case "external_command_to_server": Server.NextFrame(() => HandleCommandRequest(ws, json)); break;
            }
        } catch (Exception ex) { Logger.LogError($"[Plugin] Failed to handle WS message: {ex.Message}"); }
    }

    private void HandleChatMessage(string json) {
        if (!_config.EnableReceiveGroupMessage) return;
        var msg = JsonSerializer.Deserialize<GroupMessage>(json, JsonOptions);
        if (msg == null) return;
        var formatted = _config.GroupMessageFormat
            .Replace("{group_name}", msg.GroupName)
            .Replace("{group_id}", msg.GroupId)
            .Replace("{nickname}", msg.Nickname)
            .Replace("{message}", msg.Message);
        foreach (var player in Utilities.GetPlayers())
            if (player.IsValid) player.PrintToChat($" {ChatColors.Default}{formatted}");
    }

    private void HandleCommandRequest(WebSocket ws, string json) {
        if (!_config.EnableRemoteExecCommand) return;
        var msg = JsonSerializer.Deserialize<CommandRequestMessage>(json, JsonOptions);
        if (msg == null) return;

        var cmd = msg.Command.Trim();

        if (_config.RemoteExecCommandWhitelist.Length > 0) {
            if (!_config.RemoteExecCommandWhitelist.Any(p => cmd.StartsWith(p, StringComparison.OrdinalIgnoreCase))) {
                SendCommandResult(ws, msg.RequestId, msg.Command, false, null, "Command not in whitelist");
                return;
            }
        }

        var builtInResult = ExecuteBuiltInCommand(cmd);
        if (builtInResult != null) {
            SendCommandResult(ws, msg.RequestId, msg.Command, true, builtInResult, null);
            return;
        }

        try {
            Server.ExecuteCommand(cmd);
            SendCommandResult(ws, msg.RequestId, msg.Command, true, _config.RemoteCommandReturnEmptyResult ? "" : null, null);
        } catch (Exception ex) {
            SendCommandResult(ws, msg.RequestId, msg.Command, false, null, ex.Message);
        }
    }

    private void SendCommandResult(WebSocket ws, string requestId, string command, bool ok, string? result, string? error) {
        var msg = new CommandResultMessage { RequestId = requestId, Command = command, Ok = ok, Result = result, Error = error };
        var json = JsonSerializer.Serialize(msg, JsonOptions);
        _ = Task.Run(() => _wsServer?.SendAsync(ws, json));
    }

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

public class CommandResultMessage {
    public string Type { get; set; } = "command_result";
    public string RequestId { get; set; } = "";
    public string Command { get; set; } = "";
    public bool Ok { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }
}
