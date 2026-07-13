// Skanly.Infrastructure/RealTime/ConnectionTracker.cs
using System.Collections.Concurrent;

namespace Skanly.Infrastructure.RealTime;

/// <summary>
/// Thread-safe in-memory store mapping UserId → set of ConnectionIds.
/// One user can have multiple connections (browser tabs, mobile).
/// Registered as Singleton — shared across all Hub instances.
///
/// Phase 2 upgrade: replace the ConcurrentDictionary with a Redis
/// HSET structure. The interface stays identical.
/// </summary>
public class ConnectionTracker
{
    // userId → set of active connectionIds
    private readonly ConcurrentDictionary<string, HashSet<string>> _connections
        = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _lock = new();

    public void Add(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(userId, out var connections))
            {
                connections = new HashSet<string>(StringComparer.Ordinal);
                _connections[userId] = connections;
            }
            connections.Add(connectionId);
        }
    }

    public void Remove(string userId, string connectionId)
    {
        lock (_lock)
        {
            if (!_connections.TryGetValue(userId, out var connections)) return;
            connections.Remove(connectionId);
            if (connections.Count == 0)
                _connections.TryRemove(userId, out _);
        }
    }

    public bool IsOnline(string userId)
        => _connections.TryGetValue(userId, out var conns) && conns.Count > 0;

    public IReadOnlyList<string> GetConnectionIds(string userId)
    {
        if (_connections.TryGetValue(userId, out var conns))
            lock (_lock) return conns.ToList();
        return Array.Empty<string>();
    }

    public IReadOnlyList<string> GetOnlineUsers()
        => _connections.Keys.ToList();

    public int GetOnlineCount()
        => _connections.Count;
}