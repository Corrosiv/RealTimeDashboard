using Microsoft.Extensions.DependencyInjection;

namespace RealTimeDashboard.API.DomainEvents;

/// <summary>
/// Interface for publishing domain events.
/// Implementations can handle events synchronously or asynchronously.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : DomainEvent;
}

/// <summary>
/// In-memory domain event publisher that dispatches to registered handlers via DI.
/// Handlers are resolved from the service provider to support dependency injection.
/// </summary>
public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Publishes an event to all registered handlers.
    /// Handlers are resolved from the DI container by convention:
    /// For event type "TEvent", looks for handler type "TEventHandler"
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : DomainEvent
    {
        var eventType = typeof(TEvent);
        var handlerType = Type.GetType($"{eventType.Namespace}.{eventType.Name}Handler");

        if (handlerType == null)
        {
            return; // No handler registered for this event type
        }

        try
        {
            var handler = _serviceProvider.GetService(handlerType);
            if (handler == null)
            {
                return; // Handler not registered in DI
            }

            // Get the HandleAsync method
            var handleMethod = handlerType.GetMethod("HandleAsync", 
                new[] { eventType, typeof(CancellationToken) });

            if (handleMethod != null)
            {
                var task = handleMethod.Invoke(handler, new object[] { @event, cancellationToken }) as Task;
                if (task != null)
                {
                    await task;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error publishing event {eventType.Name}: {ex.Message}");
        }
    }
}
