using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace RealTimeDashboard.API.Infrastructure;

public class TransactionStore : FinanceTracker.Core.Interfaces.ITransactionStore
{
    private readonly FinanceDbContext _db;

    public TransactionStore(FinanceDbContext db) => _db = db;

    public async Task AddTransactionsAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
    {
        await _db.Transactions.AddRangeAsync(transactions, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default)
    {
        return await _db.Transactions.OrderByDescending(t => t.Timestamp).Skip(offset).Take(limit).ToListAsync(cancellationToken);
    }
}
