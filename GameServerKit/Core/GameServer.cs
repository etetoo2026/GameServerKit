using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GameServerKit.Core;

public class GameServer
{
    private readonly int _port;
    private Socket? _listener;
    private bool _running;
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();
    private readonly PacketRouter _router = new();
    private readonly HeartbeatManager _heartbeat;

    public event Action<GameSession>? OnConnected;
    public event Action<GameSession>? OnDisconnected;

    public GameServer(int port, int heartbeatIntervalMs = 30000)
    {
        _port = port;
        _heartbeat = new HeartbeatManager(_sessions, heartbeatIntervalMs, OnSessionTimeout);
    }

    public PacketRouter Router => _router;

    public async Task StartAsync(CancellationToken ct = default)
    {
        _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listener.Bind(new IPEndPoint(IPAddress.Any, _port));
        _listener.Listen(128);
        _running = true;
        _heartbeat.Start();

        Console.WriteLine($"[GameServer] Listening on port {_port}");

        while (_running && !ct.IsCancellationRequested)
        {
            try
            {
                var socket = await _listener.AcceptAsync(ct);
                var session = new GameSession(socket, OnSessionDisconnected);
                _sessions[session.Id] = session;
                OnConnected?.Invoke(session);
                _ = session.StartReceiveLoopAsync(_router);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) { Console.WriteLine($"[GameServer] Accept error: {ex.Message}"); }
        }
    }

    public void Broadcast<T>(T packet) where T : IPacket
    {
        foreach (var session in _sessions.Values)
            session.Send(packet);
    }

    public void Stop()
    {
        _running = false;
        _heartbeat.Stop();
        _listener?.Close();
        foreach (var s in _sessions.Values) s.Disconnect();
        _sessions.Clear();
    }

    private void OnSessionDisconnected(GameSession session)
    {
        _sessions.TryRemove(session.Id, out _);
        OnDisconnected?.Invoke(session);
    }

    private void OnSessionTimeout(GameSession session)
    {
        Console.WriteLine($"[GameServer] Session {session.Id} timed out");
        session.Disconnect();
    }
}
