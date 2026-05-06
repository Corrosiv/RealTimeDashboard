using Microsoft.EntityFrameworkCore;
using FinanceTracker.Core.Domain;

namespace RealTimeDashboard.API.Infrastructure;

/// <summary>
/// Entity Framework Core DbContext for the RealTimeDashboard.
/// Manages transactions, categories, budgets, activity events, and event logs.
/// </summary>
public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options) { }

    /// <summary>
    /// Financial transactions (expenses/income).
    /// </summary>
    public DbSet<Transaction> Transactions { get; set; } = null!;

    /// <summary>
    /// Transaction categories (expense grouping).
    /// </summary>
    public DbSet<Category> Categories { get; set; } = null!;

    /// <summary>
    /// Budget constraints per category with versioning.
    /// </summary>
    public DbSet<Budget> Budgets { get; set; } = null!;

    /// <summary>
    /// Activity events for the live feed and event sourcing.
    /// </summary>
    public DbSet<ActivityEvent> ActivityEvents { get; set; } = null!;

    /// <summary>
    /// Event log entries for bounded replay window.
    /// </summary>
    public DbSet<EventLog> EventLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Transaction configuration
        modelBuilder.Entity<Transaction>(eb =>
        {
            eb.HasKey(t => t.Id);
            eb.Property(t => t.Id).ValueGeneratedOnAdd();
            eb.Property(t => t.Amount).IsRequired().HasPrecision(18, 2);
            eb.Property(t => t.Timestamp).IsRequired();
            eb.Property(t => t.CreatedAt).IsRequired();
            eb.Property(t => t.Scope).IsRequired().HasMaxLength(100).HasDefaultValue("default");
            eb.Property(t => t.IdempotencyKey).HasMaxLength(64);
            eb.Property(t => t.Description).HasMaxLength(500);
            eb.Property(t => t.Currency).HasMaxLength(10);
            eb.Property(t => t.CreatedBy).HasMaxLength(100);
            eb.Property(t => t.Source).HasMaxLength(50).HasDefaultValue("Manual");

            // Index for deduplication queries
            eb.HasIndex(t => t.IdempotencyKey).IsUnique(false);
            eb.HasIndex(t => new { t.Scope, t.Timestamp });
            eb.HasIndex(t => t.CreatedBy);
        });

        // Category configuration
        modelBuilder.Entity<Category>(eb =>
        {
            eb.HasKey(c => c.Id);
            eb.Property(c => c.Id).ValueGeneratedOnAdd();
            eb.Property(c => c.Name).IsRequired().HasMaxLength(100);
            eb.HasOne<Category>()
                .WithMany()
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Budget configuration with versioning
        modelBuilder.Entity<Budget>(eb =>
        {
            eb.HasKey(b => b.Id);
            eb.Property(b => b.Id).ValueGeneratedOnAdd();
            eb.Property(b => b.CategoryId).IsRequired();
            eb.Property(b => b.LimitAmount).IsRequired().HasPrecision(18, 2);
            eb.Property(b => b.Period).IsRequired().HasMaxLength(50).HasDefaultValue("Monthly");
            eb.Property(b => b.Scope).IsRequired().HasMaxLength(100).HasDefaultValue("default");
            eb.Property(b => b.Version).IsRequired().HasDefaultValue(1);
            eb.Property(b => b.CreatedAt).IsRequired();
            eb.Property(b => b.UpdatedAt).IsRequired();

            // Unique constraint: one budget per category per scope per period
            eb.HasIndex(b => new { b.CategoryId, b.Scope, b.Period }).IsUnique();

            // Foreign key to Category
            eb.HasOne<Category>()
                .WithMany()
                .HasForeignKey(b => b.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityEvent configuration (event sourcing)
        modelBuilder.Entity<ActivityEvent>(eb =>
        {
            eb.HasKey(a => a.Id);
            eb.Property(a => a.Id).ValueGeneratedOnAdd();
            eb.Property(a => a.EventId).IsRequired().HasMaxLength(36);
            eb.Property(a => a.SequenceId).IsRequired().ValueGeneratedOnAdd();
            eb.Property(a => a.Type).IsRequired().HasMaxLength(100);
            eb.Property(a => a.EntityId).IsRequired();
            eb.Property(a => a.Scope).IsRequired().HasMaxLength(100).HasDefaultValue("default");
            eb.Property(a => a.Message).IsRequired().HasMaxLength(1000);
            eb.Property(a => a.CreatedAt).IsRequired();
            eb.Property(a => a.Version).IsRequired(false);
            eb.Property(a => a.Metadata).IsRequired(false);

            // Complex property: ActorInfo (owned entity)
            eb.OwnsOne(a => a.Actor, nav =>
            {
                nav.Property(ac => ac.Username).IsRequired().HasMaxLength(100).HasColumnName("Actor_Username");
                nav.Property(ac => ac.DisplayName).HasMaxLength(150).HasColumnName("Actor_DisplayName");
                nav.Property(ac => ac.JoinedAt).IsRequired().HasColumnName("Actor_JoinedAt");
            });

            // Indexes for efficient querying
            eb.HasIndex(a => a.EventId).IsUnique();
            eb.HasIndex(a => a.SequenceId);
            eb.HasIndex(a => new { a.Scope, a.SequenceId });
            eb.HasIndex(a => a.Type);
            eb.HasIndex(a => new { a.EntityId, a.Type });
        });

        // EventLog configuration (bounded replay window)
        modelBuilder.Entity<EventLog>(eb =>
        {
            eb.HasKey(el => el.Id);
            eb.Property(el => el.Id).ValueGeneratedOnAdd();
            eb.Property(el => el.ActivityEventId).IsRequired();
            eb.Property(el => el.Scope).IsRequired().HasMaxLength(100).HasDefaultValue("default");
            eb.Property(el => el.LoggedAt).IsRequired();
            eb.Property(el => el.IsPruned).IsRequired().HasDefaultValue(false);

            // Foreign key to ActivityEvent
            eb.HasOne<ActivityEvent>()
                .WithMany()
                .HasForeignKey(el => el.ActivityEventId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for replay queries
            eb.HasIndex(el => new { el.Scope, el.LoggedAt });
            eb.HasIndex(el => new { el.Scope, el.IsPruned });
        });
    }
}
