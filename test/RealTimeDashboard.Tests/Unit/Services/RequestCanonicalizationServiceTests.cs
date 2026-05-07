using System;
using Xunit;
using RealTimeDashboard.API.DTOs;
using RealTimeDashboard.API.Services;

namespace RealTimeDashboard.Tests.Unit.Services;

/// <summary>
/// Unit tests for RequestCanonicalizationService.
/// Tests all transformation logic: DateTime normalization, currency normalization,
/// text field sanitization, and decimal precision.
/// </summary>
public class RequestCanonicalizationServiceTests
{
    private readonly RequestCanonicalizationService _service = new();

    #region CreateTransactionRequest Tests

    [Fact]
    public void CanonicalizeCreateTransactionRequest_WithValidRequest_SanitizesAllFields()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = new DateTime(2026, 5, 7, 14, 0, 0, DateTimeKind.Local),
            Amount = 123.456m,
            Currency = "usd",
            Description = "  Test  Description  with  spaces  ",
            CreatedBy = "  JohnDoe  ",
            Source = "  API  "
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        // DateTime converted to UTC
        Assert.Equal(DateTimeKind.Utc, request.Timestamp.Kind);

        // Amount rounded to 2 decimal places
        Assert.Equal(123.46m, request.Amount);

        // Currency normalized to uppercase
        Assert.Equal("USD", request.Currency);

        // Text fields trimmed and whitespace normalized
        Assert.Equal("Test Description with spaces", request.Description);
        Assert.Equal("JohnDoe", request.CreatedBy);
        Assert.Equal("API", request.Source);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_WithUtcTimestamp_RemainedUnchanged()
    {
        var timestamp = new DateTime(2026, 5, 7, 15, 0, 0, DateTimeKind.Utc);
        var request = new CreateTransactionRequest
        {
            Timestamp = timestamp,
            Amount = 50m,
            Currency = "USD",
            Description = "Test",
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Equal(timestamp, request.Timestamp);
        Assert.Equal(DateTimeKind.Utc, request.Timestamp.Kind);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_WithUnspecifiedTimezone_MarkedAsUtc()
    {
        var timestamp = new DateTime(2026, 5, 7, 15, 0, 0, DateTimeKind.Unspecified);
        var request = new CreateTransactionRequest
        {
            Timestamp = timestamp,
            Amount = 50m,
            Currency = "USD",
            Description = "Test",
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Equal(DateTimeKind.Utc, request.Timestamp.Kind);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_AmountRounding_BankerRounding()
    {
        // Test banker's rounding (round to nearest, ties to even)
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 10.125m,
            Currency = "USD",
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        // 10.125 rounds to 10.12 (banker's rounding - ties to even)
        Assert.Equal(10.12m, request.Amount);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_NullDescription_RemainsNull()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            Description = null,
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Null(request.Description);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_WhitespaceOnlyDescription_BecomesNull()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            Description = "   \t  \n  ",
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Null(request.Description);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_RemovesControlCharacters()
    {
        // Create string with actual control characters between words (no spaces)
        var testString = "Test" + (char)0x00 + "Description" + (char)0x1F + "with" + (char)0x08 + "control";
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            Description = testString,
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        // Control characters are removed, leaving the words together
        Assert.Equal("TestDescriptionwithcontrol", request.Description);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_PreservesNewlinesInDescription()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            Description = "Line1\nLine2\nLine3",
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        // Newlines should be preserved in description
        Assert.Contains("\n", request.Description ?? "");
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_EnforcesDescriptionMaxLength()
    {
        var longDescription = new string('A', 600);
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            Description = longDescription,
            CreatedBy = "User"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Equal(500, request.Description?.Length);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_EnforcesCreatedByMaxLength()
    {
        var longUsername = new string('X', 150);
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            CreatedBy = longUsername
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Equal(100, request.CreatedBy.Length);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_EnforcesSourceMaxLength()
    {
        var longSource = new string('Y', 100);
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            CreatedBy = "User",
            Source = longSource
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Equal(50, request.Source?.Length);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_NullSourceRemainsNull()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            CreatedBy = "User",
            Source = null
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Null(request.Source);
    }

    [Fact]
    public void CanonicalizeCreateTransactionRequest_MultipleSpaceCollapsed()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 50m,
            Currency = "USD",
            CreatedBy = "John    Doe    Smith"
        };

        _service.CanonicalizeCreateTransactionRequest(request);

        Assert.Equal("John Doe Smith", request.CreatedBy);
    }

    #endregion

    #region TransactionQueryRequest Tests

    [Fact]
    public void CanonicalizeTransactionQueryRequest_NormalizesDates()
    {
        var request = new TransactionQueryRequest
        {
            DateFrom = new DateTime(2026, 5, 7, 14, 0, 0, DateTimeKind.Local),
            DateTo = new DateTime(2026, 5, 8, 14, 0, 0, DateTimeKind.Local),
            Search = "test search"
        };

        _service.CanonicalizeTransactionQueryRequest(request);

        Assert.Equal(DateTimeKind.Utc, request.DateFrom?.Kind);
        Assert.Equal(DateTimeKind.Utc, request.DateTo?.Kind);
    }

    [Fact]
    public void CanonicalizeTransactionQueryRequest_TrimsSearchText()
    {
        var request = new TransactionQueryRequest
        {
            Search = "  test  search  query  "
        };

        _service.CanonicalizeTransactionQueryRequest(request);

        Assert.Equal("test search query", request.Search);
    }

    [Fact]
    public void CanonicalizeTransactionQueryRequest_EnforceSearchMaxLength()
    {
        var longSearch = new string('A', 300);
        var request = new TransactionQueryRequest
        {
            Search = longSearch
        };

        _service.CanonicalizeTransactionQueryRequest(request);

        Assert.Equal(200, request.Search?.Length);
    }

    [Fact]
    public void CanonicalizeTransactionQueryRequest_NormalizesSortToLowercase()
    {
        var request = new TransactionQueryRequest
        {
            Sort = "TIMESTAMP:DESC"
        };

        _service.CanonicalizeTransactionQueryRequest(request);

        Assert.Equal("timestamp:desc", request.Sort);
    }

    #endregion
}
