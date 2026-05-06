using Xunit;
using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Services;

namespace RealTimeDashboard.Tests.Unit;

/// <summary>
/// Unit tests for TransactionDeduplicationService.
/// Critical for data integrity during batch imports.
/// </summary>
public class TransactionDeduplicationServiceTests
{
    private readonly TransactionDeduplicationService _deduplicationService;
    private readonly TransactionHashGenerator _hashGenerator;

    public TransactionDeduplicationServiceTests()
    {
        _hashGenerator = new TransactionHashGenerator();
        _deduplicationService = new TransactionDeduplicationService(_hashGenerator);
    }

    [Fact]
    public void FindDuplicates_WithExactMatches_IdentifiesBatchDuplicates()
    {
        // Arrange
        var tx1 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");
        var tx2 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");
        var tx3 = CreateTransaction(new DateTime(2024, 1, 16), 75.00m, "Gas");

        var newTransactions = new[] { tx1, tx2, tx3 };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions).ToList();

        // Assert
        Assert.Single(duplicates);
        Assert.Equal(2, duplicates[0].BatchIndices.Count);
        Assert.Contains(0, duplicates[0].BatchIndices);
        Assert.Contains(1, duplicates[0].BatchIndices);
        Assert.Null(duplicates[0].ExistingDbIndex);
    }

    [Fact]
    public void FindDuplicates_WithoutDuplicates_ReturnsEmpty()
    {
        // Arrange
        var tx1 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");
        var tx2 = CreateTransaction(new DateTime(2024, 1, 16), 75.00m, "Gas");
        var tx3 = CreateTransaction(new DateTime(2024, 1, 17), 30.00m, "Parking");

        var newTransactions = new[] { tx1, tx2, tx3 };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions).ToList();

        // Assert
        Assert.Empty(duplicates);
    }

    [Fact]
    public void FindDuplicates_WithExistingTransactions_IdentifiesDbMatches()
    {
        // Arrange
        var existingTx = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");
        var newTx = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");

        var newTransactions = new[] { newTx };
        var existingTransactions = new[] { existingTx };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions, existingTransactions).ToList();

        // Assert
        Assert.Single(duplicates);
        Assert.Single(duplicates[0].BatchIndices);
        Assert.Equal(0, duplicates[0].BatchIndices[0]);
        Assert.Equal(0, duplicates[0].ExistingDbIndex);
    }

    [Fact]
    public void FindDuplicates_WithWhitespaceVariations_IdentifiesDuplicates()
    {
        // Arrange
        var tx1 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "  Groceries  ");
        var tx2 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");

        var newTransactions = new[] { tx1, tx2 };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions).ToList();

        // Assert: Should detect as duplicate despite whitespace
        Assert.Single(duplicates);
    }

    [Fact]
    public void FindDuplicates_WithCaseVariations_IdentifiesDuplicates()
    {
        // Arrange
        var tx1 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "GROCERIES");
        var tx2 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "groceries");

        var newTransactions = new[] { tx1, tx2 };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions).ToList();

        // Assert: Should detect as duplicate despite case
        Assert.Single(duplicates);
    }

    [Fact]
    public void FindDuplicates_WithMultipleDuplicateGroups_ReturnsMultipleGroups()
    {
        // Arrange
        var tx1 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");
        var tx2 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");
        var tx3 = CreateTransaction(new DateTime(2024, 1, 16), 75.00m, "Gas");
        var tx4 = CreateTransaction(new DateTime(2024, 1, 16), 75.00m, "Gas");

        var newTransactions = new[] { tx1, tx2, tx3, tx4 };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions).ToList();

        // Assert
        Assert.Equal(2, duplicates.Count);
    }

    [Fact]
    public void FindDuplicates_WithNullDescriptions_HandlesProperly()
    {
        // Arrange
        var tx1 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, null);
        var tx2 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, null);

        var newTransactions = new[] { tx1, tx2 };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions).ToList();

        // Assert
        Assert.Single(duplicates);
    }

    [Fact]
    public void FindDuplicates_DoesNotFlagSimilarButDifferent_AsDuplicates()
    {
        // Arrange
        var tx1 = CreateTransaction(new DateTime(2024, 1, 15), 50.00m, "Groceries");
        var tx2 = CreateTransaction(new DateTime(2024, 1, 15), 50.01m, "Groceries");  // Different amount
        var tx3 = CreateTransaction(new DateTime(2024, 1, 16), 50.00m, "Groceries");  // Different date

        var newTransactions = new[] { tx1, tx2, tx3 };

        // Act
        var duplicates = _deduplicationService.FindDuplicates(newTransactions).ToList();

        // Assert
        Assert.Empty(duplicates);
    }

    // Helper method
    private Transaction CreateTransaction(DateTime timestamp, decimal amount, string? description)
    {
        return new Transaction
        {
            Timestamp = timestamp,
            Amount = amount,
            Description = description
        };
    }
}
