namespace RealTimeDashboard.API.Realtime;

using System.Text.Json.Serialization;

/// <summary>
/// Base envelope for all WebSocket messages.
/// Uses a discriminated union pattern: type + payload.
/// </summary>
public class WebSocketMessage
{
    /// <summary>
    /// Message type (e.g., "Subscribe", "Resume", "ActivityEvent", "Error").
    /// Used by clients to route/handle the message.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// Message payload (type-specific data).
    /// Clients deserialize this based on the Type field.
    /// </summary>
    [JsonPropertyName("payload")]
    public object Payload { get; set; } = null!;
}

/// <summary>
/// Client message: request missed events since lastSeenEventId.
/// </summary>
public class ResumeMessage
{
    [JsonPropertyName("lastSeenEventId")]
    public long LastSeenEventId { get; set; }
}

/// <summary>
/// Server message: replay of missed events is starting.
/// </summary>
public class ReplayStartedMessage
{
    [JsonPropertyName("fromEventId")]
    public long FromEventId { get; set; }
}

/// <summary>
/// Server message: replay of missed events is complete.
/// </summary>
public class ReplayCompletedMessage
{
    [JsonPropertyName("lastEventId")]
    public long LastEventId { get; set; }
}

/// <summary>
/// Represents a replayed or live ActivityEvent in WebSocket format.
/// </summary>
public class ActivityEventMessage
{
    [JsonPropertyName("eventId")]
    public long EventId { get; set; }

    [JsonPropertyName("sequenceId")]
    public long SequenceId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; }

    [JsonPropertyName("actor")]
    public ActorData Actor { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Actor information in WebSocket messages.
/// </summary>
public class ActorData
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

/// <summary>
/// Structured error message for WebSocket protocol errors.
/// NOT HTTP status codes; these are application-level errors.
/// </summary>
public class WebSocketErrorMessage
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Well-known error codes for WebSocket protocol errors.
/// </summary>
public static class WebSocketErrorCodes
{
    public const string ReplayWindowExceeded = "REPLAY_WINDOW_EXCEEDED";
    public const string InvalidResumeRequest = "INVALID_RESUME_REQUEST";
    public const string InvalidMessage = "INVALID_MESSAGE";
    public const string InternalError = "INTERNAL_ERROR";
}
