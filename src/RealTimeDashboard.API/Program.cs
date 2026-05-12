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

// Serve static files from wwwroot
app.UseStaticFiles();

// Map WebSocket endpoint
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var websocket = await context.WebSockets.AcceptWebSocketAsync();
        var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
        var connectionId = context.Connection.Id ?? Guid.NewGuid().ToString();
        await handler.HandleAsync(connectionId, websocket, context.RequestAborted);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

app.UseRouting();
app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
