namespace FinanceTracker.Core.Services;

using System.Security.Cryptography;
using System.Text;
using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;

/// <summary>
/// Generates deterministic hashes for transactions.
/// Uses SHA256 for reliability and URL-safe encoding for idempotency keys.
/// </summary>
public class TransactionHashGenerator : ITransactionHashGenerator
{
    /// <inheritdoc/>
    public string GenerateHash(Transaction transaction)
    {
        return GenerateHash(transaction.Timestamp, transaction.Amount, transaction.Description);
    }

    /// <inheritdoc/>
    public string GenerateHash(DateTime timestamp, decimal amount, string? description)
    {
        // Normalize description: lowercase, trim whitespace
        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? string.Empty
            : description.Trim().ToLowerInvariant();

        // Create a deterministic string from the key fields
        // Format: YYYY-MM-DD|amount|description
        var hashInput = $"{timestamp:yyyy-MM-dd}|{amount:F2}|{normalizedDescription}";

        // Hash using SHA256
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
            // Return base64url-safe encoding (replace / with _, + with -)
            return Convert.ToBase64String(hashBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
