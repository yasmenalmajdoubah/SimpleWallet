namespace SimpleWallet.Domain.Enums;

public enum LedgerEntryType
{
    Deposit = 1,
    TransferIn = 2,
    TransferOut = 3,
    Salary = 4,
    BillPayment = 5,
    CardPayment = 6,
    MoveToSavings = 7,
    MoveToSpending = 8
}
