using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace RealTimeDashboard.Tests.Integration.Helpers;

public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Hook for overriding services in integration tests.
        builder.ConfigureServices(services =>
        {
            // Replace FinanceDbContext with SQLite in-memory per-test
            // Note: the API registers FinanceDbContext; here we remove that registration by searching descriptors.
            var descriptors = services.Where(d => d.ServiceType?.FullName?.Contains("FinanceDbContext") == true).ToList();
            foreach (var d in descriptors) services.Remove(d);

            var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
            connection.Open();
            services.AddDbContext<RealTimeDashboard.API.Infrastructure.FinanceDbContext>(options => options.UseSqlite(connection));

            // Ensure DB is created when host starts
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RealTimeDashboard.API.Infrastructure.FinanceDbContext>();
            db.Database.EnsureCreated();
        });

        return base.CreateHost(builder);
    }
}
