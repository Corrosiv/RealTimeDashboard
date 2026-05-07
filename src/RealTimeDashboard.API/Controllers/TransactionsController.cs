using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Core.Domain;
using RealTimeDashboard.API.DTOs;
using RealTimeDashboard.API.Services;
using RealTimeDashboard.API.DomainEvents;
using RealTimeDashboard.API.Infrastructure;

namespace RealTimeDashboard.API.Controllers;

/// <summary>
/// API endpoints for transaction management.
/// Provides querying, filtering, pagination, and creation of financial transactions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly TransactionQueryService _queryService;
    private readonly FinanceDbContext _db;
    private readonly IValidator<TransactionQueryRequest> _queryValidator;
    private readonly IValidator<CreateTransactionRequest> _createValidator;
    private readonly IDomainEventPublisher _eventPublisher;
    private readonly RequestCanonicalizationService _canonicalizationService;

    public TransactionsController(
        TransactionQueryService queryService,
        FinanceDbContext db,
        IValidator<TransactionQueryRequest> queryValidator,
        IValidator<CreateTransactionRequest> createValidator,
        IDomainEventPublisher eventPublisher,
        RequestCanonicalizationService canonicalizationService)
    {
        _queryService = queryService;
        _db = db;
        _queryValidator = queryValidator;
        _createValidator = createValidator;
        _eventPublisher = eventPublisher;
        _canonicalizationService = canonicalizationService;
    }

    /// <summary>
    /// Gets transactions with filtering, pagination, and sorting.
    /// Supports cursor-based pagination for stable, real-time friendly queries.
    /// 
    /// Query Parameters:
    /// - dateFrom: Filter transactions on or after this date (ISO 8601)
    /// - dateTo: Filter transactions on or before this date (ISO 8601)
    /// - minAmount: Minimum transaction amount (inclusive)
    /// - maxAmount: Maximum transaction amount (inclusive)
    /// - categoryId: Filter by category ID
    /// - type: Filter by transaction source (CSV, Manual, API, etc.)
    /// - search: Text search in description
    /// - cursor: Pagination cursor from previous response (omit on first request)
    /// - limit: Maximum records to return (1-100, default 20)
    /// - sort: Sort order (timestamp:asc or timestamp:desc, default timestamp:desc)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedTransactionResponse>> GetTransactions(
        [FromQuery] TransactionQueryRequest request,
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

        // Canonicalize request (normalize dates, trim search, etc.)
        _canonicalizationService.CanonicalizeTransactionQueryRequest(request);

        try
        {
            var response = await _queryService.QueryTransactionsAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while querying transactions.",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }

    /// <summary>
    /// Gets a single transaction by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TransactionDto>> GetTransactionById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _queryService.GetTransactionByIdAsync(id, cancellationToken);
        if (transaction == null)
        {
            return NotFound(new ApiErrorResponse
            {
                Code = "NOT_FOUND",
                Message = $"Transaction with ID {id} not found.",
                StatusCode = StatusCodes.Status404NotFound
            });
        }

        var dto = new TransactionDto(
            transaction.Id,
            transaction.Timestamp,
            transaction.Amount,
            transaction.Description,
            transaction.CategoryId,
            transaction.CreatedBy ?? "Unknown"
        );

        return Ok(dto);
    }

    /// <summary>
    /// Creates a new transaction.
    /// 
    /// Pipeline:
    /// 1. Request is validated (FluentValidation)
    /// 2. Request is canonicalized (DateTime normalization, currency uppercase, text trimming, amount rounding)
    /// 3. Transaction domain entity is created
    /// 4. Entity is persisted atomically
    /// 5. Domain event is published for handlers (activity feed, metrics, WebSocket broadcast)
    /// 
    /// Notes:
    /// - CreatedAt is always server-set and cannot be provided by clients
    /// - Source defaults to "Manual" during canonicalization if not provided
    /// - Amount is rounded to 2 decimal places
    /// - Currency must be a valid ISO 4217 code
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate request structure
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
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

        // 2. Canonicalize request (normalize/clean all fields)
        _canonicalizationService.CanonicalizeCreateTransactionRequest(request);

        try
        {
            // 3. Create transaction entity from canonicalized request
            var transaction = new Transaction
            {
                Timestamp = request.Timestamp,
                Amount = request.Amount,
                Currency = request.Currency,
                Description = request.Description,
                CategoryId = request.CategoryId,
                CreatedBy = request.CreatedBy,
                CreatedAt = DateTime.UtcNow,
                Source = request.Source ?? "Manual",
                Scope = "default"
            };

            // 4. Persist atomically
            _db.Transactions.Add(transaction);
            await _db.SaveChangesAsync(cancellationToken);

            // 5. Publish domain event for handlers (metrics, activity feed, WebSocket broadcast)
            var @event = new TransactionCreatedEvent
            {
                TransactionId = transaction.Id,
                Timestamp = transaction.Timestamp,
                Amount = transaction.Amount,
                CategoryId = transaction.CategoryId,
                CreatedBy = transaction.CreatedBy ?? "Unknown",
                Scope = transaction.Scope
            };
            await _eventPublisher.PublishAsync(@event, cancellationToken);

            // Return created resource (matches persisted state)
            var dto = new TransactionDto(
                transaction.Id,
                transaction.Timestamp,
                transaction.Amount,
                transaction.Description,
                transaction.CategoryId,
                transaction.CreatedBy ?? "Unknown"
            );

            return CreatedAtAction(nameof(GetTransactionById), new { id = transaction.Id }, dto);
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                Code = "INTERNAL_ERROR",
                Message = "An error occurred while creating the transaction.",
                StatusCode = StatusCodes.Status500InternalServerError
            });
        }
    }
}
