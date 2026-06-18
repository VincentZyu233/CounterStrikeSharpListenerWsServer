using System.Net.Sockets;
using System.Text;

namespace CounterStrikeSharpListenerWsServer;

// Source RCON protocol client — raw TCP, zero dependencies
public class RconClient : IDisposable {
    private TcpClient? _client;
    private NetworkStream? _stream;

    // TCP connect with cancellation timeout
    public async Task ConnectAsync(string host, int port, int timeoutMs = 5000) {
        _client = new TcpClient();
        using var cts = new CancellationTokenSource(timeoutMs);
        await _client.ConnectAsync(host, port, cts.Token);
        _stream = _client.GetStream();
    }

    // AUTH packet (type=3): send password, wait for AUTH_RESPONSE (type=2)
    public async Task<bool> AuthenticateAsync(string password, int timeoutMs = 3000) {
        var id = Random.Shared.Next();
        await SendPacketAsync(3, id, password);
        using var cts = new CancellationTokenSource(timeoutMs);
        while (!cts.IsCancellationRequested) {
            var (rid, rtype, rbody) = await ReadPacketAsync(cts.Token);
            if (rid == -1) return false;
            if (rtype == 2 && rid == id) return true;
        }
        return false;
    }

    // EXEC command (type=2): send command, read multi-packet RESPONSE (type=0)
    public async Task<string> ExecuteCommandAsync(string command, int timeoutMs = 10000) {
        var id = Random.Shared.Next();
        await SendPacketAsync(2, id, command);
        var sb = new StringBuilder();
        using var cts = new CancellationTokenSource(timeoutMs);
        while (!cts.IsCancellationRequested) {
            var (rid, _, rbody) = await ReadPacketAsync(cts.Token);
            if (rid != id) continue;
            if (rbody.Length == 0) break;
            sb.Append(rbody);
        }
        return sb.ToString().TrimEnd('\n', '\r', ' ');
    }

    // Encode packet: [Size(int32 LE)][ID][Type][Body\0][\0]
    private async Task SendPacketAsync(int type, int id, string body) {
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var size = 10 + bodyBytes.Length;
        using var ms = new MemoryStream(4 + size);
        using var bw = new BinaryWriter(ms);
        bw.Write(size); bw.Write(id); bw.Write(type);
        bw.Write(bodyBytes); bw.Write((byte)0); bw.Write((byte)0);
        bw.Flush();
        await _stream!.WriteAsync(ms.ToArray());
        await _stream.FlushAsync();
    }

    // Read header (12 bytes) + body; parse null-terminated UTF-8 string
    private async Task<(int id, int type, string body)> ReadPacketAsync(CancellationToken ct = default) {
        var header = new byte[12];
        await ReadExactAsync(header, 12, ct);
        var size = BitConverter.ToInt32(header, 0);
        var id = BitConverter.ToInt32(header, 4);
        var type = BitConverter.ToInt32(header, 8);
        var bodyLen = size - 8;
        if (bodyLen <= 0) return (id, type, "");
        var bodyBytes = new byte[bodyLen];
        await ReadExactAsync(bodyBytes, bodyLen, ct);
        var nullIdx = Array.IndexOf(bodyBytes, (byte)0);
        if (nullIdx < 0) nullIdx = bodyBytes.Length;
        return (id, type, Encoding.UTF8.GetString(bodyBytes, 0, nullIdx));
    }

    // Read exactly count bytes from stream (handles partial reads)
    private async Task ReadExactAsync(byte[] buffer, int count, CancellationToken ct) {
        int offset = 0;
        while (offset < count) {
            var read = await _stream!.ReadAsync(buffer.AsMemory(offset, count - offset), ct);
            if (read == 0) throw new EndOfStreamException("RCON connection closed");
            offset += read;
        }
    }

    // Dispose TcpClient and NetworkStream
    public void Dispose() { _stream?.Dispose(); _client?.Dispose(); }
}
