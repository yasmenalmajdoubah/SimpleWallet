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

public sealed class SalaryRequest
{
    public decimal Amount { get; set; }
}

public sealed class MoveMoneyRequest
{
    public bool ToSavings { get; set; }

    [Required(ErrorMessage = "المبلغ مطلوب.")]
    public decimal Amount { get; set; }
}

public sealed class CreateMoneyRequest
{
    [Required(ErrorMessage = "إيميل الشخص مطلوب.")]
    [EmailAddress(ErrorMessage = "الإيميل غير صحيح.")]
    public string PayerEmail { get; set; } = "";

    [Required(ErrorMessage = "المبلغ مطلوب.")]
    public decimal Amount { get; set; }

    [MaxLength(300, ErrorMessage = "الملاحظة طويلة.")]
    public string? Note { get; set; }
}

public sealed class CardPaymentRequest
{
    [Required(ErrorMessage = "المبلغ مطلوب.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "اسم التاجر مطلوب.")]
    [MaxLength(80, ErrorMessage = "اسم التاجر طويل.")]
    public string Merchant { get; set; } = "";
}

public sealed class PayBillRequest
{
    [Required(ErrorMessage = "نوع الفاتورة مطلوب.")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "المبلغ مطلوب.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "رقم الاشتراك مطلوب.")]
    [MaxLength(40, ErrorMessage = "رقم الاشتراك طويل.")]
    public string Reference { get; set; } = "";
}

public sealed class FreezeCardRequest
{
    public bool Frozen { get; set; }
}

public sealed record CardResponse(string HolderName, string MaskedNumber, string Expiry, bool IsFrozen);

public sealed record WalletHomeResponse(
    decimal SpendingBalance,
    decimal SavingsBalance,
    string Currency,
    decimal SalaryAmount,
    bool CanCollectSalary,
    int IncomingRequests,
    CardResponse? Card);

public sealed record MoneyMovementResponse(
    decimal SpendingBalance,
    decimal SavingsBalance,
    string Currency,
    Guid ReferenceId);

public sealed record SalaryStateResponse(decimal SalaryAmount, bool CanCollectSalary);

public sealed record BillResponse(string Code, string Name);

public sealed record MoneyRequestResponse(
    Guid Id,
    string Direction,
    string Status,
    decimal Amount,
    string? Note,
    string CounterpartyName,
    string CounterpartyEmail,
    DateTime CreatedAt);

public sealed record TransactionResponse(
    Guid Id,
    string Type,
    string Pocket,
    decimal Amount,
    decimal BalanceAfter,
    string? CounterpartyEmail,
    string? Note,
    DateTime CreatedAt);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
