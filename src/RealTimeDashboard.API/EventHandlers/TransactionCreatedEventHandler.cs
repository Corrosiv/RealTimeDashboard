using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FinanceTracker.Core.Interfaces;
using FinanceTracker.Core.Domain;
using RealTimeDashboard.API.DomainEvents;
using RealTimeDashboard.API.Infrastructure;
using RealTimeDashboard.API.Realtime;
using Microsoft.EntityFrameworkCore;

namespace RealTimeDashboard.API.EventHandlers;

/// <summary>
/// Handles TransactionCreatedEvent by:
/// 1. Creating an activity feed entry
/// 2. Broadcasting transaction created event to WebSocket clients
/// 3. Logging event for replay window
/// </summary>
public class TransactionCreatedEventHandler
{
    private readonly FinanceDbContext _db;
    private readonly IActivityEventFactory _eventFactory;
    private readonly WebSocketConnectionManager _wsManager;
    private readonly IEventLogService _eventLogService;

    public TransactionCreatedEventHandler(
        FinanceDbContext db,
        IActivityEventFactory eventFactory,
        WebSocketConnectionManager wsManager,
        IEventLogService eventLogService)
    {
        _db = db;
        _eventFactory = eventFactory;
        _wsManager = wsManager;
        _eventLogService = eventLogService;
    }

    /// <summary>
    /// Handles transaction created event:
    /// - Creates activity feed entry
    /// - Broadcasts WebSocket event to connected clients
    /// - Logs event for replay window
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

            // Get current sequence ID (will be auto-incremented by EF)
            var lastSequenceId = await _db.ActivityEvents
                .AsNoTracking()
                .Where(ae => ae.Scope == @event.Scope)
                .MaxAsync(ae => (long?)ae.SequenceId, cancellationToken) ?? 0;

            // Create activity event using factory
            var activityEvent = _eventFactory.CreateTransactionCreatedEvent(transaction, actor, lastSequenceId + 1);

            _db.ActivityEvents.Add(activityEvent);
            await _db.SaveChangesAsync(cancellationToken);

            // Log event for replay window
            var eventLog = new EventLog
            {
                ActivityEventId = activityEvent.Id,
                Scope = @event.Scope,
                LoggedAt = DateTime.UtcNow,
                IsPruned = false
            };
            _db.EventLogs.Add(eventLog);
            await _db.SaveChangesAsync(cancellationToken);

            // Broadcast to WebSocket clients
            await BroadcastWebSocketEventAsync(@event, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - we don't want to fail the transaction
            System.Diagnostics.Debug.WriteLine($"Error handling TransactionCreatedEvent: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcasts transaction created event to all connected WebSocket clients.
    /// </summary>
    private async Task BroadcastWebSocketEventAsync(TransactionCreatedEvent @event, CancellationToken cancellationToken)
    {
        var wsMessage = new
        {
            type = "TransactionCreated",
            payload = new
            {
                transactionId = @event.TransactionId,
                timestamp = @event.Timestamp,
                amount = @event.Amount,
                categoryId = @event.CategoryId,
                createdBy = @event.CreatedBy,
                scope = @event.Scope
            }
        };

        var json = JsonSerializer.Serialize(wsMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        var tasks = new List<Task>();
        foreach (var kvp in _wsManager.GetAll())
        {
            var socket = kvp.Value;
            if (socket.State == WebSocketState.Open)
            {
                tasks.Add(socket.SendAsync(
                    new ArraySegment<byte>(bytes, 0, bytes.Length),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: cancellationToken
                ));
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }
}
