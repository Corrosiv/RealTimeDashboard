namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Request model for querying the activity feed with filtering and pagination.
/// </summary>
public class ActivityFeedQueryRequest
{
    /// <summary>
    /// Filter: activities created by this username (exact match, case-insensitive).
    /// Optional.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Filter: activity event type (e.g., "TransactionCreated", "CsvUploaded", "MetricsUpdated").
    /// Multiple values can be provided as comma-separated string.
    /// Optional.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Filter: activities on or after this timestamp (UTC). Format: ISO 8601.
    /// Optional.
    /// </summary>
    public DateTime? Since { get; set; }

    /// <summary>
    /// Filter: activities on or before this timestamp (UTC). Format: ISO 8601.
    /// Optional.
    /// </summary>
    public DateTime? Until { get; set; }

    /// <summary>
    /// Filter: activities related to this resource ID (e.g., transaction ID).
    /// Optional, for future-proofing.
    /// </summary>
    public int? ResourceId { get; set; }

    /// <summary>
    /// Cursor for pagination. Base64 encoded (occurredAtUtc + eventId).
    /// Omit on first request.
    /// </summary>
    public string? Cursor { get; set; }

    /// <summary>
    /// Maximum number of entries to return. Default: 50, Max: 200.
    /// </summary>
    public int Limit { get; set; } = 50;
}
