using Microsoft.Extensions.Logging;

namespace CounterStrikeSharpListenerWsServer;

// Lightweight log wrapper with configurable level filtering
public class PluginLogger {
    private readonly ILogger _inner;
    private readonly LogLevel _minLevel;

    // Wrap CSS ILogger, parse configured logLevel string
    public PluginLogger(ILogger inner, string? level) {
        _inner = inner;
        _minLevel = ParseLevel(level);
    }

    // Debug/Trace fallback: if Serilog rejects, downgrade to Info with [level] prefix
    public void Trace(string s) {
        if (!IsEnabled(LogLevel.Trace)) return;
        if (_inner.IsEnabled(LogLevel.Trace)) _inner.LogTrace(s);
        else _inner.LogInformation($"[Trace] {s}");
    }
    public void Debug(string s) {
        if (!IsEnabled(LogLevel.Debug)) return;
        if (_inner.IsEnabled(LogLevel.Debug)) _inner.LogDebug(s);
        else _inner.LogInformation($"[Debug] {s}");
    }
    public void Info(string s)  { if (IsEnabled(LogLevel.Information)) _inner.LogInformation(s); }
    public void Warn(string s)  { if (IsEnabled(LogLevel.Warning)) _inner.LogWarning(s); }
    public void Error(string s) { if (IsEnabled(LogLevel.Error)) _inner.LogError(s); }
    public void Fatal(string s) { _inner.LogCritical(s); }

    // True if requested log level meets configured minimum
    private bool IsEnabled(LogLevel level) => level >= _minLevel;

    // Map config string to Microsoft.Extensions.Logging.LogLevel
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
