using Microsoft.EntityFrameworkCore;
using FinanceTracker.Core.Domain;

namespace RealTimeDashboard.API.Infrastructure;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions { get; set; } = null!;
    public DbSet<FinanceTracker.Core.Domain.Category> Categories { get; set; } = null!;
    public DbSet<FinanceTracker.Core.Domain.ActivityEvent> ActivityEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(eb =>
        {
            eb.HasKey(t => t.Id);
            eb.Property(t => t.Amount).IsRequired();
            eb.Property(t => t.Timestamp).IsRequired();
        });

        modelBuilder.Entity<FinanceTracker.Core.Domain.Category>(eb =>
        {
            eb.HasKey(c => c.Id);
            eb.Property(c => c.Name).IsRequired();
        });

        modelBuilder.Entity<FinanceTracker.Core.Domain.ActivityEvent>(eb =>
        {
            eb.HasKey(a => a.Id);
            eb.Property(a => a.Timestamp).IsRequired();
            eb.Property(a => a.Message).IsRequired();
        });
    }
}
