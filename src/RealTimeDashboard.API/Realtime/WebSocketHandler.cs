namespace RealTimeDashboard.API.Realtime;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FinanceTracker.Core.Domain;

/// <summary>
/// Handles WebSocket message receive loop and protocol routing.
/// Implements Subscribe, Resume (replay), and other client-initiated messages.
/// </summary>
public class WebSocketHandler
{
    private readonly WebSocketConnectionManager _manager;
    private readonly WebSocketReplayService _replayService;

    public WebSocketHandler(
        WebSocketConnectionManager manager,
        WebSocketReplayService replayService)
    {
        _manager = manager;
        _replayService = replayService;
    }

    public async Task HandleAsync(string id, WebSocket socket, CancellationToken cancellationToken = default)
    {
        _manager.AddSocket(id, socket);

        try
        {
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(
                    buffer: new ArraySegment<byte>(buffer),
                    cancellationToken: cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closed by client",
                        CancellationToken.None);
                    _manager.RemoveSocket(id);
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    await HandleMessageAsync(id, message, socket, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in WebSocket handler for {id}: {ex.Message}");
            _manager.RemoveSocket(id);
        }
    }

    /// <summary>
    /// Processes an incoming message from a client.
    /// Routes based on message type (Resume, Subscribe, etc.).
    /// </summary>
    private async Task HandleMessageAsync(string connectionId, string messageJson, WebSocket socket, CancellationToken cancellationToken)
    {
        try
        {
            // Deserialize message envelope
            var envelope = JsonSerializer.Deserialize<WebSocketMessage>(messageJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (envelope == null || string.IsNullOrEmpty(envelope.Type))
            {
                await SendErrorAsync(socket, WebSocketErrorCodes.InvalidMessage, "Message must have a 'type' field", cancellationToken);
                return;
            }

            switch (envelope.Type.ToLowerInvariant())
            {
                case "resume":
                    await HandleResumeAsync(connectionId, envelope, socket, cancellationToken);
                    break;

                case "subscribe":
                    await HandleSubscribeAsync(connectionId, socket, cancellationToken);
                    break;

                case "ping":
                    await HandlePingAsync(socket, cancellationToken);
                    break;

                default:
                    await SendErrorAsync(socket, WebSocketErrorCodes.InvalidMessage, $"Unknown message type: {envelope.Type}", cancellationToken);
                    break;
            }
        }
        catch (JsonException ex)
        {
            await SendErrorAsync(socket, WebSocketErrorCodes.InvalidMessage, $"Failed to parse message: {ex.Message}", cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling message: {ex.Message}");
            await SendErrorAsync(socket, WebSocketErrorCodes.InvalidMessage, "An internal error occurred", cancellationToken);
        }
    }

    /// <summary>
    /// Handles Resume messages: client reconnecting after disconnection.
    /// Sends ReplayStarted, then missed events, then ReplayCompleted.
    /// </summary>
    private async Task HandleResumeAsync(string connectionId, WebSocketMessage envelope, WebSocket socket, CancellationToken cancellationToken)
    {
        try
        {
            // Parse Resume payload
            var payload = envelope.Payload as System.Text.Json.JsonElement?;
            if (!payload.HasValue)
            {
                await SendErrorAsync(socket, WebSocketErrorCodes.InvalidResumeRequest, "Resume message must have a payload", cancellationToken);
                return;
            }

            if (!payload.Value.TryGetProperty("lastSeenEventId", out var lastSeenElement) ||
                !lastSeenElement.TryGetInt64(out long lastSeenEventId))
            {
                await SendErrorAsync(socket, WebSocketErrorCodes.InvalidResumeRequest, "Resume must include 'lastSeenEventId' as a number", cancellationToken);
                return;
            }

            // Update connection's last-seen event ID
            _manager.UpdateLastSeenEventId(connectionId, lastSeenEventId);

            // Fetch events from the replay service
            var replayResult = await _replayService.GetEventsForReplayAsync(lastSeenEventId, scope: "default");

            if (!replayResult.IsSuccess)
            {
                // Replay window exceeded or other error
                await SendMessageAsync(socket, new WebSocketMessage
                {
                    Type = "Error",
                    Payload = replayResult.Error!
                }, cancellationToken);
                return;
            }

            var events = replayResult.Events;

            // Send ReplayStarted
            await SendMessageAsync(socket, new WebSocketMessage
            {
                Type = "ReplayStarted",
                Payload = new ReplayStartedMessage
                {
                    FromEventId = events.Count > 0 ? events.First().SequenceId : lastSeenEventId
                }
            }, cancellationToken);

            // Send each missed event
            foreach (var @event in events)
            {
                var eventMessage = ConvertActivityEventToMessage(@event);
                await SendMessageAsync(socket, new WebSocketMessage
                {
                    Type = "ActivityEvent",
                    Payload = eventMessage
                }, cancellationToken);

                // Update the connection's last-seen ID as we stream events
                _manager.UpdateLastSeenEventId(connectionId, @event.SequenceId);
            }

            // Send ReplayCompleted
            await SendMessageAsync(socket, new WebSocketMessage
            {
                Type = "ReplayCompleted",
                Payload = new ReplayCompletedMessage
                {
                    LastEventId = events.Count > 0 ? events.Last().SequenceId : lastSeenEventId
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling Resume: {ex.Message}");
            await SendErrorAsync(socket, WebSocketErrorCodes.InternalError, "Failed to process resume request", cancellationToken);
        }
    }

    /// <summary>
    /// Handles Subscribe messages: client wants to receive future activity events.
    /// </summary>
    private async Task HandleSubscribeAsync(string connectionId, WebSocket socket, CancellationToken cancellationToken)
    {
        try
        {
            // Mark connection as subscribed
            var connection = _manager.GetConnection(connectionId);
            if (connection != null)
            {
                connection.IsSubscribed = true;
            }

            // Send confirmation (optional, could also just start receiving events)
            await SendMessageAsync(socket, new WebSocketMessage
            {
                Type = "SubscriptionConfirmed",
                Payload = new { }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling Subscribe: {ex.Message}");
            await SendErrorAsync(socket, WebSocketErrorCodes.InternalError, "Failed to subscribe", cancellationToken);
        }
    }

    /// <summary>
    /// Handles Ping messages: simple heartbeat for keeping connection alive.
    /// </summary>
    private async Task HandlePingAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        try
        {
            await SendMessageAsync(socket, new WebSocketMessage
            {
                Type = "Pong",
                Payload = new { timestamp = DateTime.UtcNow }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling Ping: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a message to the client.
    /// </summary>
    private static async Task SendMessageAsync(WebSocket socket, WebSocketMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var bytes = Encoding.UTF8.GetBytes(json);

            if (socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends an error message to the client.
    /// </summary>
    private static async Task SendErrorAsync(WebSocket socket, string code, string message, CancellationToken cancellationToken)
    {
        await SendMessageAsync(socket, new WebSocketMessage
        {
            Type = "Error",
            Payload = new WebSocketErrorMessage
            {
                Code = code,
                Message = message
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Converts a domain ActivityEvent to a WebSocket ActivityEventMessage.
    /// </summary>
    private static ActivityEventMessage ConvertActivityEventToMessage(ActivityEvent @event)
    {
        return new ActivityEventMessage
        {
            EventId = @event.Id,
            SequenceId = @event.SequenceId,
            Type = @event.Type,
            OccurredAtUtc = @event.CreatedAt,
            Actor = new ActorData
            {
                Username = @event.Actor.Username,
                DisplayName = @event.Actor.DisplayName
            },
            Message = @event.Message,
            Metadata = @event.Metadata != null 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(@event.Metadata)
                : null
        };
    }
}
