using Microsoft.EntityFrameworkCore;
using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<BankCard> BankCards => Set<BankCard>();
    public DbSet<MoneyRequest> MoneyRequests => Set<MoneyRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        if (Database.IsSqlite())
        {
            modelBuilder.Entity<Wallet>().Property(wallet => wallet.RowVersion)
                .IsRequired(false)
                .IsConcurrencyToken(false)
                .ValueGeneratedNever();
        }
    }
}
