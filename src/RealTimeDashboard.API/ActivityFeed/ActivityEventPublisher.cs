namespace RealTimeDashboard.API.ActivityFeed;

public class ActivityEventPublisher
{
    // TODO: implement publishing of ActivityEvent to WebSocket manager and persistence
    // Responsibilities:
    // - Accept ActivityEvent domain objects or DTOs
    // - Persist events via an ActivityEventStore or repository (API-level adapter)
    // - Broadcast events to connected clients using WebSocketConnectionManager
    // - Provide durable delivery / retry logic as needed
}
