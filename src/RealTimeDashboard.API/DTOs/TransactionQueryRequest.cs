namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Request model for querying transactions with filtering, pagination, and sorting.
/// </summary>
public class TransactionQueryRequest
{
    /// <summary>
    /// Filter: transactions on or after this date (UTC). Format: ISO 8601 (e.g., "2026-04-01").
    /// </summary>
    public DateTime? DateFrom { get; set; }

    /// <summary>
    /// Filter: transactions on or before this date (UTC). Format: ISO 8601.
    /// </summary>
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// Filter: minimum transaction amount (inclusive).
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Filter: maximum transaction amount (inclusive).
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Filter: transactions in this category id.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Filter: transaction type/source (e.g., "CSV", "Manual", "API").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Filter: text search in transaction description.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Cursor for pagination. Base64 encoded (timestamp + id).
    /// Omit on first request.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Maximum number of transactions to return. Default: 20, Max: 100.
    /// </summary>
    public int Limit { get; set; } = 20;

    /// <summary>
    /// Sort order: "timestamp:asc" or "timestamp:desc" (default: "timestamp:desc").
    /// </summary>
    public string Sort { get; set; } = "timestamp:desc";
}
