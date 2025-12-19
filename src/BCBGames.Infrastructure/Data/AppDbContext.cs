using BCBGames.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BCBGames.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.AccountNumber)
                .IsRequired()
                .HasMaxLength(20);

            entity.Property(e => e.OwnerName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Balance)
                .HasPrecision(18, 2);

            entity.Property(e => e.Version)
                .IsConcurrencyToken();

            entity.HasIndex(e => e.AccountNumber)
                .IsUnique();

            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.Account)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);

            entity.Property(e => e.BalanceBefore)
                .HasPrecision(18, 2);

            entity.Property(e => e.BalanceAfter)
                .HasPrecision(18, 2);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.Type)
                .HasConversion<int>();

            entity.Property(e => e.Status)
                .HasConversion<int>();

            entity.HasIndex(e => e.AccountId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.AccountId, e.CreatedAt });
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.Property(e => e.LastError)
                .HasMaxLength(2000);

            entity.HasIndex(e => e.ProcessedAt);

            entity.HasIndex(e => new { e.IdempotencyKey, e.EventType, e.AggregateId })
                .IsUnique();
        });
    }
}
