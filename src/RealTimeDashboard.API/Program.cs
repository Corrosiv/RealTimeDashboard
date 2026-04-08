using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FinanceTracker.Core.Services;
using RealTimeDashboard.API.Extensions;
using RealTimeDashboard.API.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Services registration
builder.Services.AddControllers();

// Register shared FinanceTracker core implementations
// (Core project is referenced by the API project)
builder.Services.AddScoped<ITransactionProcessor, TransactionProcessor>();
builder.Services.AddScoped<IBudgetCalculator, BudgetCalculator>();

// Register API-specific services (websocket, activity feed)
builder.Services.AddDashboardServices();

// Ensure EF Core SQLite services are available (registered in ServiceCollectionExtensions normally)

var app = builder.Build();

// Basic middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseRouting();
app.MapControllers();

app.Run();
