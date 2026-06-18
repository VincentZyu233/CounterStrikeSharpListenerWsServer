using System.Text.Json;

namespace CounterStrikeSharpListenerWsServer;

public class PluginConfig {
    public string _comment_logLevel { get; set; } = "📋 日志等级：silent | fatal | error | warn | info | debug | trace";
    public string logLevel { get; set; } = "info";

    public string _comment_host { get; set; } = "🌐 WebSocket 服务器监听地址，0.0.0.0 表示监听所有网卡";
    public string Host { get; set; } = "0.0.0.0";

    public string _comment_port { get; set; } = "🔌 WebSocket 服务器监听端口";
    public int Port { get; set; } = 60618;

    public string _comment_wsToken { get; set; } = "🔑 客户端连接时需要提供的 Token，空字符串表示不校验";
    public string WsToken { get; set; } = "test12345";

    public string _comment_enablePlayerJoinBroadcast { get; set; } = "🚪 玩家加入服务器时是否广播通知到聊天平台";
    public bool EnablePlayerJoinBroadcast { get; set; } = true;

    public string _comment_enablePlayerLeaveBroadcast { get; set; } = "🚶 玩家离开服务器时是否广播通知到聊天平台";
    public bool EnablePlayerLeaveBroadcast { get; set; } = true;

    public string _comment_enablePlayerChatBroadcast { get; set; } = "💬 玩家在游戏内聊天时是否广播到聊天平台";
    public bool EnablePlayerChatBroadcast { get; set; } = true;

    public string _comment_enableReceiveGroupMessage { get; set; } = "📥 是否接收聊天平台消息并转发到游戏内";
    public bool EnableReceiveGroupMessage { get; set; } = true;

    public string _comment_groupMessageFormat { get; set; } = "✏️ 群消息在游戏内的显示格式，占位符：{group_name} {group_id} {nickname} {message}";
    public string GroupMessageFormat { get; set; } = "[{group_name}]({group_id}) {nickname}: {message}";

    public string _comment_botSuffix { get; set; } = "🤖 Bot 玩家名字后缀，设为空字符串则不标记";
    public string BotSuffix { get; set; } = " (bot)";

    public string _comment_playerSuffix { get; set; } = "🧑 人类玩家名字后缀，设为空字符串则不标记";
    public string PlayerSuffix { get; set; } = " (player)";

    public string _comment_enableRemoteExecCommand { get; set; } = "⚡ 启用远程命令执行（需同时开启 Koishi 侧 enableExecCommand）";
    public bool EnableRemoteExecCommand { get; set; } = false;

    public string _comment_remoteExecCommandWhitelist { get; set; } = "🛡️ 远程命令前缀白名单，空列表表示不限制（如：[\"mp_\", \"kick\", \"say \"]）";
    public string[] RemoteExecCommandWhitelist { get; set; } = [];

    public string _comment_remoteExecCommandTimeoutSec { get; set; } = "⏱️ 远程命令执行超时（秒）";
    public int RemoteExecCommandTimeoutSec { get; set; } = 10;

    public string _comment_remoteCommandReturnEmptyResult { get; set; } = "📄 引擎命令无输出时：true=返回空字符串，false=不返回 result 字段";
    public bool RemoteCommandReturnEmptyResult { get; set; } = true;

    public static PluginConfig Load(string path) {
        if (!File.Exists(path)) {
            var cfg = new PluginConfig();
            var dir = Path.GetDirectoryName(path);
            if (dir != null) Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
            return cfg;
        }
        return JsonSerializer.Deserialize<PluginConfig>(File.ReadAllText(path)) ?? new PluginConfig();
    }
}
