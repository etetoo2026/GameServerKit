using GameServerKit.Core;
using System.Text;

[Packet(0x1001)]
public class LoginPacket : IPacket
{
    public ushort Opcode => 0x1001;
    public string Username { get; set; } = "";

    public byte[] Serialize()
    {
        var bytes = Encoding.UTF8.GetBytes(Username);
        var result = new byte[2 + bytes.Length];
        BitConverter.TryWriteBytes(result, (ushort)bytes.Length);
        bytes.CopyTo(result, 2);
        return result;
    }

    public void Deserialize(byte[] data)
    {
        int len = BitConverter.ToUInt16(data, 0);
        Username = Encoding.UTF8.GetString(data, 2, len);
    }
}

class Program
{
    static async Task Main()
    {
        var server = new GameServer(port: 9000);
        server.OnConnected    += s => Console.WriteLine($"[+] Connected:    {s.Id}");
        server.OnDisconnected += s => Console.WriteLine($"[-] Disconnected: {s.Id}");

        server.Router.On<LoginPacket>((session, pkt) =>
            Console.WriteLine($"[Login] Username={pkt.Username} from {session.Id}"));

        Console.WriteLine("GameServerKit Demo — listening on port 9000");
        Console.WriteLine("Press Ctrl+C to stop.");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        await server.StartAsync(cts.Token);
        server.Stop();
    }
}
