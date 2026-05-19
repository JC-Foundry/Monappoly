using JC.Core.Models;
using Microsoft.EntityFrameworkCore;
using UltimateMonopoly.Data;

namespace UltimateMonopoly.Areas.Social.Services;

public class PresenceFlushJob : IBackgroundJob
{
    private readonly PresenceService _presence;
    private readonly AppDbContext _db;
    private readonly ILogger<PresenceFlushJob> _logger;

    public PresenceFlushJob(
        PresenceService presence,
        AppDbContext db,
        ILogger<PresenceFlushJob> logger)
    {
        _presence = presence;
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var dirty = _presence.DrainDirty();
        if (dirty.Count == 0)
            return;

        var userIds = dirty.Keys.ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            if (dirty.TryGetValue(user.Id, out var ts))
                user.LastActiveUtc = ts;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Flushed last-active timestamps for {Count} users",
            users.Count);
    }
}