using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using SimpleWallet.Application.Abstractions;
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

    public async Task<WalletBalanceResponse?> GetBalanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Balance, Currency
            FROM Wallets
            WHERE UserId = @UserId
            """;

        await using var connection = new SqlConnection(_connectionString);
        var row = await connection.QuerySingleOrDefaultAsync<BalanceRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));

        return row is null ? null : new WalletBalanceResponse(row.Balance, row.Currency);
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
            row.Amount,
            row.BalanceAfter,
            row.CounterpartyEmail,
            row.Note,
            AsUtc(row.CreatedAt))).ToList();

        return new PagedResponse<TransactionResponse>(items, page, pageSize, total);
    }

    private static string TypeName(int type) => type switch
    {
        (int)LedgerEntryType.Deposit => nameof(LedgerEntryType.Deposit),
        (int)LedgerEntryType.TransferIn => nameof(LedgerEntryType.TransferIn),
        (int)LedgerEntryType.TransferOut => nameof(LedgerEntryType.TransferOut),
        _ => type.ToString()
    };

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    private sealed class BalanceRow
    {
        public decimal Balance { get; set; }
        public string Currency { get; set; } = "";
    }

    private sealed class TransactionRow
    {
        public Guid Id { get; set; }
        public int Type { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string? CounterpartyEmail { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
