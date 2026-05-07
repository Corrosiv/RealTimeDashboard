using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using RealTimeDashboard.Tests.Integration.Helpers;
using RealTimeDashboard.API.DTOs;
using System.Text.Json;

namespace RealTimeDashboard.Tests.Integration;

/// <summary>
/// Integration tests for the Transactions API.
/// Tests pagination, filtering, sorting, and CRUD operations.
/// </summary>
public class TransactionsApiTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public TransactionsApiTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    #region Pagination Tests

    [Fact]
    public async Task GetTransactions_FirstPage_ReturnsDefaultLimit()
    {
        using var client = _factory.CreateClient();

        // Create some test transactions
        await CreateTestTransactions(client, 25);

        // Query first page
        var response = await client.GetAsync("/api/transactions?limit=20");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedTransactionResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(20, result.Data.Count);
        Assert.NotNull(result.NextCursor);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public async Task GetTransactions_WithCursor_FetchesNextPage()
    {
        using var client = _factory.CreateClient();

        // Create test transactions
        await CreateTestTransactions(client, 50);

        // Get first page
        var firstResponse = await client.GetAsync("/api/transactions?limit=20");
        firstResponse.EnsureSuccessStatusCode();
        var firstContent = await firstResponse.Content.ReadAsStringAsync();
        var firstResult = JsonSerializer.Deserialize<PaginatedTransactionResponse>(firstContent, _jsonOptions);

        Assert.NotNull(firstResult?.NextCursor);

        // Get second page using cursor
        var secondResponse = await client.GetAsync($"/api/transactions?limit=20&cursor={Uri.EscapeDataString(firstResult.NextCursor)}");
        secondResponse.EnsureSuccessStatusCode();
        var secondContent = await secondResponse.Content.ReadAsStringAsync();
        var secondResult = JsonSerializer.Deserialize<PaginatedTransactionResponse>(secondContent, _jsonOptions);

        Assert.NotNull(secondResult);
        Assert.Equal(20, secondResult.Data.Count);
        // Verify no overlap between pages
        Assert.DoesNotContain(secondResult.Data, t => firstResult.Data.Any(f => f.Id == t.Id));
    }

    [Fact]
    public async Task GetTransactions_LimitExceedsMax_Returns400()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/transactions?limit=101");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot exceed 100", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetTransactions_InvalidCursor_StartsFromBeginning()
    {
        using var client = _factory.CreateClient();

        await CreateTestTransactions(client, 10);

        var response = await client.GetAsync("/api/transactions?cursor=invalid-base64");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedTransactionResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Data.Count > 0);
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public async Task GetTransactions_FilterByDateRange_ReturnsMatchingTransactions()
    {
        using var client = _factory.CreateClient();

        var now = DateTime.UtcNow;
        var yesterday = now.AddDays(-1);
        var tomorrow = now.AddDays(1);

        // Create transaction from yesterday
        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = yesterday,
            Amount = -50,
            Currency = "USD",
            Description = "Yesterday",
            CreatedBy = "TestUser"
        });

        // Create transaction from today
        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = now,
            Amount = -30,
            Currency = "USD",
            Description = "Today",
            CreatedBy = "TestUser"
        });

        // Query for today only
        var dateFromStr = Uri.EscapeDataString(now.ToString("O"));
        var dateToStr = Uri.EscapeDataString(tomorrow.ToString("O"));
        var response = await client.GetAsync($"/api/transactions?dateFrom={dateFromStr}&dateTo={dateToStr}");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedTransactionResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        var todayTransaction = result.Data.FirstOrDefault(t => t.Description == "Today");
        Assert.NotNull(todayTransaction);
        var yesterdayTransaction = result.Data.FirstOrDefault(t => t.Description == "Yesterday");
        Assert.Null(yesterdayTransaction);
    }

    [Fact]
    public async Task GetTransactions_FilterByAmount_ReturnsMatchingTransactions()
    {
        using var client = _factory.CreateClient();

        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -10,
            Currency = "USD",
            Description = "Small",
            CreatedBy = "TestUser"
        });

        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -100,
            Currency = "USD",
            Description = "Large",
            CreatedBy = "TestUser"
        });

        // Query for amounts between -50 and -20
        var response = await client.GetAsync("/api/transactions?minAmount=-50&maxAmount=-20");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedTransactionResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Empty(result.Data); // No transactions in that range
    }

    [Fact]
    public async Task GetTransactions_FilterBySearch_ReturnsMatchingDescriptions()
    {
        using var client = _factory.CreateClient();

        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -50,
            Currency = "USD",
            Description = "Grocery Store",
            CreatedBy = "TestUser"
        });

        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -30,
            Currency = "USD",
            Description = "Coffee Shop",
            CreatedBy = "TestUser"
        });

        // Search for "Grocery"
        var response = await client.GetAsync("/api/transactions?search=Grocery");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedTransactionResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        var groceryTransaction = result.Data.FirstOrDefault(t => t.Description?.Contains("Grocery") == true);
        Assert.NotNull(groceryTransaction);
        Assert.DoesNotContain(result.Data, t => t.Description?.Contains("Coffee") == true);
    }

    [Fact]
    public async Task GetTransactions_DateFromAfterDateTo_Returns400()
    {
        using var client = _factory.CreateClient();

        var now = DateTime.UtcNow;
        var future = now.AddDays(1);

        // dateFrom is after dateTo
        var dateFromStr = Uri.EscapeDataString(future.ToString("O"));
        var dateToStr = Uri.EscapeDataString(now.ToString("O"));
        var response = await client.GetAsync($"/api/transactions?dateFrom={dateFromStr}&dateTo={dateToStr}");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task GetTransactions_SortDescending_ReturnsNewestFirst()
    {
        using var client = _factory.CreateClient();

        var baseTime = DateTime.UtcNow;
        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = baseTime.AddHours(-2),
            Amount = -50,
            Currency = "USD",
            Description = "Old",
            CreatedBy = "TestUser"
        });

        await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = baseTime.AddHours(-1),
            Amount = -30,
            Currency = "USD",
            Description = "Newer",
            CreatedBy = "TestUser"
        });

        var response = await client.GetAsync("/api/transactions?sort=timestamp:desc&limit=10");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PaginatedTransactionResponse>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Data.Count >= 2);
        // First should be newer
        Assert.Equal("Newer", result.Data[0].Description);
        Assert.Equal("Old", result.Data[1].Description);
    }

    [Fact]
    public async Task GetTransactions_InvalidSort_Returns400()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/transactions?sort=invalid");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region CRUD Tests

    [Fact]
    public async Task CreateTransaction_ValidRequest_Returns201Created()
    {
        using var client = _factory.CreateClient();

        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -50.00m,
            Currency = "USD",
            Description = "Test Transaction",
            CreatedBy = "TestUser",
            CategoryId = 1
        };

        var response = await CreateTransaction(client, request);

        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransactionDto>(content, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(-50.00m, result.Amount);
        Assert.Equal("Test Transaction", result.Description);
        Assert.Equal("TestUser", result.CreatedBy);
    }

    [Fact]
    public async Task CreateTransaction_InvalidAmount_Returns400()
    {
        using var client = _factory.CreateClient();

        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = 0, // Invalid: amount cannot be zero
            Currency = "USD",
            Description = "Test",
            CreatedBy = "TestUser"
        };

        var response = await CreateTransaction(client, request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Amount cannot be zero", content);
    }

    [Fact]
    public async Task CreateTransaction_FutureTimestamp_Returns400()
    {
        using var client = _factory.CreateClient();

        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow.AddDays(1), // Future timestamp
            Amount = -50,
            Currency = "USD",
            Description = "Test",
            CreatedBy = "TestUser"
        };

        var response = await CreateTransaction(client, request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot be in the future", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateTransaction_MissingCreatedBy_Returns400()
    {
        using var client = _factory.CreateClient();

        var request = new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -50,
            Currency = "USD",
            Description = "Test",
            CreatedBy = "" // Missing
        };

        var response = await CreateTransaction(client, request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTransactionById_ExistingId_ReturnsTransaction()
    {
        using var client = _factory.CreateClient();

        var createResponse = await CreateTransaction(client, new CreateTransactionRequest
        {
            Timestamp = DateTime.UtcNow,
            Amount = -50,
            Currency = "USD",
            Description = "Test",
            CreatedBy = "TestUser"
        });

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<TransactionDto>(createContent, _jsonOptions);

        var getResponse = await client.GetAsync($"/api/transactions/{created?.Id}");
        getResponse.EnsureSuccessStatusCode();

        var getContent = await getResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TransactionDto>(getContent, _jsonOptions);

        Assert.NotNull(result);
        Assert.Equal(created?.Id, result.Id);
        Assert.Equal("Test", result.Description);
    }

    [Fact]
    public async Task GetTransactionById_NonExistentId_Returns404()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/transactions/99999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not found", content, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Helper Methods

    private async Task<HttpResponseMessage> CreateTransaction(HttpClient client, CreateTransactionRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync("/api/transactions", content);
    }

    private async Task CreateTestTransactions(HttpClient client, int count)
    {
        var baseTime = DateTime.UtcNow;
        for (int i = 0; i < count; i++)
        {
            var request = new CreateTransactionRequest
            {
                Timestamp = baseTime.AddMinutes(-i),
                Amount = -(10 + i),
                Currency = "USD",
                Description = $"Test Transaction {i}",
                CreatedBy = "TestUser"
            };

            await CreateTransaction(client, request);
        }
    }

    #endregion
}
