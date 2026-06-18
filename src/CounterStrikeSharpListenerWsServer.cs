using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CounterStrikeSharpListenerWsServer;

public class CounterStrikeSharpListenerWsServer : BasePlugin {
    public override string ModuleName => "CounterStrikeSharp Listener WS Server";
    public override string ModuleVersion => "0.2.0";

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

    private HookResult BroadcastPlayerEvent(string playerName, string type, bool enabled) {
        if (!enabled || string.IsNullOrEmpty(playerName)) return HookResult.Continue;
        var json = JsonSerializer.Serialize(new PlayerEventMessage(type, playerName), JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));
        return HookResult.Continue;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info) =>
        BroadcastPlayerEvent(@event.Userid?.PlayerName ?? "", "player_join", _config.EnablePlayerJoinBroadcast);

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info) =>
        BroadcastPlayerEvent(@event.Name, "player_leave", _config.EnablePlayerLeaveBroadcast);

    private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info) {
        if (!_config.EnablePlayerChatBroadcast) return HookResult.Continue;
        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid || string.IsNullOrEmpty(player.PlayerName)) return HookResult.Continue;
        var json = JsonSerializer.Serialize(new PlayerChatMessage("player_chat", player.PlayerName, @event.Text), JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));
        return HookResult.Continue;
    }

    private void HandleWsMessage(string json) {
        if (!_config.EnableReceiveGroupMessage) return;
        try {
            var msg = JsonSerializer.Deserialize<GroupMessage>(json, JsonOptions);
            if (msg == null || msg.Type != "chat_platform_to_server") return;
            var formatted = _config.GroupMessageFormat
                .Replace("{group_name}", msg.GroupName)
                .Replace("{group_id}", msg.GroupId)
                .Replace("{nickname}", msg.Nickname)
                .Replace("{message}", msg.Message);
            foreach (var player in Utilities.GetPlayers())
                if (player.IsValid) player.PrintToChat($" {ChatColors.Default}{formatted}");
        } catch (Exception ex) { Logger.LogError($"[Plugin] Failed to handle WS message: {ex.Message}"); }
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
