namespace FinanceTracker.Core.Interfaces;

using FinanceTracker.Core.Domain;

/// <summary>
/// Creates properly-formatted ActivityEvents with correct sequence IDs and payloads.
/// Critical for event sourcing and reliable replay.
/// </summary>
public interface IActivityEventFactory
{
    /// <summary>
    /// Creates an event for a transaction creation.
    /// </summary>
    /// <param name="transaction">The created transaction.</param>
    /// <param name="actor">The user who performed the action.</param>
    /// <param name="sequenceId">The auto-incremented sequence ID for ordering.</param>
    /// <returns>A properly-formatted ActivityEvent with no sensitive IDs.</returns>
    ActivityEvent CreateTransactionCreatedEvent(Transaction transaction, ActorInfo actor, long sequenceId);

    /// <summary>
    /// Creates an event for a CSV import completion.
    /// </summary>
    /// <param name="importedCount">Number of transactions successfully imported.</param>
    /// <param name="actor">The user who performed the import.</param>
    /// <param name="sequenceId">The sequence ID for ordering.</param>
    /// <param name="metadata">Optional metadata (error count, duration, etc.).</param>
    /// <returns>A properly-formatted ActivityEvent.</returns>
    ActivityEvent CreateCsvImportedEvent(int importedCount, ActorInfo actor, long sequenceId, string? metadata = null);

    /// <summary>
    /// Creates an event for a budget exceeded alert.
    /// </summary>
    /// <param name="budget">The budget that was exceeded.</param>
    /// <param name="currentSpent">Current spending in the period.</param>
    /// <param name="sequenceId">The sequence ID for ordering.</param>
    /// <returns>A properly-formatted ActivityEvent.</returns>
    ActivityEvent CreateBudgetExceededEvent(Budget budget, decimal currentSpent, long sequenceId);

    /// <summary>
    /// Creates a generic event.
    /// </summary>
    /// <param name="eventType">Event type (e.g., "TransactionCreated", "BudgetExceeded").</param>
    /// <param name="entityId">Primary entity ID.</param>
    /// <param name="message">Human-friendly summary (no IDs).</param>
    /// <param name="actor">The actor.</param>
    /// <param name="sequenceId">Sequence ID for ordering.</param>
    /// <param name="version">Optional version for mutable state tracking.</param>
    /// <param name="metadata">Optional JSON metadata.</param>
    /// <returns>A properly-formatted ActivityEvent.</returns>
    ActivityEvent CreateEvent(
        string eventType,
        int entityId,
        string message,
        ActorInfo actor,
        long sequenceId,
        int? version = null,
        string? metadata = null);
}
