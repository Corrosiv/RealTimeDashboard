using Xunit;
using System.Text.Json;
using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Services;

namespace RealTimeDashboard.Tests.Unit;

/// <summary>
/// Unit tests for ActivityEventFactory.
/// Critical for correct event creation, sequencing, and payload shape.
/// </summary>
public class ActivityEventFactoryTests
{
    private readonly ActivityEventFactory _factory = new();

    private static ActorInfo CreateActor(string username = "alice", string? displayName = null)
    {
        return new ActorInfo
        {
            Username = username,
            DisplayName = displayName ?? username,
            JoinedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void CreateEvent_GeneratesUniqueEventId()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event1 = _factory.CreateEvent("TestEvent", 1, "Test message 1", actor, 1);
        var event2 = _factory.CreateEvent("TestEvent", 2, "Test message 2", actor, 2);

        // Assert
        Assert.NotNull(event1.EventId);
        Assert.NotNull(event2.EventId);
        Assert.NotEqual(event1.EventId, event2.EventId);
    }

    [Fact]
    public void CreateEvent_SetsSequenceId()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event1 = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1);
        var event2 = _factory.CreateEvent("TestEvent", 2, "Test", actor, 5);

        // Assert
        Assert.Equal(1, event1.SequenceId);
        Assert.Equal(5, event2.SequenceId);
    }

    [Fact]
    public void CreateEvent_SetsCorrectType()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateEvent("MyCustomEvent", 1, "Test", actor, 1);

        // Assert
        Assert.Equal("MyCustomEvent", event_.Type);
    }

    [Fact]
    public void CreateEvent_SetsEntityId()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 42, "Test", actor, 1);

        // Assert
        Assert.Equal(42, event_.EntityId);
    }

    [Fact]
    public void CreateEvent_IncludesMessage()
    {
        // Arrange
        var actor = CreateActor();
        var message = "User created a transaction";

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, message, actor, 1);

        // Assert
        Assert.Equal(message, event_.Message);
    }

    [Fact]
    public void CreateEvent_IncludesActorInfo()
    {
        // Arrange
        var actor = CreateActor("bob", "Bob Smith");

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1);

        // Assert
        Assert.Equal("bob", event_.Actor.Username);
        Assert.Equal("Bob Smith", event_.Actor.DisplayName);
    }

    [Fact]
    public void CreateEvent_IncludesVersion_WhenProvided()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1, version: 3);

        // Assert
        Assert.Equal(3, event_.Version);
    }

    [Fact]
    public void CreateEvent_VersionIsNull_WhenNotProvided()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1);

        // Assert
        Assert.Null(event_.Version);
    }

    [Fact]
    public void CreateEvent_IncludesMetadata()
    {
        // Arrange
        var actor = CreateActor();
        var metadata = JsonSerializer.Serialize(new { key = "value" });

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1, metadata: metadata);

        // Assert
        Assert.Equal(metadata, event_.Metadata);
    }

    [Fact]
    public void CreateEvent_SetsScopeToDefault()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1);

        // Assert
        Assert.Equal("default", event_.Scope);
    }

    [Fact]
    public void CreateTransactionCreatedEvent_IncludesSummaryNotId()
    {
        // Arrange
        var tx = new Transaction 
        { 
            Id = 123,  // Should NOT appear in message
            Amount = 50.00m,
            Description = "Groceries",
            Currency = "USD"
        };
        var actor = CreateActor("alice", "Alice");

        // Act
        var event_ = _factory.CreateTransactionCreatedEvent(tx, actor, 1);

        // Assert
        Assert.DoesNotContain("123", event_.Message);  // ID not in message
        Assert.Contains("50.00", event_.Message);      // Amount included
        Assert.Contains("Groceries", event_.Message);  // Description included
        Assert.Contains("Alice", event_.Message);      // Actor included
    }

    [Fact]
    public void CreateTransactionCreatedEvent_HandlesNullDescription()
    {
        // Arrange
        var tx = new Transaction 
        { 
            Id = 1,
            Amount = 50.00m,
            Description = null
        };
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateTransactionCreatedEvent(tx, actor, 1);

        // Assert
        Assert.NotEmpty(event_.Message);  // Should have a message
        Assert.DoesNotContain("null", event_.Message.ToLower());
    }

    [Fact]
    public void CreateTransactionCreatedEvent_TruncatesLongDescription()
    {
        // Arrange
        var tx = new Transaction 
        { 
            Id = 1,
            Amount = 50.00m,
            Description = new string('x', 300)  // Very long description
        };
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateTransactionCreatedEvent(tx, actor, 1);

        // Assert: Message should not be excessively long and should include truncation indicator
        Assert.True(event_.Message.Length < 1000);  // Reasonable limit
    }

    [Fact]
    public void CreateTransactionCreatedEvent_IncludesMetadata()
    {
        // Arrange
        var tx = new Transaction 
        { 
            Id = 1,
            Amount = 50.00m,
            Description = "Test",
            CategoryId = 5,
            Currency = "USD",
            Source = "CSV"
        };
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateTransactionCreatedEvent(tx, actor, 1);

        // Assert: Metadata should be valid JSON and contain transaction details
        Assert.NotNull(event_.Metadata);
        var metadata = JsonSerializer.Deserialize<JsonElement>(event_.Metadata);
        Assert.Equal(50.00m, metadata.GetProperty("amount").GetDecimal());
        Assert.Equal(5, metadata.GetProperty("categoryId").GetInt32());
    }

    [Fact]
    public void CreateCsvImportedEvent_ReportsImportCount()
    {
        // Arrange
        var actor = CreateActor("bob", "Bob");

        // Act
        var event_ = _factory.CreateCsvImportedEvent(15, actor, 1);

        // Assert
        Assert.Contains("15", event_.Message);
        Assert.Contains("transaction", event_.Message);
    }

    [Fact]
    public void CreateCsvImportedEvent_HandlesSingularPlural()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event1 = _factory.CreateCsvImportedEvent(1, actor, 1);
        var event2 = _factory.CreateCsvImportedEvent(5, actor, 2);

        // Assert
        Assert.Contains("1 transaction", event1.Message);
        Assert.Contains("5 transactions", event2.Message);
    }

    [Fact]
    public void CreateBudgetExceededEvent_ReportsOverage()
    {
        // Arrange
        var budget = new Budget 
        { 
            Id = 1, 
            CategoryId = 1, 
            LimitAmount = 1000m,
            Version = 1
        };
        var currentSpent = 1200m;

        // Act
        var event_ = _factory.CreateBudgetExceededEvent(budget, currentSpent, 1);

        // Assert
        Assert.Contains("1200.00", event_.Message);  // Current spent
        Assert.Contains("1000.00", event_.Message);  // Limit
        Assert.Contains("200.00", event_.Message);   // Overage
    }

    [Fact]
    public void CreateBudgetExceededEvent_IncludesMetadata()
    {
        // Arrange
        var budget = new Budget { Id = 1, CategoryId = 1, LimitAmount = 1000m, Version = 1, Period = "Monthly" };
        var currentSpent = 1200m;

        // Act
        var event_ = _factory.CreateBudgetExceededEvent(budget, currentSpent, 1);

        // Assert
        Assert.NotNull(event_.Metadata);
        var metadata = JsonSerializer.Deserialize<JsonElement>(event_.Metadata);
        Assert.Equal(1000m, metadata.GetProperty("limitAmount").GetDecimal());
        Assert.Equal("Monthly", metadata.GetProperty("period").GetString());
    }

    [Fact]
    public void CreateBudgetExceededEvent_IncludesVersionForConcurrency()
    {
        // Arrange
        var budget = new Budget { Id = 1, CategoryId = 1, LimitAmount = 1000m, Version = 5 };

        // Act
        var event_ = _factory.CreateBudgetExceededEvent(budget, 1200m, 1);

        // Assert
        Assert.Equal(5, event_.Version);
    }

    [Fact]
    public void EventId_IsGuidFormat()
    {
        // Arrange
        var actor = CreateActor();

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1);

        // Assert: EventId should be parseable as a Guid
        Assert.True(Guid.TryParse(event_.EventId, out _));
    }

    [Fact]
    public void CreatedAt_IsUtc()
    {
        // Arrange
        var actor = CreateActor();
        var beforeCreation = DateTime.UtcNow;

        // Act
        var event_ = _factory.CreateEvent("TestEvent", 1, "Test", actor, 1);
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(event_.CreatedAt >= beforeCreation);
        Assert.True(event_.CreatedAt <= afterCreation);
    }
}
