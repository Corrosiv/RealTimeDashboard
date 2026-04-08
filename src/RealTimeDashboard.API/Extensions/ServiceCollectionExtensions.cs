namespace RealTimeDashboard.API.Extensions;

using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardServices(this IServiceCollection services)
    {
        services.AddSingleton<RealTimeDashboard.API.Realtime.WebSocketConnectionManager>();
        services.AddSingleton<RealTimeDashboard.API.Realtime.WebSocketHandler>();
        services.AddSingleton<RealTimeDashboard.API.Services.ActivityFeedService>();

        // Persistence adapters: map Core persistence interface to EF adapter
        services.AddScoped<FinanceTracker.Core.Interfaces.ITransactionStore, RealTimeDashboard.API.Infrastructure.TransactionStore>();

        return services;
    }
}
