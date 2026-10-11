using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Infrastructure.Persistence;

public class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries", table =>
        {
            table.HasCheckConstraint("CK_LedgerEntries_Amount_Positive", "[Amount] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.BalanceAfter).HasPrecision(18, 2);
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Note).HasMaxLength(300);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.WalletId, x.CreatedAt });
        builder.HasOne(x => x.Wallet)
            .WithMany(x => x.Entries)
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(x => x.CounterpartyWalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
