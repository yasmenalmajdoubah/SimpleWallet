namespace SimpleWallet.Domain.Entities;

public class Wallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "JOD";
    public byte[] RowVersion { get; set; } = [];
    public ICollection<LedgerEntry> Entries { get; set; } = new List<LedgerEntry>();
}
