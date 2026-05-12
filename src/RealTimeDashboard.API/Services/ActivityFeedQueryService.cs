using Microsoft.EntityFrameworkCore;
using FinanceTracker.Core.Domain;
using RealTimeDashboard.API.DTOs;
using RealTimeDashboard.API.Infrastructure;
using System.Text.Json;

namespace RealTimeDashboard.API.Services;

/// <summary>
/// Service for querying the activity feed with filtering, pagination, and sorting.
/// Encapsulates complex query logic away from the controller.
/// 
/// Pagination: Cursor-based, sorted by (CreatedAt DESC, Id DESC) for stable ordering across inserts.
/// Filters: CreatedBy, EventType, Since, Until, ResourceId (all optional).
/// </summary>
public class ActivityFeedQueryService
{
    private readonly FinanceDbContext _db;
    private readonly CursorService _cursorService;
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    public ActivityFeedQueryService(FinanceDbContext db, CursorService cursorService)
    {
        _db = db;
        _cursorService = cursorService;
    }

    /// <summary>
    /// Queries activity feed events based on filter and pagination criteria.
    /// Returns a paginated response with cursor-based navigation.
    /// 
    /// Sort order: CreatedAt DESC, Id DESC (newest first with stable tie-breaking).
    /// </summary>
    public async Task<PaginatedActivityFeedResponse> QueryActivityFeedAsync(
        ActivityFeedQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        // Normalize and validate limit
        var limit = Math.Min(request.Limit, MaxLimit);
        limit = Math.Max(limit, 1);

        var query = _db.ActivityEvents.AsQueryable();

        // Apply filters
        query = ApplyFilters(query, request);

        // Apply cursor-based pagination (must be before sorting to use correct sort for cursor)
        query = ApplyCursorPagination(query, request);

        // Apply sorting: newest first (CreatedAt DESC), then by Id DESC for stable tie-breaking
        query = query.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id);

        // Fetch one extra record to determine if there's a next page
        var events = await query
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = events.Count > limit;
        if (hasMore)
        {
            events = events.Take(limit).ToList();
        }

        // Convert to DTOs
        var dtos = events.Select(e => ConvertToDto(e)).ToList();

        // Generate next cursor
        string? nextCursor = null;
        if (hasMore && events.Count > 0)
        {
            var lastEvent = events[^1];
            // Cursor encodes (timestamp, id) for stable pagination
            nextCursor = _cursorService.EncodeCursor(lastEvent.CreatedAt, lastEvent.Id);
        }

        return new PaginatedActivityFeedResponse
        {
            Entries = dtos,
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    /// <summary>
    /// Applies filter conditions to the activity event query.
    /// All filters are optional and combined with AND logic.
    /// </summary>
    private IQueryable<ActivityEvent> ApplyFilters(
        IQueryable<ActivityEvent> query,
        ActivityFeedQueryRequest request)
    {
        // CreatedBy filter (case-insensitive)
        if (!string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            var createdByLower = request.CreatedBy.ToLower();
            query = query.Where(e => e.Actor.Username.ToLower() == createdByLower);
        }

        // EventType filter (supports multiple types as comma-separated string)
        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var eventTypes = request.EventType
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (eventTypes.Count > 0)
            {
                query = query.Where(e => eventTypes.Contains(e.Type));
            }
        }

        // Since filter (inclusive)
        if (request.Since.HasValue)
        {
            query = query.Where(e => e.CreatedAt >= request.Since.Value);
        }

        // Until filter (inclusive)
        if (request.Until.HasValue)
        {
            query = query.Where(e => e.CreatedAt <= request.Until.Value);
        }

        // ResourceId filter (EntityId in domain model)
        if (request.ResourceId.HasValue)
        {
            query = query.Where(e => e.EntityId == request.ResourceId.Value);
        }

        return query;
    }

    /// <summary>
    /// Applies cursor-based pagination.
    /// The cursor represents (CreatedAt, Id) of the last item from the previous page.
    /// For descending order, we fetch items that are "after" the cursor (earlier in time).
    /// </summary>
    private IQueryable<ActivityEvent> ApplyCursorPagination(
        IQueryable<ActivityEvent> query,
        ActivityFeedQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Cursor))
        {
            return query;
        }

        var decodedCursor = _cursorService.DecodeCursor(request.Cursor);
        if (!decodedCursor.HasValue)
        {
            // Invalid cursor, start from beginning
            return query;
        }

        var (cursorTimestamp, cursorId) = decodedCursor.Value;

        // For descending order (CreatedAt DESC), we want events BEFORE the cursor
        // (i.e., with earlier timestamps or same timestamp but lower ID)
        query = query.Where(e =>
            e.CreatedAt < cursorTimestamp ||
            (e.CreatedAt == cursorTimestamp && e.Id < cursorId)
        );

        return query;
    }

    /// <summary>
    /// Converts an ActivityEvent domain entity to a DTO for API response.
    /// </summary>
    private static ActivityFeedEntryDto ConvertToDto(ActivityEvent @event)
    {
        Dictionary<string, object>? payload = null;
        if (!string.IsNullOrWhiteSpace(@event.Metadata))
        {
            try
            {
                payload = JsonDocument.Parse(@event.Metadata).RootElement
                    .Deserialize<Dictionary<string, object>>();
            }
            catch
            {
                // If metadata parsing fails, leave payload as null
            }
        }

        return new ActivityFeedEntryDto
        {
            EventId = @event.EventId,
            OccurredAtUtc = @event.CreatedAt,
            EventType = @event.Type,
            CreatedBy = @event.Actor.Username,
            ResourceId = @event.EntityId > 0 ? @event.EntityId : null,
            Message = @event.Message,
            Payload = payload
        };
    }
}
