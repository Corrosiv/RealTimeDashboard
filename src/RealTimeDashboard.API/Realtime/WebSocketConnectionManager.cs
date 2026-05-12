namespace RealTimeDashboard.API.Realtime;

using System.Collections.Concurrent;
using System.Net.WebSockets;

/// <summary>
/// Manages all connected WebSocket clients and their state.
/// Responsible for:
/// - Tracking active connections by ID
/// - Broadcasting events to all connected clients
/// - Maintaining per-client replay cursor (lastSeenEventId)
/// </summary>
public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, ClientConnection> _connections = new();

    /// <summary>
    /// Adds a new WebSocket connection and allocates client state.
    /// </summary>
    public void AddSocket(string id, WebSocket socket)
    {
        var connection = new ClientConnection
        {
            ConnectionId = id,
            Socket = socket,
            ConnectedAtUtc = DateTime.UtcNow
        };
        _connections[id] = connection;
    }

    /// <summary>
    /// Removes a WebSocket connection and cleans up its state.
    /// </summary>
    public void RemoveSocket(string id)
    {
        _connections.TryRemove(id, out _);
    }

    /// <summary>
    /// Gets all active connections (for debugging/monitoring).
    /// </summary>
    public IEnumerable<KeyValuePair<string, ClientConnection>> GetAll() => _connections;

    /// <summary>
    /// Gets a specific connection by ID.
    /// </summary>
    public ClientConnection? GetConnection(string id)
    {
        _connections.TryGetValue(id, out var connection);
        return connection;
    }

    /// <summary>
    /// Updates the last-seen event ID for a connection.
    /// Called after successfully sending an event to a client.
    /// </summary>
    public void UpdateLastSeenEventId(string connectionId, long eventId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            connection.LastSeenEventId = eventId;
        }
    }

    /// <summary>
    /// Gets the last-seen event ID for a connection.
    /// Returns 0 if connection not found or not subscribed.
    /// </summary>
    public long GetLastSeenEventId(string connectionId)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
        {
            return connection.LastSeenEventId;
        }
        return 0;
    }

    /// <summary>
    /// Broadcasts a message to all connected and subscribed clients.
    /// Does NOT await; fire-and-forget pattern for real-time events.
    /// </summary>
    public void BroadcastToAll(WebSocketMessage message)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(message);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);

        foreach (var kvp in _connections)
        {
            if (kvp.Value.IsSubscribed && kvp.Value.Socket.State == WebSocketState.Open)
            {
                _ = SendToConnectionAsync(kvp.Key, buffer);
            }
        }
    }

    /// <summary>
    /// Sends a message to a specific connection.
    /// Awaitable for cases where we need to ensure delivery.
    /// </summary>
    public async Task SendToConnectionAsync(string connectionId, byte[] buffer)
    {
        if (_connections.TryGetValue(connectionId, out var connection) && 
            connection.Socket.State == WebSocketState.Open)
        {
            try
            {
                await connection.Socket.SendAsync(
                    new ArraySegment<byte>(buffer),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    CancellationToken.None
                );
            }
            catch
            {
                // If send fails, the connection will be cleaned up by the handler
            }
        }
    }
}
