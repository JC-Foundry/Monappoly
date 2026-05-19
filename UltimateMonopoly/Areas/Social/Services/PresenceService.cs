using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using UltimateMonopoly.Data;

namespace UltimateMonopoly.Areas.Social.Services;

public class PresenceService
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastActive = new();
    private readonly ConcurrentDictionary<string, byte> _dirty = new();
    private readonly ConcurrentDictionary<string, byte> _dbMissed = new();

    public PresenceService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void AddConnection(string userId, string connectionId)
    {
        var set = _connections.GetOrAdd(userId, _ => []);
        lock (set)
        {
            set.Add(connectionId);
        }
        MarkActive(userId);
    }

    public void RemoveConnection(string userId, string connectionId)
    {
        if (_connections.TryGetValue(userId, out var set))
        {
            lock (set)
            {
                set.Remove(connectionId);
                if (set.Count == 0)
                    _connections.TryRemove(userId, out _);
            }
        }
        MarkActive(userId);
    }

    public void MarkActive(string userId)
    {
        _lastActive[userId] = DateTime.UtcNow;
        _dirty[userId] = 0;
        _dbMissed.TryRemove(userId, out _);
    }

    public bool IsOnline(string userId) =>
        _connections.TryGetValue(userId, out var set) && set.Count > 0;

    public DateTime? PeekLastActiveUtc(string userId) =>
        _lastActive.TryGetValue(userId, out var ts) ? ts : null;

    public async Task<DateTime?> GetLastActiveUtcAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (_lastActive.TryGetValue(userId, out var ts))
            return ts;

        if (_dbMissed.ContainsKey(userId))
            return null;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fromDb = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.LastActiveUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (fromDb.HasValue)
            _lastActive.TryAdd(userId, fromDb.Value);
        else
            _dbMissed.TryAdd(userId, 0);

        return fromDb;
    }

    public async Task<IReadOnlyDictionary<string, DateTime?>> GetLastActiveUtcAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, DateTime?>(userIds.Count);
        var missing = new List<string>();

        foreach (var id in userIds)
        {
            if (_lastActive.TryGetValue(id, out var ts))
                result[id] = ts;
            else if (_dbMissed.ContainsKey(id))
                result[id] = null;
            else
                missing.Add(id);
        }

        if (missing.Count == 0)
            return result;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fromDb = await db.Users
            .Where(u => missing.Contains(u.Id))
            .Select(u => new { u.Id, u.LastActiveUtc })
            .ToListAsync(cancellationToken);

        var foundIds = new HashSet<string>(fromDb.Count);
        foreach (var row in fromDb)
        {
            foundIds.Add(row.Id);
            if (row.LastActiveUtc.HasValue)
            {
                _lastActive.TryAdd(row.Id, row.LastActiveUtc.Value);
                result[row.Id] = row.LastActiveUtc;
            }
            else
            {
                _dbMissed.TryAdd(row.Id, 0);
                result[row.Id] = null;
            }
        }

        foreach (var id in missing)
        {
            if (!foundIds.Contains(id))
            {
                _dbMissed.TryAdd(id, 0);
                result[id] = null;
            }
        }

        return result;
    }

    public IReadOnlyDictionary<string, DateTime> DrainDirty()
    {
        var snapshot = new Dictionary<string, DateTime>();
        foreach (var userId in _dirty.Keys.ToArray())
        {
            if (_dirty.TryRemove(userId, out _)
                && _lastActive.TryGetValue(userId, out var ts))
            {
                snapshot[userId] = ts;
            }
        }
        return snapshot;
    }
}