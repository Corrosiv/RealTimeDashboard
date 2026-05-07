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

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardServices(this IServiceCollection services)
    {
        // WebSocket services
        services.AddSingleton<RealTimeDashboard.API.Realtime.WebSocketConnectionManager>();
        services.AddSingleton<RealTimeDashboard.API.Realtime.WebSocketHandler>();
        services.AddSingleton<RealTimeDashboard.API.Services.ActivityFeedService>();

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

        // Event handlers
        services.AddScoped<TransactionCreatedEventHandler>();

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<TransactionQueryRequestValidator>();

        // Domain event publisher (use service provider for DI-based handler resolution)
        services.AddSingleton<IDomainEventPublisher>(sp => new DomainEventPublisher(sp));

        return services;
    }
}
