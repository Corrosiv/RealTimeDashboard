namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Response model for paginated activity feed queries.
/// Uses cursor-based pagination for stable, real-time friendly queries.
/// </summary>
public class PaginatedActivityFeedResponse
{
    /// <summary>
    /// List of activity entries in this page.
    /// </summary>
    public List<ActivityFeedEntryDto> Entries { get; set; } = new();

    /// <summary>
    /// Cursor to fetch the next page of results.
    /// Null if no more results available.
    /// </summary>
    public string? NextCursor { get; set; }

    /// <summary>
    /// Indicates whether more results are available after this page.
    /// </summary>
    public bool HasMore { get; set; }
}
