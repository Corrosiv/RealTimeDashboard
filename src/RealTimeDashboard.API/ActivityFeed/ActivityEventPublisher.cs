namespace RealTimeDashboard.API.ActivityFeed;

using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;
using RealTimeDashboard.API.Realtime;
using System.Text.Json;

/// <summary>
/// Publishes ActivityEvents to both persistence and real-time clients.
/// Single source of truth for event delivery.
/// Ensures events are persisted before broadcasting to avoid loss.
/// </summary>
public class ActivityEventPublisher
{
    private readonly IEventLogService _eventLogService;
    private readonly WebSocketConnectionManager _connectionManager;

    public ActivityEventPublisher(IEventLogService eventLogService, WebSocketConnectionManager connectionManager)
    {
        _eventLogService = eventLogService;
        _connectionManager = connectionManager;
    }

    /// <summary>
    /// Publishes an ActivityEvent to both persistence and real-time clients.
    /// 
    /// Flow:
    /// 1. Persist the event (gets SequenceId)
    /// 2. Convert to WebSocket message format
    /// 3. Broadcast to all subscribed clients
    /// 
    /// If persistence fails, exception is thrown (no partial delivery).
    /// If broadcast fails, individual client failures are logged but do not prevent other broadcasts.
    /// </summary>
    public async Task PublishAsync(ActivityEvent activityEvent, string scope = "default")
    {
        // Step 1: Persist the event (this assigns SequenceId)
        var eventLog = await _eventLogService.AppendAsync(activityEvent, scope);

        // Step 2: Convert to WebSocket message format
        var wsMessage = ConvertToWebSocketMessage(activityEvent);

        // Step 3: Broadcast to all subscribed clients (fire-and-forget)
        _connectionManager.BroadcastToAll(wsMessage);
    }

    /// <summary>
    /// Converts an ActivityEvent to a WebSocket message for transmission.
    /// </summary>
    private static WebSocketMessage ConvertToWebSocketMessage(ActivityEvent activityEvent)
    {
        var payload = new ActivityEventMessage
        {
            EventId = activityEvent.Id,
            SequenceId = activityEvent.SequenceId,
            Type = activityEvent.Type,
            OccurredAtUtc = activityEvent.CreatedAt,
            Actor = new ActorData
            {
                Username = activityEvent.Actor.Username,
                DisplayName = activityEvent.Actor.DisplayName
            },
            Message = activityEvent.Message,
            Metadata = activityEvent.Metadata != null 
                ? JsonDocument.Parse(activityEvent.Metadata).RootElement.Deserialize<Dictionary<string, object>>()
                : null
        };

        return new WebSocketMessage
        {
            Type = "ActivityEvent",
            Payload = payload
        };
    }
}
