namespace FinanceTracker.Core.Services;

using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;

/// <summary>
/// Detects duplicate transactions based on date, amount, and description.
/// </summary>
public class TransactionDeduplicationService : ITransactionDeduplicationService
{
    private readonly ITransactionHashGenerator _hashGenerator;

    public TransactionDeduplicationService(ITransactionHashGenerator hashGenerator)
    {
        _hashGenerator = hashGenerator;
    }

    /// <inheritdoc/>
    public IEnumerable<TransactionDuplicateGroup> FindDuplicates(
        IEnumerable<Transaction> newTransactions,
        IEnumerable<Transaction> existingTransactions = null!)
    {
        var duplicates = new List<TransactionDuplicateGroup>();
        var newTxList = newTransactions.ToList();
        var existingTxList = existingTransactions?.ToList() ?? new List<Transaction>();

        // Create hash-to-index mappings
        var newHashes = newTxList
            .Select((tx, idx) => new { Hash = _hashGenerator.GenerateHash(tx), Index = idx, Tx = tx })
            .ToList();

        var existingHashes = existingTxList
            .Select((tx, idx) => new { Hash = _hashGenerator.GenerateHash(tx), Index = idx, Tx = tx })
            .ToList();

        // Find duplicates within the new batch
        var groupedByHash = newHashes.GroupBy(x => x.Hash);
        foreach (var group in groupedByHash)
        {
            if (group.Count() > 1)
            {
                duplicates.Add(new TransactionDuplicateGroup
                {
                    BatchIndices = group.Select(g => g.Index).ToList(),
                    ExistingDbIndex = null,
                    Reason = $"Exact match (date, amount, description) - {group.Count()} occurrences in batch"
                });
            }
        }

        // Find duplicates against existing transactions
        foreach (var newHash in newHashes)
        {
            var existingMatch = existingHashes.FirstOrDefault(eh => eh.Hash == newHash.Hash);
            if (existingMatch != null)
            {
                duplicates.Add(new TransactionDuplicateGroup
                {
                    BatchIndices = new List<int> { newHash.Index },
                    ExistingDbIndex = existingMatch.Index,
                    Reason = $"Matches existing transaction (date: {existingMatch.Tx.Timestamp:yyyy-MM-dd}, amount: {existingMatch.Tx.Amount})"
                });
            }
        }

        return duplicates;
    }
}
