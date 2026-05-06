namespace RealTimeDashboard.API.Infrastructure;

using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Manages the bounded event log for WebSocket reconnect and replay.
/// </summary>
public class EventLogService : IEventLogService
{
    private readonly FinanceDbContext _db;
    private const int MaxLogSize = 10000;  // Keep last 10k events
    private const int DefaultRetentionDays = 7;

    public EventLogService(FinanceDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<EventLog> AppendAsync(ActivityEvent activityEvent, string scope = "default")
    {
        var eventLog = new EventLog
        {
            ActivityEventId = activityEvent.Id,
            Scope = scope,
            LoggedAt = DateTime.UtcNow,
            IsPruned = false
        };

        _db.EventLogs.Add(eventLog);
        await _db.SaveChangesAsync();

        // Automatically prune if log is too large
        var count = await _db.EventLogs
            .Where(el => el.Scope == scope && !el.IsPruned)
            .CountAsync();

        if (count > MaxLogSize)
        {
            await PruneAsync(DefaultRetentionDays, scope);
        }

        return eventLog;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ActivityEvent>> GetEventsForReplayAsync(long afterSequenceId, string scope = "default", int limit = 1000)
    {
        var events = await _db.EventLogs
            .Where(el => el.Scope == scope && !el.IsPruned)
            .Include(el => new { ActivityEvent = el })
            .OrderBy(el => el.LoggedAt)
            .ToListAsync();

        // Get the corresponding ActivityEvents
        var eventIds = events.Select(el => el.ActivityEventId).ToList();
        var activityEvents = await _db.ActivityEvents
            .Where(ae => eventIds.Contains(ae.Id) && ae.SequenceId > afterSequenceId)
            .OrderBy(ae => ae.SequenceId)
            .Take(limit)
            .ToListAsync();

        return activityEvents;
    }

    /// <inheritdoc/>
    public async Task<int> PruneAsync(int retentionDays = 7, string? scope = null)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var query = _db.EventLogs
            .Where(el => !el.IsPruned && el.LoggedAt < cutoffDate);

        if (scope != null)
        {
            query = query.Where(el => el.Scope == scope);
        }

        var entriesToPrune = await query.ToListAsync();

        foreach (var entry in entriesToPrune)
        {
            entry.IsPruned = true;
        }

        await _db.SaveChangesAsync();
        return entriesToPrune.Count;
    }

    /// <inheritdoc/>
    public async Task<long> GetLatestSequenceIdAsync(string scope = "default")
    {
        var latestEvent = await _db.ActivityEvents
            .Where(ae => ae.Scope == scope)
            .OrderByDescending(ae => ae.SequenceId)
            .FirstOrDefaultAsync();

        return latestEvent?.SequenceId ?? 0;
    }

    /// <inheritdoc/>
    public async Task<bool> IsSequenceIdAvailableAsync(long sequenceId, string scope = "default")
    {
        var isPruned = await _db.EventLogs
            .Where(el => el.Scope == scope && el.IsPruned)
            .Select(el => el.ActivityEventId)
            .ToListAsync();

        var event_ = await _db.ActivityEvents
            .Where(ae => ae.SequenceId == sequenceId && ae.Scope == scope)
            .FirstOrDefaultAsync();

        if (event_ == null)
            return false;

        return !isPruned.Contains(event_.Id);
    }
}
