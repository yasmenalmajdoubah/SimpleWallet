using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Infrastructure.Persistence;

public class BankCardConfiguration : IEntityTypeConfiguration<BankCard>
{
    public void Configure(EntityTypeBuilder<BankCard> builder)
    {
        builder.ToTable("BankCards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.HolderName).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Last4).HasMaxLength(4).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.WalletId).IsUnique();
        builder.HasOne(x => x.Wallet)
            .WithOne(x => x.Card)
            .HasForeignKey<BankCard>(x => x.WalletId);
    }
}
