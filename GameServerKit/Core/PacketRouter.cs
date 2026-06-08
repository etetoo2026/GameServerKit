using System.Reflection;

namespace GameServerKit.Core;

public class PacketRouter
{
    private readonly Dictionary<ushort, Action<GameSession, byte[]>> _handlers = new();

    public void On<T>(Action<GameSession, T> handler) where T : IPacket, new()
    {
        var attr = typeof(T).GetCustomAttribute<PacketAttribute>()
            ?? throw new InvalidOperationException($"{typeof(T).Name} missing [Packet] attribute");

        _handlers[attr.Opcode] = (session, data) =>
        {
            var packet = new T();
            packet.Deserialize(data);
            handler(session, packet);
        };
    }

    public void Dispatch(GameSession session, ushort opcode, byte[] data)
    {
        if (_handlers.TryGetValue(opcode, out var handler))
            handler(session, data);
        else
            Console.WriteLine($"[Router] Unhandled opcode 0x{opcode:X4}");
    }
}
