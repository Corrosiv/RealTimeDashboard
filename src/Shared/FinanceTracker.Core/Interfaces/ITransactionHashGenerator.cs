namespace FinanceTracker.Core.Interfaces;

using FinanceTracker.Core.Domain;

/// <summary>
/// Generates deterministic, idempotent hashes for transactions.
/// Used for deduplication and replay-safe idempotency keys.
/// </summary>
public interface ITransactionHashGenerator
{
    /// <summary>
    /// Generates a deterministic hash for a transaction.
    /// Two identical transactions produce the same hash.
    /// </summary>
    /// <param name="transaction">The transaction to hash.</param>
    /// <returns>A URL-safe hash string (hex or base64).</returns>
    string GenerateHash(Transaction transaction);

    /// <summary>
    /// Generates a hash from individual transaction components.
    /// Useful for comparing without creating a full Transaction object.
    /// </summary>
    /// <param name="timestamp">Transaction date/time.</param>
    /// <param name="amount">Amount.</param>
    /// <param name="description">Description (case-insensitive, whitespace-normalized).</param>
    /// <returns>A URL-safe hash string.</returns>
    string GenerateHash(DateTime timestamp, decimal amount, string? description);
}
