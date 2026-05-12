namespace RealTimeDashboard.API.Realtime;

using System.Net.WebSockets;

/// <summary>
/// Tracks state for a single WebSocket connection.
/// Maintains the last-seen event ID for replay purposes.
/// </summary>
public class ClientConnection
{
    /// <summary>
    /// Unique identifier for this connection.
    /// </summary>
    public string ConnectionId { get; set; } = null!;

    /// <summary>
    /// The underlying WebSocket for this client.
    /// </summary>
    public WebSocket Socket { get; set; } = null!;

    /// <summary>
    /// The client's username (if authenticated via Subscribe message).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// The last event ID the client has seen.
    /// Used to determine which events to replay on reconnect.
    /// Initialized to 0 (no events seen yet).
    /// </summary>
    public long LastSeenEventId { get; set; } = 0;

    /// <summary>
    /// Time when this connection was established.
    /// </summary>
    public DateTime ConnectedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the client has successfully subscribed (sent a Subscribe message).
    /// </summary>
    public bool IsSubscribed { get; set; } = false;
}
