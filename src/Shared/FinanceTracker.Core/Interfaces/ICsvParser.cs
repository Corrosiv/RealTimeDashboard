namespace FinanceTracker.Core.Interfaces;

public interface ICsvParser
{
    IAsyncEnumerable<IDictionary<string, string>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

public interface ITransactionStore
{
    Task AddTransactionsAsync(IEnumerable<FinanceTracker.Core.Domain.Transaction> transactions, CancellationToken cancellationToken = default);
    Task<IEnumerable<FinanceTracker.Core.Domain.Transaction>> GetTransactionsAsync(int limit = 100, int offset = 0, CancellationToken cancellationToken = default);
}
