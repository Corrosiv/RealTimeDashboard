namespace FinanceTracker.Core.Services;

using FinanceTracker.Core.Domain;
using FinanceTracker.Core.DTOs;

public interface ITransactionProcessor
{
    Task<CsvImportResult> ParseAndProcessAsync(Stream csvStream, string username, CancellationToken cancellationToken = default);
}
