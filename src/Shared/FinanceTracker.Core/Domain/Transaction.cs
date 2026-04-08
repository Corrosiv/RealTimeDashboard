namespace FinanceTracker.Core.Domain;

public class Transaction
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
    public string? CreatedBy { get; set; }
}
