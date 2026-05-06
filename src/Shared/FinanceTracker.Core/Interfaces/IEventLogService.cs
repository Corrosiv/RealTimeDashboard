namespace FinanceTracker.Core.Interfaces;

using FinanceTracker.Core.Domain;

/// <summary>
/// Manages the bounded event log for reliable WebSocket replay on reconnect.
/// </summary>
public interface IEventLogService
{
    /// <summary>
    /// Appends an event to the log.
    /// </summary>
    /// <param name="activityEvent">The event to log.</param>
    /// <param name="scope">Scope/group identifier for filtering.</param>
    /// <returns>The logged EventLog entry with its ID.</returns>
    Task<EventLog> AppendAsync(ActivityEvent activityEvent, string scope = "default");

    /// <summary>
    /// Retrieves events after a given sequence ID for replay.
    /// </summary>
    /// <param name="afterSequenceId">Return events with seq_id > this value.</param>
    /// <param name="scope">Scope/group to filter by.</param>
    /// <param name="limit">Maximum number of events to return.</param>
    /// <returns>Ordered list of events for replay.</returns>
    Task<IEnumerable<ActivityEvent>> GetEventsForReplayAsync(long afterSequenceId, string scope = "default", int limit = 1000);

    /// <summary>
    /// Prunes old events to keep the log bounded.
    /// Removes events older than the retention period.
    /// </summary>
    /// <param name="retentionDays">Keep events from the last N days.</param>
    /// <param name="scope">Scope to prune (null = all scopes).</param>
    /// <returns>Number of events pruned.</returns>
    Task<int> PruneAsync(int retentionDays = 7, string? scope = null);

    /// <summary>
    /// Gets the latest sequence ID in the log for a scope.
    /// Used for clients to know their starting point for replay.
    /// </summary>
    /// <param name="scope">Scope/group identifier.</param>
    /// <returns>The highest sequence ID, or 0 if empty.</returns>
    Task<long> GetLatestSequenceIdAsync(string scope = "default");

    /// <summary>
    /// Checks if a sequence ID is still within the replay window.
    /// </summary>
    /// <param name="sequenceId">The sequence ID to check.</param>
    /// <param name="scope">Scope to check.</param>
    /// <returns>True if ID is within window; false if pruned/too old.</returns>
    Task<bool> IsSequenceIdAvailableAsync(long sequenceId, string scope = "default");
}
