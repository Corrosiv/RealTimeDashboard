using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace RealTimeDashboard.Tests.Integration.Helpers;

public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint> where TEntryPoint : class
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"rtd-tests-{Guid.NewGuid():N}.db");

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Hook for overriding services in integration tests.
        builder.ConfigureServices(services =>
        {
            // Replace FinanceDbContext with SQLite using a unique temporary database file per factory instance
            // This ensures each test class/factory gets an isolated database, preventing "table already exists" errors
            var descriptors = services.Where(d => d.ServiceType?.FullName?.Contains("FinanceDbContext") == true).ToList();
            foreach (var d in descriptors) services.Remove(d);

            var connectionString = $"Data Source={_dbPath}";
            services.AddDbContext<RealTimeDashboard.API.Infrastructure.FinanceDbContext>(options =>
                options.UseSqlite(connectionString)
            );

            // Apply migrations once when the host starts (Program.cs will call Migrate() again, which is idempotent)
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RealTimeDashboard.API.Infrastructure.FinanceDbContext>();
            db.Database.Migrate();
        });

        return base.CreateHost(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        // Clean up the temporary database file when the factory is disposed
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
            // Ignore cleanup failures (file may be locked or already deleted)
        }

        await base.DisposeAsync();
    }
}
