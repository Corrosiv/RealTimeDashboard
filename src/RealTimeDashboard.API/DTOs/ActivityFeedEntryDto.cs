namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Response entry for a single activity feed item.
/// </summary>
public class ActivityFeedEntryDto
{
    /// <summary>
    /// Unique event identifier.
    /// </summary>
    public string EventId { get; set; } = null!;

    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    public DateTime OccurredAtUtc { get; set; }

    /// <summary>
    /// Event type (e.g., "TransactionCreated", "CsvUploaded", "MetricsUpdated").
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// Username or actor that triggered the event.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Related resource ID (e.g., transaction ID).
    /// Optional.
    /// </summary>
    public int? ResourceId { get; set; }

    /// <summary>
    /// Human-friendly message describing the event.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Freeform metadata payload (deserialized from JSON).
    /// </summary>
    public Dictionary<string, object>? Payload { get; set; }
}
