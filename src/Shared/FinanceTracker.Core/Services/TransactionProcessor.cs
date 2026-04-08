namespace FinanceTracker.Core.Services;

using FinanceTracker.Core.Domain;
using FinanceTracker.Core.DTOs;

public class TransactionProcessor : ITransactionProcessor
{
    public Task<CsvImportResult> ParseAndProcessAsync(Stream csvStream, string username, CancellationToken cancellationToken = default)
    {
        // TODO: implement CSV parsing, mapping and validation.
        // - Parse CSV rows from the provided stream
        // - Validate required columns and row-level data
        // - Map rows to Domain.Transaction instances
        // - Categorize transactions (use a ICategoryResolver if available)
        // - Persist transactions via a repository adapter (in API project)
        // - Return CsvImportResult with processed count and errors
        // Current implementation is a safe placeholder to keep the project buildable.
        return Task.FromResult(new CsvImportResult { ProcessedCount = 0 });
    }
}
