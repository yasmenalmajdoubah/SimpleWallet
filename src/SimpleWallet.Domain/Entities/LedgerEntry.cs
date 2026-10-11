using SimpleWallet.Domain.Enums;

namespace SimpleWallet.Domain.Entities;

public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = null!;
    public LedgerEntryType Type { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public Guid ReferenceId { get; set; }
    public Guid? CounterpartyWalletId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
