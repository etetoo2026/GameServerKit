using System.Net.Sockets;

namespace GameServerKit.Core;

public class GameSession
{
    private readonly Socket _socket;
    private readonly Action<GameSession> _onDisconnect;
    private readonly byte[] _buffer = new byte[8192];

    public Guid Id { get; } = Guid.NewGuid();
    public DateTime LastActivity { get; private set; } = DateTime.UtcNow;
    public bool Connected => _socket.Connected;

    public GameSession(Socket socket, Action<GameSession> onDisconnect)
    {
        _socket = socket;
        _onDisconnect = onDisconnect;
    }

    public async Task StartReceiveLoopAsync(PacketRouter router)
    {
        try
        {
            while (Connected)
            {
                // Read 6-byte header: [Length:4][Opcode:2]
                int headerLen = await ReceiveExactAsync(6);
                if (headerLen < 6) break;

                int bodyLen   = BitConverter.ToInt32(_buffer, 0) - 6;
                ushort opcode = BitConverter.ToUInt16(_buffer, 4);

                byte[] body = Array.Empty<byte>();
                if (bodyLen > 0)
                {
                    body = new byte[bodyLen];
                    await ReceiveExactAsync(bodyLen, body);
                }

                LastActivity = DateTime.UtcNow;
                router.Dispatch(this, opcode, body);
            }
        }
        catch { }
        finally { _onDisconnect(this); }
    }

    public void Send<T>(T packet) where T : IPacket
    {
        var body = packet.Serialize();
        var frame = new byte[6 + body.Length];
        BitConverter.TryWriteBytes(frame.AsSpan(0), 6 + body.Length);
        BitConverter.TryWriteBytes(frame.AsSpan(4), packet.Opcode);
        body.CopyTo(frame, 6);
        try { _socket.Send(frame); } catch { }
    }

    public void Disconnect()
    {
        try { _socket.Shutdown(SocketShutdown.Both); } catch { }
        try { _socket.Close(); } catch { }
    }

    private async Task<int> ReceiveExactAsync(int count, byte[]? target = null)
    {
        var buf = target ?? _buffer;
        int total = 0;
        while (total < count)
        {
            var seg = new ArraySegment<byte>(buf, total, count - total);
            int received = await _socket.ReceiveAsync(seg, SocketFlags.None);
            if (received == 0) return total;
            total += received;
        }
        return total;
    }
}
