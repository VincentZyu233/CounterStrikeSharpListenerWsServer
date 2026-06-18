using Microsoft.Extensions.Logging;

namespace CounterStrikeSharpListenerWsServer;

public class PluginLogger {
    private readonly ILogger _inner;
    private readonly LogLevel _minLevel;

    public PluginLogger(ILogger inner, string? level) {
        _inner = inner;
        _minLevel = ParseLevel(level);
    }

    public void Trace(string s) { if (IsEnabled(LogLevel.Trace)) _inner.LogTrace(s); }
    public void Debug(string s) { if (IsEnabled(LogLevel.Debug)) _inner.LogDebug(s); }
    public void Info(string s)  { if (IsEnabled(LogLevel.Information)) _inner.LogInformation(s); }
    public void Warn(string s)  { if (IsEnabled(LogLevel.Warning)) _inner.LogWarning(s); }
    public void Error(string s) { if (IsEnabled(LogLevel.Error)) _inner.LogError(s); }
    public void Fatal(string s) { _inner.LogCritical(s); }

    private bool IsEnabled(LogLevel level) => level >= _minLevel;

    private static LogLevel ParseLevel(string? level) => level?.ToLower() switch {
        "trace" => LogLevel.Trace,
        "debug" => LogLevel.Debug,
        "info" => LogLevel.Information,
        "warn" or "warning" => LogLevel.Warning,
        "error" => LogLevel.Error,
        "fatal" => LogLevel.Critical,
        "silent" or "off" => LogLevel.None,
        _ => LogLevel.Information,
    };
}
