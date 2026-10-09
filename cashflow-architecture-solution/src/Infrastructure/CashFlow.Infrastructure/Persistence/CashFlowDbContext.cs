using Microsoft.EntityFrameworkCore;

namespace CashFlow.Infrastructure.Persistence;

public class CashFlowDbContext : DbContext
{
    public CashFlowDbContext(DbContextOptions<CashFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(x => x.CurrentBalance)
                .HasColumnType("decimal(18,2)");
            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.HasIndex(x => x.Name)
                .IsUnique();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Description)
                .HasMaxLength(250);
            entity.Property(x => x.Amount)
                .HasColumnType("decimal(18,2)");
            entity.Property(x => x.TransactionDate)
                .IsRequired();

            entity.HasOne(x => x.Account)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}

public class Account
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentBalance { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class Category
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsExpense { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public TransactionType Type { get; set; }
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
    public Category? Category { get; set; }
}

public enum TransactionType
{
    Income = 1,
    Expense = 2,
    Transfer = 3
}
