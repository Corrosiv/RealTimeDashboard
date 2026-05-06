namespace FinanceTracker.Core.Domain;

/// <summary>
/// Bounded event log for reliable replay on reconnect.
/// Maintains a fixed-size window of events with automatic pruning.
/// </summary>
public class EventLog
{
    /// <summary>
    /// Auto-generated primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reference to the ActivityEvent being logged.
    /// </summary>
    public int ActivityEventId { get; set; }

    /// <summary>
    /// Scope/group identifier for filtering replay requests.
    /// </summary>
    public string Scope { get; set; } = "default";

    /// <summary>
    /// Timestamp when event was logged (UTC).
    /// </summary>
    public DateTime LoggedAt { get; set; }

    /// <summary>
    /// Flag indicating if this entry has been pruned (soft delete for audit).
    /// </summary>
    public bool IsPruned { get; set; }
}
