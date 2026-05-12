using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using RealTimeDashboard.API.DTOs;
using RealTimeDashboard.API.Services;

namespace RealTimeDashboard.API.Controllers;

/// <summary>
/// API endpoints for activity feed management.
/// Provides querying, filtering, and pagination of historical activity events.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ActivityFeedController : ControllerBase
{
    private readonly ActivityFeedQueryService _queryService;
    private readonly IValidator<ActivityFeedQueryRequest> _queryValidator;

    public ActivityFeedController(
        ActivityFeedQueryService queryService,
        IValidator<ActivityFeedQueryRequest> queryValidator)
    {
        _queryService = queryService;
        _queryValidator = queryValidator;
    }

    /// <summary>
    /// Gets activity feed entries with filtering, pagination, and sorting.
    /// Supports cursor-based pagination for stable, real-time friendly queries.
    /// 
    /// Query Parameters:
    /// - createdBy: Filter activities by username (exact match, case-insensitive)
    /// - eventType: Filter by event type (comma-separated for multiple values, e.g., "TransactionCreated,CsvUploaded")
    /// - since: Filter activities on or after this timestamp (ISO 8601)
    /// - until: Filter activities on or before this timestamp (ISO 8601)
    /// - resourceId: Filter by related resource ID (e.g., transaction ID)
    /// - cursor: Pagination cursor from previous response (omit on first request)
    /// - limit: Maximum entries to return (1-200, default 50)
    /// 
    /// Response:
    /// - entries: Array of activity feed entries
    /// - nextCursor: Pagination cursor for the next page (null if no more results)
    /// - hasMore: Boolean indicating if more results are available
    /// 
    /// Sorting: Newest first (CreatedAt DESC), with ID DESC as tie-breaker for stable ordering.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedActivityFeedResponse>> GetActivityFeed(
        [FromQuery] ActivityFeedQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate request
        var validationResult = await _queryValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequest(new ApiErrorResponse
            {
                Code = "VALIDATION_ERROR",
                Message = "The request contains validation errors.",
                StatusCode = StatusCodes.Status400BadRequest,
                Errors = validationResult.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToList()
                    )
            });
        }

        try
        {
            var response = await _queryService.QueryActivityFeedAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while querying the activity feed.",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }
}
