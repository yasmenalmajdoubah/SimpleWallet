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
        var wallet = await LockUserWalletAsync(transaction, userId, cancellationToken);
        wallet.SpendingBalance += amount;
        var referenceId = Guid.NewGuid();
        AddEntry(wallet, LedgerEntryType.Deposit, Pocket.Spending, amount, wallet.SpendingBalance, referenceId, note, null);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return Movement(wallet, referenceId);
    }

    public async Task<MoneyMovementResponse> TransferAsync(
        Guid userId,
        string toEmail,
        decimal amount,
        string? note,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var result = await TransferSpendingAsync(transaction, userId, toEmail, amount, note, cancellationToken);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return result;
    }

    public async Task<SalaryStateResponse> SetSalaryAsync(Guid userId, decimal amount, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        user.SalaryAmount = amount;
        await _db.SaveChangesAsync(cancellationToken);
        return new SalaryStateResponse(user.SalaryAmount, SalaryCalendar.CanCollect(user.SalaryAmount, user.LastSalaryAt, DateTime.UtcNow));
    }

    public async Task<MoneyMovementResponse> CollectSalaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var user = await _db.Users.SingleAsync(item => item.Id == userId, cancellationToken);
        var now = DateTime.UtcNow;
        if (user.SalaryAmount <= 0)
            throw new WalletRuleException(400, "حدد مبلغ الراتب أولًا.");
        if (!SalaryCalendar.CanCollect(user.SalaryAmount, user.LastSalaryAt, now))
            throw new WalletRuleException(400, "قبضت راتب هذا الشهر.");

        var wallet = await LockUserWalletAsync(transaction, userId, cancellationToken);
        wallet.SpendingBalance += user.SalaryAmount;
        user.LastSalaryAt = now;
        var referenceId = Guid.NewGuid();
        AddEntry(wallet, LedgerEntryType.Salary, Pocket.Spending, user.SalaryAmount, wallet.SpendingBalance, referenceId, "راتب", null);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return Movement(wallet, referenceId);
    }

    public async Task<MoneyMovementResponse> MoveAsync(
        Guid userId,
        bool toSavings,
        decimal amount,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var wallet = await LockUserWalletAsync(transaction, userId, cancellationToken);
        var referenceId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        if (toSavings)
        {
            if (wallet.SpendingBalance < amount)
                throw new WalletRuleException(400, "رصيد الصرف غير كافٍ.");
            wallet.SpendingBalance -= amount;
            wallet.SavingsBalance += amount;
            AddEntry(wallet, LedgerEntryType.MoveToSavings, Pocket.Spending, amount, wallet.SpendingBalance, referenceId, "نقل إلى التوفير", null, now);
            AddEntry(wallet, LedgerEntryType.MoveToSavings, Pocket.Savings, amount, wallet.SavingsBalance, referenceId, "نقل من الصرف", null, now);
        }
        else
        {
            if (wallet.SavingsBalance < amount)
                throw new WalletRuleException(400, "رصيد التوفير غير كافٍ.");
            wallet.SavingsBalance -= amount;
            wallet.SpendingBalance += amount;
            AddEntry(wallet, LedgerEntryType.MoveToSpending, Pocket.Savings, amount, wallet.SavingsBalance, referenceId, "نقل إلى الصرف", null, now);
            AddEntry(wallet, LedgerEntryType.MoveToSpending, Pocket.Spending, amount, wallet.SpendingBalance, referenceId, "نقل من التوفير", null, now);
        }

        await SaveAndCommitAsync(transaction, cancellationToken);
        return Movement(wallet, referenceId);
    }

    public async Task<CardResponse> SetCardFrozenAsync(Guid userId, bool frozen, CancellationToken cancellationToken)
    {
        var card = await CardForUserAsync(userId, cancellationToken);
        card.IsFrozen = frozen;
        await _db.SaveChangesAsync(cancellationToken);
        return ToCard(card);
    }

    public async Task<MoneyMovementResponse> PayWithCardAsync(
        Guid userId,
        decimal amount,
        string merchant,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var wallet = await LockUserWalletAsync(transaction, userId, cancellationToken);
        var card = await _db.BankCards.SingleOrDefaultAsync(item => item.WalletId == wallet.Id, cancellationToken)
            ?? throw new WalletRuleException(404, "ما في بطاقة.");
        if (card.IsFrozen)
            throw new WalletRuleException(400, "البطاقة مجمدة.");
        if (wallet.SpendingBalance < amount)
            throw new WalletRuleException(400, "رصيد الصرف غير كافٍ.");

        wallet.SpendingBalance -= amount;
        var referenceId = Guid.NewGuid();
        AddEntry(wallet, LedgerEntryType.CardPayment, Pocket.Spending, amount, wallet.SpendingBalance, referenceId, merchant, null);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return Movement(wallet, referenceId);
    }

    public async Task<MoneyMovementResponse> PayBillAsync(
        Guid userId,
        string billName,
        string reference,
        decimal amount,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var wallet = await LockUserWalletAsync(transaction, userId, cancellationToken);
        if (wallet.SpendingBalance < amount)
            throw new WalletRuleException(400, "رصيد الصرف غير كافٍ.");

        wallet.SpendingBalance -= amount;
        var referenceId = Guid.NewGuid();
        AddEntry(wallet, LedgerEntryType.BillPayment, Pocket.Spending, amount, wallet.SpendingBalance, referenceId, $"{billName} · {reference}", null);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return Movement(wallet, referenceId);
    }

    public async Task CreateRequestAsync(
        Guid userId,
        string payerEmail,
        decimal amount,
        string? note,
        CancellationToken cancellationToken)
    {
        var requesterEmail = await _db.Users
            .Where(user => user.Id == userId)
            .Select(user => user.Email)
            .SingleAsync(cancellationToken);
        if (requesterEmail == payerEmail)
            throw new WalletRuleException(400, "ما بتقدر تطلب حوالة من نفسك.");

        var payerId = await _db.Users
            .Where(user => user.Email == payerEmail)
            .Select(user => (Guid?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (payerId is null)
            throw new WalletRuleException(404, "لا يوجد مستخدم بهذا الإيميل.");

        _db.MoneyRequests.Add(new MoneyRequest
        {
            Id = Guid.NewGuid(),
            RequesterUserId = userId,
            PayerUserId = payerId.Value,
            Amount = amount,
            Note = note,
            Status = MoneyRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MoneyMovementResponse> AcceptRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var request = await LockRequestAsync(transaction, requestId, cancellationToken);
        if (request.PayerUserId != userId)
            throw new WalletRuleException(403, "هذا الطلب مو إلك.");
        EnsurePending(request);

        var requesterEmail = await _db.Users.Where(user => user.Id == request.RequesterUserId).Select(user => user.Email).SingleAsync(cancellationToken);
        var note = string.IsNullOrWhiteSpace(request.Note) ? "طلب حوالة" : request.Note;
        var result = await TransferSpendingAsync(transaction, request.PayerUserId, requesterEmail, request.Amount, note, cancellationToken);
        request.Status = MoneyRequestStatus.Accepted;
        request.ResolvedAt = DateTime.UtcNow;
        await SaveAndCommitAsync(transaction, cancellationToken);
        return result;
    }

    public Task RejectRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        return ResolveRequestAsync(userId, requestId, asPayer: true, MoneyRequestStatus.Rejected, cancellationToken);
    }

    public Task CancelRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        return ResolveRequestAsync(userId, requestId, asPayer: false, MoneyRequestStatus.Cancelled, cancellationToken);
    }

    private async Task ResolveRequestAsync(
        Guid userId,
        Guid requestId,
        bool asPayer,
        MoneyRequestStatus status,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var request = await LockRequestAsync(transaction, requestId, cancellationToken);
        var ownerId = asPayer ? request.PayerUserId : request.RequesterUserId;
        if (ownerId != userId)
            throw new WalletRuleException(403, "هذا الطلب مو إلك.");
        EnsurePending(request);
        request.Status = status;
        request.ResolvedAt = DateTime.UtcNow;
        await SaveAndCommitAsync(transaction, cancellationToken);
    }

    private async Task<MoneyMovementResponse> TransferSpendingAsync(
        IDbContextTransaction transaction,
        Guid userId,
        string toEmail,
        decimal amount,
        string? note,
        CancellationToken cancellationToken)
    {
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
        if (from.SpendingBalance < amount)
            throw new WalletRuleException(400, "رصيد الصرف غير كافٍ.");

        var referenceId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        from.SpendingBalance -= amount;
        to.SpendingBalance += amount;
        AddEntry(from, LedgerEntryType.TransferOut, Pocket.Spending, amount, from.SpendingBalance, referenceId, note, to.Id, now);
        AddEntry(to, LedgerEntryType.TransferIn, Pocket.Spending, amount, to.SpendingBalance, referenceId, note, from.Id, now);
        return Movement(from, referenceId);
    }

    private async Task<Wallet> LockUserWalletAsync(IDbContextTransaction transaction, Guid userId, CancellationToken cancellationToken)
    {
        var walletId = await _db.Wallets
            .Where(wallet => wallet.UserId == userId)
            .Select(wallet => wallet.Id)
            .SingleAsync(cancellationToken);
        await LockWalletAsync(transaction, walletId, cancellationToken);
        return await _db.Wallets.SingleAsync(wallet => wallet.Id == walletId, cancellationToken);
    }

    private async Task<MoneyRequest> LockRequestAsync(
        IDbContextTransaction transaction,
        Guid requestId,
        CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = LockSql("MoneyRequests");
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        BindId(parameter, requestId);
        command.Parameters.Add(parameter);
        var locked = await command.ExecuteScalarAsync(cancellationToken);
        if (locked is null || locked is DBNull)
            throw new WalletRuleException(404, "الطلب غير موجود.");

        return await _db.MoneyRequests.SingleAsync(request => request.Id == requestId, cancellationToken);
    }

    private async Task LockWalletAsync(IDbContextTransaction transaction, Guid walletId, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = LockSql("Wallets");
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        BindId(parameter, walletId);
        command.Parameters.Add(parameter);

        var locked = await command.ExecuteScalarAsync(cancellationToken);
        if (locked is null || locked is DBNull)
            throw new WalletRuleException(404, "المحفظة غير موجودة.");
    }

    private bool UseSqlite => _db.Database.IsSqlite();

    private string LockSql(string table) => UseSqlite
        ? $"SELECT Id FROM {table} WHERE Id = @id"
        : $"SELECT Id FROM {table} WITH (UPDLOCK, ROWLOCK) WHERE Id = @id";

    private void BindId(System.Data.Common.DbParameter parameter, Guid id)
    {
        if (UseSqlite)
        {
            parameter.Value = WalletSql.FormatId(id);
            parameter.DbType = DbType.String;
            return;
        }

        parameter.Value = id;
        parameter.DbType = DbType.Guid;
    }

    private async Task<BankCard> CardForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var card = await _db.BankCards.SingleOrDefaultAsync(item => item.Wallet.UserId == userId, cancellationToken);
        return card ?? throw new WalletRuleException(404, "ما في بطاقة.");
    }

    private void AddEntry(
        Wallet wallet,
        LedgerEntryType type,
        Pocket pocket,
        decimal amount,
        decimal balanceAfter,
        Guid referenceId,
        string? note,
        Guid? counterpartyWalletId,
        DateTime? createdAt = null)
    {
        _db.LedgerEntries.Add(new LedgerEntry
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Type = type,
            Pocket = pocket,
            Amount = amount,
            BalanceAfter = balanceAfter,
            ReferenceId = referenceId,
            CounterpartyWalletId = counterpartyWalletId,
            Note = note,
            CreatedAt = createdAt ?? DateTime.UtcNow
        });
    }

    private static void EnsurePending(MoneyRequest request)
    {
        if (request.Status != MoneyRequestStatus.Pending)
            throw new WalletRuleException(400, "هذا الطلب انحل.");
    }

    private static MoneyMovementResponse Movement(Wallet wallet, Guid referenceId)
    {
        return new MoneyMovementResponse(wallet.SpendingBalance, wallet.SavingsBalance, wallet.Currency, referenceId);
    }

    private static CardResponse ToCard(BankCard card)
    {
        return new CardResponse(card.HolderName, DemoCard.Mask(card.Last4), DemoCard.Expiry(card.ExpiryMonth, card.ExpiryYear), card.IsFrozen);
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
