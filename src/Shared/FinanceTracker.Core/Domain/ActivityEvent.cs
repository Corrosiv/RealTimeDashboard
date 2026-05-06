namespace FinanceTracker.Core.Domain;

/// <summary>
/// Represents a single event in the activity feed and event log.
/// Designed for reliable, replayable event sourcing with opt-out subscriptions.
/// </summary>
public class ActivityEvent
{
    /// <summary>
    /// Auto-generated primary key (required by EF Core).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique identifier for this event (immutable, for external reference).
    /// </summary>
    public string EventId { get; set; } = null!;

    /// <summary>
    /// Monotonically increasing sequence number for ordering and replay.
    /// </summary>
    public long SequenceId { get; set; }

    /// <summary>
    /// Event type (e.g., "TransactionCreated", "BudgetExceeded", "CsvImported").
    /// </summary>
    public string Type { get; set; } = null!;

    /// <summary>
    /// The primary entity affected by this event (e.g., transaction ID, budget ID).
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Scope/group identifier for filtering (e.g., household ID, account ID).
    /// For prototype: assumed single group, all events in same scope.
    /// </summary>
    public string Scope { get; set; } = "default";

    /// <summary>
    /// Structured actor information (non-null actor).
    /// </summary>
    public ActorInfo Actor { get; set; } = null!;

    /// <summary>
    /// Human-friendly summary (no IDs, family-friendly text).
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Timestamp when event was created (UTC, immutable).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Optional version number for mutable state tracking (optimistic concurrency).
    /// Null if event is immutable.
    /// </summary>
    public int? Version { get; set; }

    /// <summary>
    /// Freeform metadata as JSON (e.g., original transaction data, error details).
    /// </summary>
    public string? Metadata { get; set; }
}

/// <summary>
/// Structured representation of an actor (user) in the system.
/// </summary>
public class ActorInfo
{
    /// <summary>
    /// Unique username or identifier.
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// Optional display name (may differ from username).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Timestamp when actor joined or was last seen.
    /// </summary>
    public DateTime JoinedAt { get; set; }
}
