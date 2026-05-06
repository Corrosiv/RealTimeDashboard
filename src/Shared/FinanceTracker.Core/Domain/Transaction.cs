namespace FinanceTracker.Core.Domain;

/// <summary>
/// Represents a single financial transaction (expense or income).
/// Designed for immutability; updates create new events, not mutations.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Unique identifier for this transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// When the transaction occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Transaction amount (positive for expense, negative for income per convention).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD", "EUR"). Use consistent currency for prototype.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// User-friendly description (no sensitive info).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Reference to Category for grouping and budget tracking.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Scope/group identifier (household, account, etc.).
    /// For prototype: assumed single group ("default").
    /// </summary>
    public string Scope { get; set; } = "default";

    /// <summary>
    /// Username who created this transaction.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when transaction was created in the system (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Deterministic hash for deduplication and idempotency.
    /// Generated from (timestamp, amount, description) for replay safety.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Source of transaction (e.g., "CSV", "Manual", "API").
    /// </summary>
    public string Source { get; set; } = "Manual";
}
