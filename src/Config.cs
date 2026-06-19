using System.Text.Json;

namespace CounterStrikeSharpListenerWsServer;

// ===== Plugin configuration =====
public class PluginConfig {
    // ===== Logging =====
    public string _comment_logLevel { get; set; } = "Log level: silent | fatal | error | warn | info | debug | trace";
    public string logLevel { get; set; } = "info";

    // ===== WebSocket Network =====
    public string _comment_host { get; set; } = "WebSocket server listening address; 0.0.0.0 means listen on all network interfaces";
    public string Host { get; set; } = "0.0.0.0";

    public string _comment_port { get; set; } = "WebSocket server listening port";
    public int Port { get; set; } = 60618;

    public string _comment_wsToken { get; set; } = "Token required for client connection; empty string means no validation";
    public string WsToken { get; set; } = "test12345";

    // ===== Broadcast Toggles =====
    public string _comment_enablePlayerJoinBroadcast { get; set; } = "Whether to broadcast notification to chat platform when player joins";
    public bool EnablePlayerJoinBroadcast { get; set; } = true;

    public string _comment_enablePlayerLeaveBroadcast { get; set; } = "Whether to broadcast notification to chat platform when player leaves";
    public bool EnablePlayerLeaveBroadcast { get; set; } = true;

    public string _comment_enablePlayerChatBroadcast { get; set; } = "Whether to broadcast in-game chat messages to chat platform";
    public bool EnablePlayerChatBroadcast { get; set; } = true;

    public string _comment_enableReceiveGroupMessage { get; set; } = "Whether to receive chat platform messages and forward to in-game";
    public bool EnableReceiveGroupMessage { get; set; } = true;

    // ===== Message Formatting =====
    public string _comment_groupMessageFormat { get; set; } = "Group message display format in-game; placeholders: {group_name} {group_id} {nickname} {message}";
    public string GroupMessageFormat { get; set; } = "[{group_name}]({group_id}) {nickname}: {message}";

    // ===== Bot / Player labels =====
    public string _comment_botSuffix { get; set; } = "Bot player name suffix; empty string means no marker";
    public string BotSuffix { get; set; } = " (bot)";

    public string _comment_playerSuffix { get; set; } = "Human player name suffix; empty string means no marker";
    public string PlayerSuffix { get; set; } = " (player)";

    public string _comment_playerBroadcastScope { get; set; } = "Player join/leave broadcast scope: player (players only) | bot (bots only) | both (both)";
    public string PlayerBroadcastScope { get; set; } = "player";

    // ===== Remote command execution =====
    public string _comment_execCommandMode { get; set; } = "Remote command execution mode: disabled | csharp-native (engine, no output) | rcon-relay (RCON, with output)";
    public string ExecCommandMode { get; set; } = "disabled";

    public string _comment_remoteExecCommandWhitelist { get; set; } = "Remote command prefix whitelist; empty list means no restriction (e.g. [\"mp_\", \"kick\", \"say \"])";
    public string[] RemoteExecCommandWhitelist { get; set; } = [];

    public string _comment_remoteExecCommandTimeoutSec { get; set; } = "Remote command execution timeout (seconds)";
    public int RemoteExecCommandTimeoutSec { get; set; } = 10;

    public string _comment_remoteCommandReturnEmptyResult { get; set; } = "When engine command has no output: true=return empty string, false=omit result field";
    public bool RemoteCommandReturnEmptyResult { get; set; } = true;

    public string _comment_rconHost { get; set; } = "RCON connection address (default 127.0.0.1). Note: Some Linux distros (mainly in Debian/Ubuntu based) CS2 RCON may bind to 127.0.1.1 instead (see /etc/hosts hostname mapping). Test with nc -zv <ip> <port> first; if refused try 127.0.1.1. To force RCON onto all interfaces, add -ip 0.0.0.0 to cs2ds.sh — not recommended for security";
    public string RconHost { get; set; } = "127.0.0.1";

    public string _comment_rconPort { get; set; } = "RCON port (i.e. CS2 game port, default 27015)";
    public int RconPort { get; set; } = 27015;

    public string _comment_rconPassword { get; set; } = "RCON password (must match rcon_password in server.cfg)";
    public string RconPassword { get; set; } = "test67890";

    public string _comment_rconTimeoutMs { get; set; } = "RCON connection/auth/execution timeout (milliseconds)";
    public int RconTimeoutMs { get; set; } = 5000;

    // ===== Load config =====
    public static PluginConfig Load(string path) {
        if (!File.Exists(path)) {
            var cfg = new PluginConfig();
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            return cfg;
        }
        return JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(path)) ?? new PluginConfig();
    }
}
