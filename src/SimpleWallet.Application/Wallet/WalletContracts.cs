using System.ComponentModel.DataAnnotations;

namespace SimpleWallet.Application.Wallets;

public sealed class DepositRequest
{
    [Required(ErrorMessage = "المبلغ مطلوب.")]
    public decimal Amount { get; set; }

    [MaxLength(300, ErrorMessage = "الملاحظة طويلة.")]
    public string? Note { get; set; }
}

public sealed class TransferRequest
{
    [Required(ErrorMessage = "إيميل المستلم مطلوب.")]
    [EmailAddress(ErrorMessage = "إيميل المستلم غير صحيح.")]
    public string ToEmail { get; set; } = "";

    [Required(ErrorMessage = "المبلغ مطلوب.")]
    public decimal Amount { get; set; }

    [MaxLength(300, ErrorMessage = "الملاحظة طويلة.")]
    public string? Note { get; set; }
}

public sealed record WalletBalanceResponse(decimal Balance, string Currency);

public sealed record MoneyMovementResponse(decimal Balance, string Currency, Guid ReferenceId);

public sealed record TransactionResponse(
    Guid Id,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? CounterpartyEmail,
    string? Note,
    DateTime CreatedAt);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
