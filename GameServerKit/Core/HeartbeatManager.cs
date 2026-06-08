using System.Collections.Concurrent;

namespace GameServerKit.Core;

public class HeartbeatManager
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions;
    private readonly int _intervalMs;
    private readonly Action<GameSession> _onTimeout;
    private CancellationTokenSource? _cts;

    public HeartbeatManager(ConcurrentDictionary<Guid, GameSession> sessions,
        int intervalMs, Action<GameSession> onTimeout)
    {
        _sessions = sessions;
        _intervalMs = intervalMs;
        _onTimeout = onTimeout;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(_intervalMs, token);
                var cutoff = DateTime.UtcNow.AddMilliseconds(-_intervalMs * 2);
                foreach (var session in _sessions.Values)
                    if (session.LastActivity < cutoff)
                        _onTimeout(session);
            }
        }, token);
    }

    public void Stop() => _cts?.Cancel();
}
