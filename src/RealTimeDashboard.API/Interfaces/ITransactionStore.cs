using FinanceTracker.Core.Domain;

namespace RealTimeDashboard.API.Interfaces;

public interface ITransactionStore
{
    Task AddTransactionsAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default);
    Task<IEnumerable<Transaction>> GetTransactionsAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
}
