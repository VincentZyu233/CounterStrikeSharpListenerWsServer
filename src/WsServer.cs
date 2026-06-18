using System.Net;
using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CounterStrikeSharpListenerWsServer;

public class WsServer {
    private readonly ILogger _logger;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly List<WebSocket> _connections = [];
    private readonly Lock _lock = new();
    private string _token = "";

    public event Action<string>? OnMessageReceived;

    public WsServer(ILogger logger) { _logger = logger; }

    public async Task StartAsync(string host, int port, string token) {
        _token = token;
        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://{host}:{port}/");
        try {
            _listener.Start();
            _logger.LogInformation($"[WsServer] Listening on http://{host}:{port}/");
        } catch (Exception ex) { _logger.LogError($"[WsServer] Failed to start: {ex.Message}"); return; }

        var ct = _cts.Token;
        _ = Task.Run(async () => {
            try {
                while (!ct.IsCancellationRequested) {
                    var contextTask = _listener.GetContextAsync();
                    try {
                        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));
                        var completed = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, timeoutCts.Token));
                        if (completed != contextTask || ct.IsCancellationRequested) break;
                        _ = Task.Run(() => HandleHttpContextAsync(contextTask.Result, ct), ct);
                    } catch (OperationCanceledException) { break; }
                    catch (Exception ex) { _logger.LogError($"[WsServer] Accept error: {ex.Message}"); break; }
                }
            } finally { _logger.LogInformation("[WsServer] Accept loop stopped"); }
        }, ct);
    }

    public async Task StopAsync() {
        if (_cts != null) { await _cts.CancelAsync(); _cts.Dispose(); _cts = null; }
        if (_listener != null) { _listener.Stop(); _listener.Close(); _listener = null; }

        List<WebSocket> clients;
        lock (_lock) { clients = [.. _connections]; _connections.Clear(); }

        foreach (var ws in clients) {
            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None); } catch { }
            ws.Dispose();
        }
        _logger.LogInformation("[WsServer] Stopped");
    }

    public async Task BroadcastAsync(string json) {
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);
        lock (_lock) {
            foreach (var ws in _connections) {
                _ = Task.Run(async () => {
                    try { await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch { lock (_lock) _connections.Remove(ws); try { ws.Dispose(); } catch { } }
                });
            }
        }
    }

    private async Task HandleHttpContextAsync(HttpListenerContext context, CancellationToken ct) {
        try {
            if (!context.Request.IsWebSocketRequest) { context.Response.StatusCode = 400; context.Response.Close(); return; }

            var queryToken = context.Request.QueryString["token"];
            if (!string.IsNullOrEmpty(_token) && queryToken != _token) {
                _logger.LogWarning($"[WsServer] Token verification failed from {context.Request.RemoteEndPoint}");
                context.Response.StatusCode = 401;
                context.Response.Close();
                return;
            }

            var ws = (await context.AcceptWebSocketAsync(null)).WebSocket;
            _logger.LogInformation($"[WsServer] Client connected: {context.Request.RemoteEndPoint}");
            lock (_lock) _connections.Add(ws);
            await ReceiveLoopAsync(ws, ct);
        } catch (Exception ex) {
            _logger.LogError($"[WsServer] HandleHttpContext error: {ex.Message}");
            context.Response.StatusCode = 500;
            try { context.Response.Close(); } catch { }
        }
    }

    private async Task ReceiveLoopAsync(WebSocket ws, CancellationToken ct) {
        var buffer = new byte[16384];
        try {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested) {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close) { _logger.LogInformation("[WsServer] Client sent close frame"); break; }
                if (result.MessageType == WebSocketMessageType.Text) {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.LogTrace($"[WsServer] Received: {json}");
                    OnMessageReceived?.Invoke(json);
                }
            }
        } catch (WebSocketException ex) { _logger.LogDebug($"[WsServer] WebSocket error: {ex.Message}"); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.LogError($"[WsServer] ReceiveLoop error: {ex.Message}"); }
        finally {
            lock (_lock) _connections.Remove(ws);
            try {
                if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
            } catch { }
            ws.Dispose();
            _logger.LogInformation("[WsServer] Client disconnected");
        }
    }
}
