using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Infrastructure.Persistence;

public class MoneyRequestConfiguration : IEntityTypeConfiguration<MoneyRequest>
{
    public void Configure(EntityTypeBuilder<MoneyRequest> builder)
    {
        builder.ToTable("MoneyRequests", table =>
        {
            table.HasCheckConstraint("CK_MoneyRequests_Amount_Positive", "[Amount] > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.Note).HasMaxLength(300);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.PayerUserId, x.Status });
        builder.HasIndex(x => new { x.RequesterUserId, x.Status });
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RequesterUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PayerUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
