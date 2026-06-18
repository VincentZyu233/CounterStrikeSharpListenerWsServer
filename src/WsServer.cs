using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace CounterStrikeSharpListenerWsServer;

// WebSocket server using TcpListener + manual HTTP upgrade handshake
public class WsServer {
    private readonly PluginLogger _logger;
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly List<WebSocket> _connections = [];
    private readonly Lock _lock = new();
    private string _token = "";

    public event Action<WebSocket, string>? OnMessageReceived;

    // Store log reference
    public WsServer(PluginLogger logger) { _logger = logger; }

    // Bind TcpListener, spawn background accept loop
    public async Task StartAsync(string host, int port, string token) {
        _token = token;
        _cts = new CancellationTokenSource();
        try {
            var addr = host == "0.0.0.0" ? IPAddress.Any : IPAddress.Parse(host);
            _listener = new TcpListener(addr, port);
            _listener.Start();
            _logger.Info($"[WsServer] Listening on ws://{host}:{port}/");
        } catch (Exception ex) { _logger.Error($"[WsServer] Failed to start: {ex.Message}"); return; }

        var ct = _cts.Token;
        _ = Task.Run(async () => {
            try {
                while (!ct.IsCancellationRequested) {
                    TcpClient tcpClient;
                    try { tcpClient = await _listener.AcceptTcpClientAsync(ct); }
                    catch (OperationCanceledException) { break; }
                    catch (ObjectDisposedException) { break; }
                    _ = Task.Run(() => HandleTcpClientAsync(tcpClient, ct), ct);
                }
            } catch (Exception ex) { _logger.Error($"[WsServer] Accept error: {ex.Message}"); }
            finally { _logger.Info("[WsServer] Accept loop stopped"); }
        }, ct);
    }

    // Cancel accept loop, stop listener, close all client connections
    public async Task StopAsync() {
        if (_cts != null) { await _cts.CancelAsync(); _cts.Dispose(); _cts = null; }
        if (_listener != null) { _listener.Stop(); _listener = null; }

        List<WebSocket> clients;
        lock (_lock) { clients = [.. _connections]; _connections.Clear(); }

        foreach (var ws in clients) {
            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None); } catch { }
            ws.Dispose();
        }
        _logger.Info("[WsServer] Stopped");
    }

    // Broadcast JSON string to every connected WS client (fire-and-forget)
    public async Task BroadcastAsync(string json) {
        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);
        lock (_lock) {
            foreach (var ws in _connections) {
                _ = Task.Run(async () => {
                    try { await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None); }
                    catch {
                        lock (_lock) {
                            _logger.Debug($"[WsServer] Removing dead broadcast client");
                            _connections.Remove(ws);
                        }
                        try { ws.Dispose(); } catch { }
                    }
                });
            }
        }
    }

    // Send JSON to a single specific WS client (for command_result replies)
    public async Task SendAsync(WebSocket ws, string json) {
        var bytes = Encoding.UTF8.GetBytes(json);
        _logger.Trace($"[WsServer] SendAsync: sending {bytes.Length} bytes...");
        try {
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.Trace("[WsServer] SendAsync: sent successfully");
        } catch (Exception ex) {
            _logger.Debug($"[WsServer] SendAsync failed: {ex.Message}");
        }
    }

    // New TCP connection: parse HTTP upgrade request → WebSocket handshake
    private async Task HandleTcpClientAsync(TcpClient tcpClient, CancellationToken ct) {
        try {
            var stream = tcpClient.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);

            // Parse GET /path?token=xxx HTTP/1.1 request line
            var requestLine = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(requestLine)) { tcpClient.Dispose(); return; }

            var parts = requestLine.Split(' ');
            var path = parts.Length > 1 ? parts[1] : "";
            var queryToken = "";
            var qIdx = path.IndexOf('?');
            if (qIdx >= 0) {
                foreach (var p in path[(qIdx + 1)..].Split('&')) {
                    var kv = p.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == "token")
                        queryToken = Uri.UnescapeDataString(kv[1]);
                }
            }

            // Verify token against configured value
            if (!string.IsNullOrEmpty(_token) && queryToken != _token) {
                _logger.Warn($"[WsServer] Token verification failed from {tcpClient.Client.RemoteEndPoint}");
                tcpClient.Dispose();
                return;
            }

            // Extract Sec-WebSocket-Key from HTTP headers
            string? secWebSocketKey = null;
            string? headerLine;
            while (!string.IsNullOrEmpty(headerLine = await reader.ReadLineAsync(ct))) {
                if (headerLine.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                    secWebSocketKey = headerLine["Sec-WebSocket-Key:".Length..].Trim();
            }

            if (secWebSocketKey == null) { tcpClient.Dispose(); return; }

            // Compute Accept = Base64(SHA1(clientKey + WsMagicGuid))
            var acceptKey = ComputeAcceptKey(secWebSocketKey);
            var response = $"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {acceptKey}\r\n\r\n";
            var responseBytes = Encoding.UTF8.GetBytes(response);
            await stream.WriteAsync(responseBytes, ct);
            await stream.FlushAsync(ct);

            // Upgrade to WebSocket, register connection, start receive loop
            var ws = WebSocket.CreateFromStream(stream, true, null, TimeSpan.FromSeconds(30));
            _logger.Info($"[WsServer] Client connected: {tcpClient.Client.RemoteEndPoint}");
            lock (_lock) _connections.Add(ws);
            _logger.Debug($"[WsServer] Total connections: {_connections.Count}");
            await ReceiveLoopAsync(ws, ct);
        } catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.Error($"[WsServer] HandleTcpClient error: {ex.Message}");
        } finally {
            tcpClient.Dispose();
        }
    }

    // Continuous loop: receive WS frames, dispatch text → OnMessageReceived
    private async Task ReceiveLoopAsync(WebSocket ws, CancellationToken ct) {
        var buffer = new byte[16384];
        try {
            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested) {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close) { _logger.Info("[WsServer] Client sent close frame"); break; }
                if (result.MessageType == WebSocketMessageType.Text) {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logger.Debug($"[WsServer] Received message ({json.Length} chars)");
                    OnMessageReceived?.Invoke(ws, json);
                }
            }
        } catch (WebSocketException ex) { _logger.Debug($"[WsServer] WebSocket error: {ex.Message}"); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _logger.Error($"[WsServer] ReceiveLoop error: {ex.Message}"); }
        finally {
            lock (_lock) { _connections.Remove(ws); _logger.Debug($"[WsServer] Total connections: {_connections.Count}"); }
            try {
                if (ws.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Goodbye", CancellationToken.None);
            } catch { }
            ws.Dispose();
            _logger.Info("[WsServer] Client disconnected");
        }
    }

    // RFC 6455 magic GUID for WebSocket key verification
    private static readonly string WsMagicGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    // Compute WebSocket accept key
    private static string ComputeAcceptKey(string clientKey) {
        var combined = clientKey + WsMagicGuid;
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }
}
