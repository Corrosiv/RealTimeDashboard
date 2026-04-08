namespace FinanceTracker.Core.Domain;

public class ActivityEvent
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Actor { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? Metadata { get; set; }
}
