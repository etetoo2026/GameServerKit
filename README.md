# GameServerKit

> Reusable C# TCP game server framework — session management, packet routing, and heartbeat out of the box

## What it does
A production-grade foundation for building TCP-based multiplayer game servers in C#. Handles the boilerplate: async socket accept loop, per-client session objects, binary packet framing, opcode routing, and heartbeat/timeout management. Build your game logic on top — don't rewrite the plumbing.

## Quick Start
```bash
git clone https://github.com/yourusername/GameServerKit
cd GameServerKit
dotnet build
dotnet run --project GameServerKit.Demo
```

```csharp
// Define packets
[Packet(0x1001)]
public class LoginPacket : IPacket
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

// Register handlers
server.On<LoginPacket>((session, packet) =>
{
    Console.WriteLine($"Login from {session.RemoteEndPoint}: {packet.Username}");
    session.Send(new LoginResponsePacket { Success = true });
});

// Start
var server = new GameServer(port: 9000);
await server.StartAsync();
```

## Features
- Async `SocketAsyncEventArgs`-based accept + receive loop
- Binary packet framing: `[Length:4][Opcode:2][Body:N]`
- Attribute-based packet routing: `[Packet(opcode)]`
- Per-session state object (extensible via generics)
- Heartbeat manager: auto-disconnect inactive clients
- Thread-safe broadcast: `server.Broadcast(packet)`
- Built-in packet serialization via `BinaryReader`/`BinaryWriter`
- Connection event hooks: `OnConnected`, `OnDisconnected`, `OnError`

## Tech Stack
| Tool | Why |
|------|-----|
| C# / .NET 8 | `SocketAsyncEventArgs` for zero-alloc I/O |
| `System.Reflection` | Opcode → handler mapping at startup |
| `System.Collections.Concurrent` | Thread-safe session registry |

## Architecture
```
GameServerKit/
├── Core/
│   ├── GameServer.cs        # Accept loop + session factory
│   ├── GameSession.cs       # Per-client state + send queue
│   ├── PacketRouter.cs      # Opcode → handler dispatch
│   └── HeartbeatManager.cs  # Timeout detection
├── Packets/
│   ├── IPacket.cs
│   └── PacketAttribute.cs
└── GameServerKit.Demo/      # Echo server + login demo
```
