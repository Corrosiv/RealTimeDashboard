namespace RealTimeDashboard.API.Extensions;

using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using FinanceTracker.Core.Interfaces;
using FinanceTracker.Core.Services;
using RealTimeDashboard.API.Infrastructure;
using RealTimeDashboard.API.Services;
using RealTimeDashboard.API.Validators;
using RealTimeDashboard.API.DomainEvents;
using RealTimeDashboard.API.EventHandlers;
using RealTimeDashboard.API.Realtime;
using RealTimeDashboard.API.ActivityFeed;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardServices(this IServiceCollection services)
    {
        // WebSocket services
        // Note: WebSocketHandler is scoped because it depends on WebSocketReplayService (scoped)
        services.AddSingleton<WebSocketConnectionManager>();
        services.AddScoped<WebSocketHandler>();
        services.AddSingleton<ActivityFeedService>();
        services.AddScoped<WebSocketReplayService>();

        // Real-time event publishing (single source of truth for live + replay)
        services.AddScoped<ActivityEventPublisher>();

        // Correctness-critical services (heavy unit testing)
        services.AddScoped<ITransactionHashGenerator, TransactionHashGenerator>();
        services.AddScoped<ITransactionDeduplicationService, TransactionDeduplicationService>();
        services.AddScoped<IBudgetVersioningService, BudgetVersioningService>();
        services.AddScoped<IActivityEventFactory, ActivityEventFactory>();
        services.AddScoped<IEventLogService, EventLogService>();

        // Persistence adapters
        services.AddScoped<FinanceTracker.Core.Interfaces.ITransactionStore, TransactionStore>();

        // Transaction API services
        services.AddScoped<TransactionQueryService>();
        services.AddScoped<CursorService>();
        services.AddScoped<RequestCanonicalizationService>();

        // Event handlers
        services.AddScoped<TransactionCreatedEventHandler>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<TransactionQueryRequestValidator>();

        // Domain event publisher (use service provider for DI-based handler resolution)
        services.AddSingleton<IDomainEventPublisher>(sp => new DomainEventPublisher(sp));

        return services;
    }
}
