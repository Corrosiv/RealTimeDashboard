namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Response model for paginated transaction queries.
/// Uses cursor-based pagination for stability in real-time environments.
/// </summary>
public class PaginatedTransactionResponse
{
    /// <summary>
    /// List of transactions in this page.
    /// </summary>
    public List<TransactionDto> Data { get; set; } = new();

    /// <summary>
    /// Cursor to fetch the next page of results.
    /// Null if no more results available.
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Total count of transactions matching the filter criteria.
    /// Note: This is computed on each request for accuracy in real-time scenarios.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of transactions returned in this page.
    /// </summary>
    public int Count { get; set; }
}
