using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SimpleWallet.Application.Abstractions;
using SimpleWallet.Application.Common;
using SimpleWallet.Application.Wallets;
using SimpleWallet.Domain.Enums;

namespace SimpleWallet.Infrastructure.Persistence;

public sealed class WalletQueryStore : IWalletQueries
{
    private readonly string _connectionString;

    public WalletQueryStore(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Wallet")
            ?? throw new InvalidOperationException("Connection string 'Wallet' is missing.");
    }

    public async Task<WalletHomeResponse?> GetHomeAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                w.SpendingBalance,
                w.SavingsBalance,
                w.Currency,
                u.SalaryAmount,
                u.LastSalaryAt,
                c.HolderName,
                c.Last4,
                c.ExpiryMonth,
                c.ExpiryYear,
                c.IsFrozen,
                (
                    SELECT COUNT(*)
                    FROM MoneyRequests r
                    WHERE r.PayerUserId = u.Id AND r.Status = @Pending
                ) AS IncomingRequests
            FROM Wallets w
            INNER JOIN Users u ON u.Id = w.UserId
            LEFT JOIN BankCards c ON c.WalletId = w.Id
            WHERE w.UserId = @UserId
            """;

        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<HomeRow>(new CommandDefinition(
            sql,
            new { UserId = userId, Pending = (int)MoneyRequestStatus.Pending },
            cancellationToken: cancellationToken));
        if (row is null)
            return null;

        CardResponse? card = row.Last4 is null
            ? null
            : new CardResponse(
                row.HolderName ?? "",
                DemoCard.Mask(row.Last4),
                DemoCard.Expiry(row.ExpiryMonth, row.ExpiryYear),
                row.IsFrozen);

        return new WalletHomeResponse(
            row.SpendingBalance,
            row.SavingsBalance,
            row.Currency,
            row.SalaryAmount,
            SalaryCalendar.CanCollect(row.SalaryAmount, row.LastSalaryAt, DateTime.UtcNow),
            row.IncomingRequests,
            card);
    }

    public async Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                e.Id,
                e.Type,
                e.Pocket,
                e.Amount,
                e.BalanceAfter,
                e.Note,
                e.CreatedAt,
                cu.Email AS CounterpartyEmail
            FROM LedgerEntries e
            INNER JOIN Wallets w ON w.Id = e.WalletId
            LEFT JOIN Wallets cw ON cw.Id = e.CounterpartyWalletId
            LEFT JOIN Users cu ON cu.Id = cw.UserId
            WHERE w.UserId = @UserId
            ORDER BY e.CreatedAt DESC, e.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(*)
            FROM LedgerEntries e
            INNER JOIN Wallets w ON w.Id = e.WalletId
            WHERE w.UserId = @UserId;
            """;

        await using var connection = new SqlConnection(_connectionString);
        using var grid = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            new { UserId = userId, Offset = (page - 1) * pageSize, PageSize = pageSize },
            cancellationToken: cancellationToken));

        var rows = (await grid.ReadAsync<TransactionRow>()).ToList();
        var total = await grid.ReadSingleAsync<int>();
        var items = rows.Select(row => new TransactionResponse(
            row.Id,
            TypeName(row.Type),
            PocketName(row.Pocket),
            row.Amount,
            row.BalanceAfter,
            row.CounterpartyEmail,
            row.Note,
            AsUtc(row.CreatedAt))).ToList();

        return new PagedResponse<TransactionResponse>(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<MoneyRequestResponse>> GetRequestsAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                r.Id,
                r.Amount,
                r.Note,
                r.Status,
                r.CreatedAt,
                r.RequesterUserId,
                requester.FullName AS RequesterName,
                requester.Email AS RequesterEmail,
                payer.FullName AS PayerName,
                payer.Email AS PayerEmail
            FROM MoneyRequests r
            INNER JOIN Users requester ON requester.Id = r.RequesterUserId
            INNER JOIN Users payer ON payer.Id = r.PayerUserId
            WHERE r.RequesterUserId = @UserId OR r.PayerUserId = @UserId
            ORDER BY CASE WHEN r.Status = @Pending THEN 0 ELSE 1 END, r.CreatedAt DESC
            """;

        await using var connection = new SqlConnection(_connectionString);
        var rows = await connection.QueryAsync<RequestRow>(new CommandDefinition(
            sql,
            new { UserId = userId, Pending = (int)MoneyRequestStatus.Pending },
            cancellationToken: cancellationToken));

        return rows.Select(row =>
        {
            var incoming = row.RequesterUserId != userId;
            return new MoneyRequestResponse(
                row.Id,
                incoming ? "incoming" : "outgoing",
                StatusName(row.Status),
                row.Amount,
                row.Note,
                incoming ? row.RequesterName : row.PayerName,
                incoming ? row.RequesterEmail : row.PayerEmail,
                AsUtc(row.CreatedAt));
        }).ToList();
    }

    private static string TypeName(int type) => type switch
    {
        (int)LedgerEntryType.Deposit => nameof(LedgerEntryType.Deposit),
        (int)LedgerEntryType.TransferIn => nameof(LedgerEntryType.TransferIn),
        (int)LedgerEntryType.TransferOut => nameof(LedgerEntryType.TransferOut),
        (int)LedgerEntryType.Salary => nameof(LedgerEntryType.Salary),
        (int)LedgerEntryType.BillPayment => nameof(LedgerEntryType.BillPayment),
        (int)LedgerEntryType.CardPayment => nameof(LedgerEntryType.CardPayment),
        (int)LedgerEntryType.MoveToSavings => nameof(LedgerEntryType.MoveToSavings),
        (int)LedgerEntryType.MoveToSpending => nameof(LedgerEntryType.MoveToSpending),
        _ => type.ToString()
    };

    private static string PocketName(int pocket) =>
        pocket == (int)Pocket.Savings ? nameof(Pocket.Savings) : nameof(Pocket.Spending);

    private static string StatusName(int status) => status switch
    {
        (int)MoneyRequestStatus.Pending => nameof(MoneyRequestStatus.Pending),
        (int)MoneyRequestStatus.Accepted => nameof(MoneyRequestStatus.Accepted),
        (int)MoneyRequestStatus.Rejected => nameof(MoneyRequestStatus.Rejected),
        (int)MoneyRequestStatus.Cancelled => nameof(MoneyRequestStatus.Cancelled),
        _ => status.ToString()
    };

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private sealed class HomeRow
    {
        public decimal SpendingBalance { get; set; }
        public decimal SavingsBalance { get; set; }
        public string Currency { get; set; } = "";
        public decimal SalaryAmount { get; set; }
        public DateTime? LastSalaryAt { get; set; }
        public string? HolderName { get; set; }
        public string? Last4 { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public bool IsFrozen { get; set; }
        public int IncomingRequests { get; set; }
    }

    private sealed class TransactionRow
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public int Pocket { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? CounterpartyEmail { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class RequestRow
    {
        public Guid Id { get; set; }
        public decimal Amount { get; set; }
        public string? Note { get; set; }
        public int Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid RequesterUserId { get; set; }
        public string RequesterName { get; set; } = "";
        public string RequesterEmail { get; set; } = "";
        public string PayerName { get; set; } = "";
        public string PayerEmail { get; set; } = "";
    }
}
