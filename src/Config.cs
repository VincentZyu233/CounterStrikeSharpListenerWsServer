using System.Text.Json;

namespace CounterStrikeSharpListenerWsServer;

// ===== Plugin configuration =====
public class PluginConfig {
    // ===== Logging =====
    public string _comment_logLevel { get; set; } = "日志等级：silent | fatal | error | warn | info | debug | trace";
    public string logLevel { get; set; } = "info";

    // ===== WebSocket Network =====
    public string _comment_host { get; set; } = "WebSocket 服务器监听地址，0.0.0.0 表示监听所有网卡";
    public string Host { get; set; } = "0.0.0.0";

    public string _comment_port { get; set; } = "WebSocket 服务器监听端口";
    public int Port { get; set; } = 60618;

    public string _comment_wsToken { get; set; } = "客户端连接时需要提供的 Token，空字符串表示不校验";
    public string WsToken { get; set; } = "test12345";

    // ===== Broadcast Toggles =====
    public string _comment_enablePlayerJoinBroadcast { get; set; } = "玩家加入服务器时是否广播通知到聊天平台";
    public bool EnablePlayerJoinBroadcast { get; set; } = true;

    public string _comment_enablePlayerLeaveBroadcast { get; set; } = "玩家离开服务器时是否广播通知到聊天平台";
    public bool EnablePlayerLeaveBroadcast { get; set; } = true;

    public string _comment_enablePlayerChatBroadcast { get; set; } = "玩家在游戏内聊天时是否广播到聊天平台";
    public bool EnablePlayerChatBroadcast { get; set; } = true;

    public string _comment_enableReceiveGroupMessage { get; set; } = "是否接收聊天平台消息并转发到游戏内";
    public bool EnableReceiveGroupMessage { get; set; } = true;

    // ===== Message Formatting =====
    public string _comment_groupMessageFormat { get; set; } = "群消息在游戏内的显示格式，占位符：{group_name} {group_id} {nickname} {message}";
    public string GroupMessageFormat { get; set; } = "[{group_name}]({group_id}) {nickname}: {message}";

    // ===== Bot / Player labels =====
    public string _comment_botSuffix { get; set; } = "Bot 玩家名字后缀，设为空字符串则不标记";
    public string BotSuffix { get; set; } = " (bot)";

    public string _comment_playerSuffix { get; set; } = "人类玩家名字后缀，设为空字符串则不标记";
    public string PlayerSuffix { get; set; } = " (player)";

    public string _comment_playerBroadcastScope { get; set; } = "玩家进出事件广播范围：player(仅玩家) | bot(仅Bot) | both(两者都广播)";
    public string PlayerBroadcastScope { get; set; } = "player";

    // ===== Remote command execution =====
    public string _comment_execCommandMode { get; set; } = "远程指令执行模式：disabled(关闭) | csharp-native(引擎执行，无输出) | rcon-relay(RCON 回显，有输出)";
    public string ExecCommandMode { get; set; } = "disabled";

    public string _comment_remoteExecCommandWhitelist { get; set; } = "远程命令前缀白名单，空列表表示不限制（如：[\"mp_\", \"kick\", \"say \"]）";
    public string[] RemoteExecCommandWhitelist { get; set; } = [];

    public string _comment_remoteExecCommandTimeoutSec { get; set; } = "远程命令执行超时（秒）";
    public int RemoteExecCommandTimeoutSec { get; set; } = 10;

    public string _comment_remoteCommandReturnEmptyResult { get; set; } = "引擎命令无输出时：true=返回空字符串，false=不返回 result 字段";
    public bool RemoteCommandReturnEmptyResult { get; set; } = true;

    public string _comment_rconHost { get; set; } = "RCON 连接地址（默认 127.0.0.1）。注意：部分 Linux 发行版 CS2 RCON 会绑定到 127.0.1.1（/etc/hosts 主机名映射），请用 nc -zv <ip> <port> 测试 TCP 可达性";
    public string RconHost { get; set; } = "127.0.0.1";

    public string _comment_rconPort { get; set; } = "RCON 端口（即 CS2 游戏端口，默认 27015）";
    public int RconPort { get; set; } = 27015;

    public string _comment_rconPassword { get; set; } = "RCON 密码（需与 server.cfg 的 rcon_password 一致）";
    public string RconPassword { get; set; } = "test67890";

    public string _comment_rconTimeoutMs { get; set; } = "RCON 连接/认证/执行超时（毫秒）";
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
