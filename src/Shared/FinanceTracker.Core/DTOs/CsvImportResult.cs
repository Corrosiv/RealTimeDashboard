namespace FinanceTracker.Core.DTOs;

public class CsvImportResult
{
    public int ProcessedCount { get; set; }
    public List<string>? Errors { get; set; }
}
