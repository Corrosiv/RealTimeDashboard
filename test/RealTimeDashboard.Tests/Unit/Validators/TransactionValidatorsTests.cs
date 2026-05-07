using System;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Xunit;
using RealTimeDashboard.API.DTOs;
using RealTimeDashboard.API.Validators;

namespace RealTimeDashboard.Tests.Unit.Validators;

/// <summary>
/// Unit tests for TransactionQueryRequestValidator.
/// </summary>
public class TransactionQueryRequestValidatorTests
{
    private readonly TransactionQueryRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_PassesValidation()
    {
        var request = new TransactionQueryRequest
        {
            Limit = 20,
            Sort = "timestamp:desc"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_LimitExceedsMax_FailsValidation()
    {
        var request = new TransactionQueryRequest
        {
            Limit = 101
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Limit)
            .WithErrorMessage("Limit cannot exceed 100.");
    }

    [Fact]
    public async Task Validate_LimitIsZero_FailsValidation()
    {
        var request = new TransactionQueryRequest
        {
            Limit = 0
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Limit)
            .WithErrorMessage("Limit must be greater than 0.");
    }

    [Fact]
    public async Task Validate_DateFromAfterDateTo_FailsValidation()
    {
        var now = DateTime.UtcNow;
        var request = new TransactionQueryRequest
        {
            DateFrom = now,
            DateTo = now.AddDays(-1) // Before DateFrom
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.DateFrom);
    }

    [Fact]
    public async Task Validate_MinAmountGreaterThanMaxAmount_FailsValidation()
    {
        var request = new TransactionQueryRequest
        {
            MinAmount = 100,
            MaxAmount = 50
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.MinAmount);
    }

    [Fact]
    public async Task Validate_InvalidSort_FailsValidation()
    {
        var request = new TransactionQueryRequest
        {
            Sort = "invalid:sort"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Sort)
            .WithErrorMessage("Sort must be in format 'timestamp:asc' or 'timestamp:desc'.");
    }

    [Fact]
    public async Task Validate_SearchTooLong_FailsValidation()
    {
        var request = new TransactionQueryRequest
        {
            Search = new string('a', 201) // Exceeds 200 char limit
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Search)
            .WithErrorMessage("Search text cannot exceed 200 characters.");
    }

    [Fact]
    public async Task Validate_ValidSortAscending_PassesValidation()
    {
        var request = new TransactionQueryRequest
        {
            Sort = "timestamp:asc"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Sort);
    }
}

/// <summary>
/// Unit tests for CreateTransactionRequestValidator.
/// </summary>
public class CreateTransactionRequestValidatorTests
{
    private readonly CreateTransactionRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_PassesValidation()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow.AddHours(-1),
            Amount = -50.00m,
            Currency = "USD",
            Description = "Test",
            CreatedBy = "TestUser"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_FutureTimestamp_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow.AddDays(1),
            Amount = -50,
            Currency = "USD",
            CreatedBy = "TestUser"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Timestamp)
            .WithErrorMessage("Timestamp cannot be in the future.");
    }

    [Fact]
    public async Task Validate_ZeroAmount_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 0m,
            Currency = "USD",
            CreatedBy = "TestUser"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount cannot be zero.");
    }

    [Fact]
    public async Task Validate_EmptyCreatedBy_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -50,
            Currency = "USD",
            CreatedBy = ""
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.CreatedBy);
    }

    [Fact]
    public async Task Validate_InvalidCurrencyLength_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -50,
            Currency = "TOOLONG", // More than 5 characters
            CreatedBy = "TestUser"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency code must be 2-5 characters (ISO 4217 format).");
    }

    [Fact]
    public async Task Validate_DescriptionTooLong_FailsValidation()
    {
        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -50,
            Currency = "USD",
            Description = new string('a', 501),
            CreatedBy = "TestUser"
        };

        var result = await _validator.TestValidateAsync(request);

        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 500 characters.");
    }
}
