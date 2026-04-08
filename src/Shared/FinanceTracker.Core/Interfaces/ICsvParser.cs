namespace FinanceTracker.Core.Interfaces;

public interface ICsvParser
{
    IAsyncEnumerable<IDictionary<string, string>> ParseAsync(Stream csvStream, CancellationToken cancellationToken = default);
}
