using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Infrastructure.Persistence;

public static class DemoCard
{
    public static BankCard Create(Guid walletId, string holderName)
    {
        var now = DateTime.UtcNow;
        return new BankCard
        {
            Id = Guid.NewGuid(),
            WalletId = walletId,
            HolderName = holderName,
            Last4 = Random.Shared.Next(0, 10000).ToString("D4"),
            ExpiryMonth = now.Month,
            ExpiryYear = now.Year + 3,
            IsFrozen = false,
            CreatedAt = now
        };
    }

    public static string Mask(string last4) => $"•••• •••• •••• {last4}";

    public static string Expiry(int month, int year) => $"{month:00}/{year % 100:00}";
}
