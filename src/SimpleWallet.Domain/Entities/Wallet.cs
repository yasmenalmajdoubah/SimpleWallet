namespace SimpleWallet.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal SpendingBalance { get; set; }
    public decimal SavingsBalance { get; set; }
    public string Currency { get; set; } = "JOD";
    public BankCard? Card { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<LedgerEntry> Entries { get; set; } = new List<LedgerEntry>();
}
