namespace RealTimeDashboard.API.DomainEvents;

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

/// <summary>
/// Event raised when a transaction is created.
/// Handlers can use this to trigger metrics recalculation, activity feed updates, etc.
/// </summary>
public class TransactionCreatedEvent : DomainEvent
{
    public int TransactionId { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Amount { get; set; }
    public int? CategoryId { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string Scope { get; set; } = "default";
}
