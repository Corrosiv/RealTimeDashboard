namespace RealTimeDashboard.API.Extensions;

using Microsoft.Extensions.DependencyInjection;
using FinanceTracker.Core.Interfaces;
using FinanceTracker.Core.Services;
using RealTimeDashboard.API.Infrastructure;

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

        return services;
    }
}
