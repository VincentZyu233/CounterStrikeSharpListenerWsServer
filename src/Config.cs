using System.Text.Json;

namespace CounterStrikeSharpListenerWsServer;

public class PluginConfig {
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 60606;
    public string WsToken { get; set; } = "test12345";
    public bool EnablePlayerJoinBroadcast { get; set; } = true;
    public bool EnablePlayerLeaveBroadcast { get; set; } = true;
    public bool EnablePlayerChatBroadcast { get; set; } = true;
    public bool EnableReceiveGroupMessage { get; set; } = true;
    public string GroupMessageFormat { get; set; } = "[{group_name}]({group_id}) {nickname}: {message}";

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
