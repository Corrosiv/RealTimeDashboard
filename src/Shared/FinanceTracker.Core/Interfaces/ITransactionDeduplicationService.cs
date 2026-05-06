namespace FinanceTracker.Core.Interfaces;

using FinanceTracker.Core.Domain;

/// <summary>
/// Detects duplicate transactions within a CSV upload or against existing DB transactions.
/// Critical for data integrity during batch imports.
/// </summary>
public interface ITransactionDeduplicationService
{
    /// <summary>
    /// Detects duplicates in a batch of new transactions.
    /// </summary>
    /// <param name="newTransactions">Transactions to check for duplicates.</param>
    /// <param name="existingTransactions">Existing transactions in the database.</param>
    /// <returns>List of duplicate groups, each containing indices of duplicate transactions.</returns>
    IEnumerable<TransactionDuplicateGroup> FindDuplicates(
        IEnumerable<Transaction> newTransactions,
        IEnumerable<Transaction> existingTransactions = null!);
}

/// <summary>
/// Represents a group of duplicate transactions.
/// </summary>
public class TransactionDuplicateGroup
{
    /// <summary>
    /// Indices of transactions in the new batch that are duplicates of each other.
    /// </summary>
    public List<int> BatchIndices { get; set; } = new();

    /// <summary>
    /// Index of the duplicate in existing DB (if matching against DB).
    /// Null if duplicates are within the batch only.
    /// </summary>
    public int? ExistingDbIndex { get; set; }

    /// <summary>
    /// Human-readable description of why these are duplicates.
    /// </summary>
    public string Reason { get; set; } = null!;
}
