using SimpleWallet.Domain.Enums;

namespace SimpleWallet.Domain.Entities;

public class MoneyRequest
{
    public Guid Id { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid PayerUserId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public MoneyRequestStatus Status { get; set; } = MoneyRequestStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
