namespace FinanceTracker.Core.Domain;

/// <summary>
/// Represents a budget constraint for a category.
/// Supports optimistic concurrency via Version field.
/// </summary>
public class Budget
{
    /// <summary>
    /// Unique identifier for this budget.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Reference to the category this budget applies to.
    /// </summary>
    public int CategoryId { get; set; }

    /// <summary>
    /// Maximum spending limit for the period.
    /// </summary>
    public decimal LimitAmount { get; set; }

    /// <summary>
    /// Budget period (e.g., "Monthly", "Yearly").
    /// </summary>
    public string Period { get; set; } = "Monthly";

    /// <summary>
    /// Scope/group identifier (household, account, etc.).
    /// For prototype: assumed single group ("default").
    /// </summary>
    public string Scope { get; set; } = "default";

    /// <summary>
    /// Version number for optimistic concurrency control.
    /// Incremented on each update; used to detect conflicts.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// When this budget was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this budget was last updated (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
