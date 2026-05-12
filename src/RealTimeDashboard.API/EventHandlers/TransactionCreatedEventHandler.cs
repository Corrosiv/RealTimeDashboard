using FinanceTracker.Core.Interfaces;
using FinanceTracker.Core.Domain;
using RealTimeDashboard.API.DomainEvents;
using RealTimeDashboard.API.ActivityFeed;
using RealTimeDashboard.API.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace RealTimeDashboard.API.EventHandlers;

/// <summary>
/// Handles TransactionCreatedEvent by:
/// 1. Creating an activity feed entry
/// 2. Publishing it via ActivityEventPublisher (which persists + broadcasts)
/// </summary>
public class TransactionCreatedEventHandler
{
    private readonly FinanceDbContext _db;
    private readonly IActivityEventFactory _eventFactory;
    private readonly ActivityEventPublisher _eventPublisher;

    public TransactionCreatedEventHandler(
        FinanceDbContext db,
        IActivityEventFactory eventFactory,
        ActivityEventPublisher eventPublisher)
    {
        _db = db;
        _eventFactory = eventFactory;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles transaction created event:
    /// - Creates activity event via factory
    /// - Publishes via ActivityEventPublisher (single source of truth)
    /// </summary>
    public async Task HandleAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the transaction to pass to the factory
            var transaction = await _db.Transactions
                .FirstOrDefaultAsync(t => t.Id == @event.TransactionId, cancellationToken);

            if (transaction == null)
            {
                System.Diagnostics.Debug.WriteLine($"Transaction {@event.TransactionId} not found for activity event creation");
                return;
            }

            // Create actor info
            var actor = new ActorInfo
            {
                Username = @event.CreatedBy,
                DisplayName = @event.CreatedBy,
                JoinedAt = DateTime.UtcNow
            };

            // Get next sequence ID by querying the max existing sequence ID
            var maxSequenceId = await _db.ActivityEvents
                .AsNoTracking()
                .Where(ae => ae.Scope == @event.Scope)
                .MaxAsync(ae => (long?)ae.SequenceId, cancellationToken) ?? 0;
            var nextSequenceId = maxSequenceId + 1;

            // Create activity event using factory with the next sequence ID
            var activityEvent = _eventFactory.CreateTransactionCreatedEvent(transaction, actor, nextSequenceId);

            // Persist and broadcast via single publisher
            // This ensures events are ordered globally and stored durably
            await _eventPublisher.PublishAsync(activityEvent, @event.Scope);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we don't want to fail the transaction
            System.Diagnostics.Debug.WriteLine($"Error handling TransactionCreatedEvent: {ex.Message}");
        }
    }
}
