namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Request model for creating a new transaction.
/// </summary>
public class CreateTransactionRequest
{
    /// <summary>
    /// When the transaction occurred (UTC). Format: ISO 8601 (e.g., "2026-04-07T15:00:00Z").
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Transaction amount (positive for expense, negative for income per convention).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (e.g., "USD", "EUR").
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// User-friendly transaction description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category ID for grouping this transaction.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Username of the user creating this transaction.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Optional: Source/type of transaction (e.g., "Manual", "API"). Defaults to "Manual".
    /// </summary>
    public string? Source { get; set; }
}
