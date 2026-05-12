namespace RealTimeDashboard.API.Realtime;

using FinanceTracker.Core.Interfaces;
using Microsoft.Extensions.Options;

/// <summary>
/// Manages replay of missed events for reconnecting clients.
/// Enforces replay window limits and provides structured errors.
/// </summary>
public class WebSocketReplayService
{
    private readonly IEventLogService _eventLogService;
    private readonly WebSocketReplayOptions _options;

    public WebSocketReplayService(IEventLogService eventLogService, IOptions<WebSocketReplayOptions> options)
    {
        _eventLogService = eventLogService;
        _options = options.Value;
    }

    /// <summary>
    /// Fetches events for replay since the given event ID.
    /// Enforces replay window limits (count and age).
    /// Returns either events or a structured error.
    /// </summary>
    public async Task<ReplayResult> GetEventsForReplayAsync(long lastSeenEventId, string scope = "default")
    {
        // Fetch events since lastSeenEventId, respecting the max count limit
        var events = await _eventLogService.GetEventsForReplayAsync(
            afterSequenceId: lastSeenEventId,
            scope: scope,
            limit: _options.MaxReplayEvents
        );

        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            // No new events to replay
            return new ReplayResult { IsSuccess = true, Events = eventList };
        }

        // Check if oldest event exceeds age limit
        var oldestEvent = eventList.First();
        var eventAge = DateTime.UtcNow - oldestEvent.CreatedAt;

        if (eventAge > _options.MaxReplayAge)
        {
            // Replay window exceeded: oldest event is too old
            return new ReplayResult
            {
                IsSuccess = false,
                Error = new WebSocketErrorMessage
                {
                    Code = WebSocketErrorCodes.ReplayWindowExceeded,
                    Message = $"Requested events are outside the replay window. Maximum replay age is {_options.MaxReplayAgeMinutes} minutes.",
                    Details = new Dictionary<string, object>
                    {
                        { "maxReplayAgeMinutes", _options.MaxReplayAgeMinutes },
                        { "oldestEventAgeMinutes", (int)eventAge.TotalMinutes },
                        { "oldestEventId", oldestEvent.SequenceId }
                    }
                }
            };
        }

        // If there are more events available than MaxReplayEvents, indicate truncation
        if (eventList.Count >= _options.MaxReplayEvents)
        {
            return new ReplayResult
            {
                IsSuccess = false,
                Error = new WebSocketErrorMessage
                {
                    Code = WebSocketErrorCodes.ReplayWindowExceeded,
                    Message = $"Too many missed events. Maximum replay event count is {_options.MaxReplayEvents}. Client must reconnect to clear backlog.",
                    Details = new Dictionary<string, object>
                    {
                        { "maxReplayEvents", _options.MaxReplayEvents },
                        { "eventsRequested", _options.MaxReplayEvents }
                    }
                }
            };
        }

        return new ReplayResult { IsSuccess = true, Events = eventList };
    }
}

/// <summary>
/// Result of a replay request.
/// Either contains events OR an error, never both.
/// </summary>
public class ReplayResult
{
    /// <summary>
    /// Whether the replay request was successful.
    /// If true, Events is populated. If false, Error is populated.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Events to replay (only if IsSuccess = true).
    /// </summary>
    public IList<FinanceTracker.Core.Domain.ActivityEvent> Events { get; set; } = new List<FinanceTracker.Core.Domain.ActivityEvent>();

    /// <summary>
    /// Error details (only if IsSuccess = false).
    /// </summary>
    public WebSocketErrorMessage? Error { get; set; }
}
