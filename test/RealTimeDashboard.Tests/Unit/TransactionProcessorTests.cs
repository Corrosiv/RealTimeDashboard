using Xunit;
using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Services;

namespace RealTimeDashboard.Tests.Unit;

/// <summary>
/// Unit tests for TransactionHashGenerator.
/// Critical for deduplication and idempotency.
/// </summary>
public class TransactionHashGeneratorTests
{
    private readonly TransactionHashGenerator _generator = new();

    [Fact]
    public void GenerateHash_WithIdenticalTransactions_ReturnsSameHash()
    {
        // Arrange
        var tx1 = new Transaction
        {
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 50.00m,
            Description = "Groceries"
        };

        var tx2 = new Transaction
        {
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 50.00m,
            Description = "Groceries"
        };

        // Act
        var hash1 = _generator.GenerateHash(tx1);
        var hash2 = _generator.GenerateHash(tx2);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_WithDifferentAmounts_ReturnsDifferentHash()
    {
        // Arrange
        var tx1 = new Transaction
        {
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 50.00m,
            Description = "Groceries"
        };

        var tx2 = new Transaction
        {
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 51.00m,
            Description = "Groceries"
        };

        // Act
        var hash1 = _generator.GenerateHash(tx1);
        var hash2 = _generator.GenerateHash(tx2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_WithDifferentDescriptions_ReturnsDifferentHash()
    {
        // Arrange
        var tx1 = new Transaction
        {
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 50.00m,
            Description = "Groceries"
        };

        var tx2 = new Transaction
        {
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 50.00m,
            Description = "Gas"
        };

        // Act
        var hash1 = _generator.GenerateHash(tx1);
        var hash2 = _generator.GenerateHash(tx2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_WithDifferentDates_ReturnsDifferentHash()
    {
        // Arrange
        var hash1 = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            "Groceries"
        );

        var hash2 = _generator.GenerateHash(
            new DateTime(2024, 1, 16),
            50.00m,
            "Groceries"
        );

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_WithNullDescription_ProducesDeterministicHash()
    {
        // Arrange
        var hash1 = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            null
        );

        var hash2 = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            null
        );

        // Act & Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_WithWhitespaceVariations_ProducesSameHash()
    {
        // Arrange
        var hash1 = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            "  Groceries  "
        );

        var hash2 = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            "Groceries"
        );

        // Act & Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_WithCaseVariations_ProducesSameHash()
    {
        // Arrange
        var hash1 = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            "GROCERIES"
        );

        var hash2 = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            "groceries"
        );

        // Act & Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_ReturnsUrlSafeString()
    {
        // Arrange & Act
        var hash = _generator.GenerateHash(
            new DateTime(2024, 1, 15),
            50.00m,
            "Groceries"
        );

        // Assert: URL-safe (no padding, no / or +)
        Assert.DoesNotContain("+", hash);
        Assert.DoesNotContain("/", hash);
        Assert.DoesNotContain("=", hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void GenerateHash_IgnoresIdField()
    {
        // Arrange
        var tx1 = new Transaction
        {
            Id = 1,
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 50.00m,
            Description = "Groceries"
        };

        var tx2 = new Transaction
        {
            Id = 999,  // Different ID
            Timestamp = new DateTime(2024, 1, 15),
            Amount = 50.00m,
            Description = "Groceries"
        };

        // Act
        var hash1 = _generator.GenerateHash(tx1);
        var hash2 = _generator.GenerateHash(tx2);

        // Assert: Hashes should be same (ID ignored)
        Assert.Equal(hash1, hash2);
    }
}

