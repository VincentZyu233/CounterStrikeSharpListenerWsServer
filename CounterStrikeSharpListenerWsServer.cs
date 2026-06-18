using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace CounterStrikeSharpListenerWsServer;

public class CounterStrikeSharpListenerWsServer : BasePlugin
{
    public override string ModuleName => "CounterStrikeSharp Listener WS Server";
    public override string ModuleVersion => "0.1.0";

    private WsServer? _wsServer;
    private PluginConfig _config = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public override void Load(bool hotReload)
    {
        var configPath = Path.Combine(ModuleDirectory, "config.json");
        _config = PluginConfig.Load(configPath);

        _wsServer = new WsServer(Logger);
        _wsServer.OnMessageReceived += HandleWsMessage;
        _ = _wsServer.StartAsync(_config.Host, _config.Port, _config.WsToken);

        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerChat>(OnPlayerChat);

        Logger.LogInformation($"[Plugin] WS Server started on {_config.Host}:{_config.Port}");
    }

    public override void Unload(bool hotReload)
    {
        _wsServer?.StopAsync().Wait();
        Logger.LogInformation("[Plugin] WS Server stopped");
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        if (!_config.EnablePlayerJoinBroadcast)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || string.IsNullOrEmpty(player.PlayerName))
            return HookResult.Continue;

        var msg = new PlayerEventMessage { Type = "player_join", PlayerName = player.PlayerName };
        var json = JsonSerializer.Serialize(msg, JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (!_config.EnablePlayerLeaveBroadcast)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null || !player.IsValid || string.IsNullOrEmpty(player.PlayerName))
            return HookResult.Continue;

        var msg = new PlayerEventMessage { Type = "player_leave", PlayerName = player.PlayerName };
        var json = JsonSerializer.Serialize(msg, JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));

        return HookResult.Continue;
    }

    private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
    {
        if (!_config.EnablePlayerChatBroadcast)
            return HookResult.Continue;

        var player = Utilities.GetPlayerFromUserid(@event.Userid);
        if (player == null || !player.IsValid || string.IsNullOrEmpty(player.PlayerName))
            return HookResult.Continue;

        var msg = new PlayerChatMessage
        {
            Type = "player_chat",
            PlayerName = player.PlayerName,
            Content = @event.Text
        };
        var json = JsonSerializer.Serialize(msg, JsonOptions);
        _ = Task.Run(() => _wsServer?.BroadcastAsync(json));

        return HookResult.Continue;
    }

    private void HandleWsMessage(string json)
    {
        if (!_config.EnableReceiveGroupMessage)
            return;

        try
        {
            var msg = JsonSerializer.Deserialize<GroupMessage>(json, JsonOptions);
            if (msg == null)
                return;

            if (msg.Type == "chat_platform_to_server")
            {
                var formatted = _config.GroupMessageFormat
                    .Replace("{group_name}", msg.GroupName)
                    .Replace("{group_id}", msg.GroupId)
                    .Replace("{nickname}", msg.Nickname)
                    .Replace("{message}", msg.Message);

                foreach (var player in Utilities.GetPlayers())
                {
                    if (player.IsValid)
                        player.PrintToChat($" {ChatColors.Default}{formatted}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"[Plugin] Failed to handle WS message: {ex.Message}");
        }
    }
}

// Message types matching the cross-server WS protocol

public class PlayerEventMessage
{
    public string Type { get; set; } = "";
    public string PlayerName { get; set; } = "";
}

public class PlayerChatMessage
{
    public string Type { get; set; } = "";
    public string PlayerName { get; set; } = "";
    public string Content { get; set; } = "";
}

public class GroupMessage
{
    public string Type { get; set; } = "";
    public string GroupId { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string Message { get; set; } = "";
}
