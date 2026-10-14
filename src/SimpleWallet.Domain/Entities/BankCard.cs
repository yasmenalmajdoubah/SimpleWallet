namespace SimpleWallet.Domain.Entities;

public class BankCard
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public string HolderName { get; set; } = "";
    public string Last4 { get; set; } = "";
    public int ExpiryMonth { get; set; }
    public int ExpiryYear { get; set; }
    public bool IsFrozen { get; set; }
    public DateTime CreatedAt { get; set; }
}
