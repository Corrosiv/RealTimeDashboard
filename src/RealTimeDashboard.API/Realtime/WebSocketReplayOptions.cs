namespace RealTimeDashboard.API.Realtime;

/// <summary>
/// Configuration options for WebSocket replay and resilience.
/// Controls the bounded event log for client reconnect scenarios.
/// </summary>
public class WebSocketReplayOptions
{
    /// <summary>
    /// Maximum number of events to retain for replay.
    /// When exceeded, oldest events are pruned.
    /// Default: 10000 events
    /// </summary>
    public int MaxReplayEvents { get; set; } = 10000;

    /// <summary>
    /// Maximum age of events to retain for replay (in minutes).
    /// Events older than this are pruned regardless of count.
    /// Default: 60 minutes (1 hour)
    /// </summary>
    public int MaxReplayAgeMinutes { get; set; } = 60;

    /// <summary>
    /// Gets the maximum age as a TimeSpan.
    /// </summary>
    public TimeSpan MaxReplayAge => TimeSpan.FromMinutes(MaxReplayAgeMinutes);
}
