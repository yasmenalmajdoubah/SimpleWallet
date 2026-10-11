using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SimpleWallet.Application.Abstractions;
using SimpleWallet.Application.Common;
using SimpleWallet.Application.Wallets;
using SimpleWallet.Domain.Entities;
using SimpleWallet.Domain.Enums;

namespace SimpleWallet.Infrastructure.Persistence;

public sealed class WalletCommandStore : IWalletCommands
{
    private readonly AppDbContext _db;

    public WalletCommandStore(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MoneyMovementResponse> DepositAsync(
        Guid userId,
        decimal amount,
        string? note,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var walletId = await _db.Wallets
            .Where(wallet => wallet.UserId == userId)
            .Select(wallet => wallet.Id)
            .SingleAsync(cancellationToken);

        await LockWalletAsync(transaction, walletId, cancellationToken);
        var wallet = await _db.Wallets.SingleAsync(item => item.Id == walletId, cancellationToken);
        wallet.Balance += amount;

        var referenceId = Guid.NewGuid();
        _db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Type = LedgerEntryType.Deposit,
            Amount = amount,
            BalanceAfter = wallet.Balance,
            ReferenceId = referenceId,
            Note = note,
            CreatedAt = DateTime.UtcNow
        });

        await SaveAndCommitAsync(transaction, cancellationToken);
        return new MoneyMovementResponse(wallet.Balance, wallet.Currency, referenceId);
    }

    public async Task<MoneyMovementResponse> TransferAsync(
        Guid userId,
        string toEmail,
        decimal amount,
        string? note,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var sender = await _db.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.UserId == userId)
            .Select(wallet => new { wallet.Id, wallet.User.Email })
            .SingleAsync(cancellationToken);

        if (sender.Email == toEmail)
            throw new WalletRuleException(400, "لا يمكنك التحويل إلى نفسك.");

        var receiverId = await _db.Wallets
            .AsNoTracking()
            .Where(wallet => wallet.User.Email == toEmail)
            .Select(wallet => (Guid?)wallet.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (receiverId is null)
            throw new WalletRuleException(404, "لا يوجد مستخدم بهذا الإيميل.");

        foreach (var walletId in new[] { sender.Id, receiverId.Value }.OrderBy(id => id))
            await LockWalletAsync(transaction, walletId, cancellationToken);

        var from = await _db.Wallets.SingleAsync(wallet => wallet.Id == sender.Id, cancellationToken);
        var to = await _db.Wallets.SingleAsync(wallet => wallet.Id == receiverId.Value, cancellationToken);

        if (from.Balance < amount)
            throw new WalletRuleException(400, "الرصيد غير كافٍ.");

        var referenceId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        from.Balance -= amount;
        to.Balance += amount;

        _db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            WalletId = from.Id,
            Type = LedgerEntryType.TransferOut,
            Amount = amount,
            BalanceAfter = from.Balance,
            ReferenceId = referenceId,
            CounterpartyWalletId = to.Id,
            Note = note,
            CreatedAt = now
        });
        _db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            WalletId = to.Id,
            Type = LedgerEntryType.TransferIn,
            Amount = amount,
            BalanceAfter = to.Balance,
            ReferenceId = referenceId,
            CounterpartyWalletId = from.Id,
            Note = note,
            CreatedAt = now
        });

        await SaveAndCommitAsync(transaction, cancellationToken);
        return new MoneyMovementResponse(from.Balance, from.Currency, referenceId);
    }

    private async Task LockWalletAsync(IDbContextTransaction transaction, Guid walletId, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = "SELECT Id FROM Wallets WITH (UPDLOCK, ROWLOCK) WHERE Id = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = walletId;
        parameter.DbType = DbType.Guid;
        command.Parameters.Add(parameter);

        var locked = await command.ExecuteScalarAsync(cancellationToken);
        if (locked is null || locked is DBNull)
            throw new WalletRuleException(404, "المحفظة غير موجودة.");
    }

    private async Task SaveAndCommitAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new WalletRuleException(409, "تعذر تنفيذ الحركة، حاول مرة ثانية.");
        }
    }
}
