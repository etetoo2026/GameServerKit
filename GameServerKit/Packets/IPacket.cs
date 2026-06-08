namespace GameServerKit.Core;

public interface IPacket
{
    ushort Opcode { get; }
    byte[] Serialize();
    void Deserialize(byte[] data);
}

[AttributeUsage(AttributeTargets.Class)]
public class PacketAttribute(ushort opcode) : Attribute
{
    public ushort Opcode { get; } = opcode;
}
