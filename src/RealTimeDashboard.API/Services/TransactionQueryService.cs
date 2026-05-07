using Microsoft.EntityFrameworkCore;
using FinanceTracker.Core.Domain;
using RealTimeDashboard.API.DTOs;
using RealTimeDashboard.API.Infrastructure;

namespace RealTimeDashboard.API.Services;

/// <summary>
/// Service for querying transactions with filtering, pagination, and sorting.
/// Encapsulates complex query logic away from the controller.
/// </summary>
public class TransactionQueryService
{
    private readonly FinanceDbContext _db;
    private readonly CursorService _cursorService;

    public TransactionQueryService(FinanceDbContext db, CursorService cursorService)
    {
        _db = db;
        _cursorService = cursorService;
    }

    /// <summary>
    /// Queries transactions based on filter and pagination criteria.
    /// Returns a paginated response with cursor-based navigation.
    /// </summary>
    public async Task<PaginatedTransactionResponse> QueryTransactionsAsync(
        TransactionQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Transactions.AsQueryable();

        // Apply filters
        query = ApplyFilters(query, request);

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply cursor-based pagination
        query = ApplyCursorPagination(query, request);

        // Apply sorting
        query = ApplySorting(query, request.Sort);

        // Fetch one extra record to determine if there's a next page
        var transactions = await query
            .Take(request.Limit + 1)
            .ToListAsync(cancellationToken);

        var hasNextPage = transactions.Count > request.Limit;
        if (hasNextPage)
        {
            transactions = transactions.Take(request.Limit).ToList();
        }

        // Convert to DTOs
        var dtos = transactions.Select(t => new TransactionDto(
            t.Id,
            t.Timestamp,
            t.Amount,
            t.Description,
            t.CategoryId,
            t.CreatedBy ?? "Unknown"
        )).ToList();

        // Generate next cursor
        string? nextCursor = null;
        if (hasNextPage && transactions.Count > 0)
        {
            var lastTransaction = transactions[^1];
            nextCursor = _cursorService.EncodeCursor(lastTransaction.Timestamp, lastTransaction.Id);
        }

        return new PaginatedTransactionResponse
        {
            Data = dtos,
            NextCursor = nextCursor,
            TotalCount = totalCount,
            Count = dtos.Count
        };
    }

    /// <summary>
    /// Applies filter conditions to the transaction query.
    /// </summary>
    private IQueryable<Transaction> ApplyFilters(
        IQueryable<Transaction> query,
        TransactionQueryRequest request)
    {
        // Date range filter
        if (request.DateFrom.HasValue)
        {
            query = query.Where(t => t.Timestamp >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            // Include entire day by adding one day
            var dateToEnd = request.DateTo.Value.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.Timestamp <= dateToEnd);
        }

        // Amount range filter
        if (request.MinAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= request.MaxAmount.Value);
        }

        // Category filter
        if (request.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        }

        // Type/Source filter
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(t => t.Source == request.Type);
        }

        // Text search in description
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.ToLower();
            query = query.Where(t => t.Description != null && t.Description.ToLower().Contains(searchTerm));
        }

        return query;
    }

    /// <summary>
    /// Applies cursor-based pagination.
    /// The cursor represents (timestamp, id) of the last item from the previous page.
    /// </summary>
    private IQueryable<Transaction> ApplyCursorPagination(
        IQueryable<Transaction> query,
        TransactionQueryRequest request)
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

        // For "timestamp:desc" (default), we want transactions BEFORE the cursor
        if (request.Sort.StartsWith("timestamp:desc", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t =>
                t.Timestamp < cursorTimestamp ||
                (t.Timestamp == cursorTimestamp && t.Id < cursorId)
            );
        }
        else if (request.Sort.StartsWith("timestamp:asc", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(t =>
                t.Timestamp > cursorTimestamp ||
                (t.Timestamp == cursorTimestamp && t.Id > cursorId)
            );
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to the query.
    /// </summary>
    private IQueryable<Transaction> ApplySorting(
        IQueryable<Transaction> query,
        string sort)
    {
        if (sort.StartsWith("timestamp:asc", StringComparison.OrdinalIgnoreCase))
        {
            return query.OrderBy(t => t.Timestamp).ThenBy(t => t.Id);
        }

        // Default: timestamp:desc
        return query.OrderByDescending(t => t.Timestamp).ThenByDescending(t => t.Id);
    }

    /// <summary>
    /// Gets a single transaction by ID.
    /// </summary>
    public async Task<Transaction?> GetTransactionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
