using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FinanceTracker.Core.Services;
using RealTimeDashboard.API.Extensions;
using RealTimeDashboard.API.Middleware;
using RealTimeDashboard.API.Infrastructure;
using RealTimeDashboard.API.Realtime;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services registration
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=finance.db";
builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlite(connectionString)
);

// Configuration binding
builder.Services.Configure<WebSocketReplayOptions>(
    builder.Configuration.GetSection("WebSocketReplay"));

// Register shared FinanceTracker core implementations
builder.Services.AddScoped<ITransactionProcessor, TransactionProcessor>();
builder.Services.AddScoped<IBudgetCalculator, BudgetCalculator>();

// Register API-specific services (websocket, activity feed, transactions)
builder.Services.AddDashboardServices();

// CORS (if needed for frontend)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    db.Database.Migrate();
}

// Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();

app.UseRouting();
app.MapControllers();

app.Run();
