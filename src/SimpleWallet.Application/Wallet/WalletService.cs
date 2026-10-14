using SimpleWallet.Application.Abstractions;
using SimpleWallet.Application.Common;

namespace SimpleWallet.Application.Wallets;

public sealed class WalletService
{
    private readonly IWalletCommands _commands;
    private readonly IWalletQueries _queries;

    public WalletService(IWalletCommands commands, IWalletQueries queries)
    {
        _commands = commands;
        _queries = queries;
    }

    public Task<WalletHomeResponse?> GetHomeAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _queries.GetHomeAsync(userId, cancellationToken);
    }

    public IReadOnlyList<BillResponse> Bills() => BillCatalog.All;

    public Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 50);
        return _queries.GetTransactionsAsync(userId, page, pageSize, cancellationToken);
    }

    public Task<IReadOnlyList<MoneyRequestResponse>> GetRequestsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _queries.GetRequestsAsync(userId, cancellationToken);
    }

    public Task<MoneyMovementResponse> DepositAsync(Guid userId, DepositRequest request, CancellationToken cancellationToken)
    {
        MoneyRules.EnsureAmount(request.Amount);
        return _commands.DepositAsync(userId, request.Amount, MoneyRules.NormalizeNote(request.Note), cancellationToken);
    }

    public Task<MoneyMovementResponse> TransferAsync(Guid userId, TransferRequest request, CancellationToken cancellationToken)
    {
        MoneyRules.EnsureAmount(request.Amount);
        var email = MoneyRules.NormalizeEmail(request.ToEmail);
        return _commands.TransferAsync(userId, email, request.Amount, MoneyRules.NormalizeNote(request.Note), cancellationToken);
    }

    public Task<SalaryStateResponse> SetSalaryAsync(Guid userId, SalaryRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount < 0)
            throw new WalletRuleException(400, "الراتب ما بيكون سالب.");
        if (request.Amount > 0)
            MoneyRules.EnsureAmount(request.Amount);
        return _commands.SetSalaryAsync(userId, request.Amount, cancellationToken);
    }

    public Task<MoneyMovementResponse> CollectSalaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _commands.CollectSalaryAsync(userId, cancellationToken);
    }

    public Task<MoneyMovementResponse> MoveAsync(Guid userId, MoveMoneyRequest request, CancellationToken cancellationToken)
    {
        MoneyRules.EnsureAmount(request.Amount);
        return _commands.MoveAsync(userId, request.ToSavings, request.Amount, cancellationToken);
    }

    public Task<CardResponse> SetCardFrozenAsync(Guid userId, FreezeCardRequest request, CancellationToken cancellationToken)
    {
        return _commands.SetCardFrozenAsync(userId, request.Frozen, cancellationToken);
    }

    public Task<MoneyMovementResponse> PayWithCardAsync(Guid userId, CardPaymentRequest request, CancellationToken cancellationToken)
    {
        MoneyRules.EnsureAmount(request.Amount);
        var merchant = request.Merchant.Trim();
        if (merchant.Length == 0)
            throw new WalletRuleException(400, "اسم التاجر مطلوب.");
        return _commands.PayWithCardAsync(userId, request.Amount, merchant, cancellationToken);
    }

    public Task<MoneyMovementResponse> PayBillAsync(Guid userId, PayBillRequest request, CancellationToken cancellationToken)
    {
        var bill = BillCatalog.Find(request.Code.Trim());
        if (bill is null)
            throw new WalletRuleException(400, "نوع الفاتورة غير معروف.");
        MoneyRules.EnsureAmount(request.Amount);
        var reference = request.Reference.Trim();
        if (reference.Length == 0)
            throw new WalletRuleException(400, "رقم الاشتراك مطلوب.");
        return _commands.PayBillAsync(userId, bill.Name, reference, request.Amount, cancellationToken);
    }

    public Task CreateRequestAsync(Guid userId, CreateMoneyRequest request, CancellationToken cancellationToken)
    {
        MoneyRules.EnsureAmount(request.Amount);
        return _commands.CreateRequestAsync(
            userId,
            MoneyRules.NormalizeEmail(request.PayerEmail),
            request.Amount,
            MoneyRules.NormalizeNote(request.Note),
            cancellationToken);
    }

    public Task<MoneyMovementResponse> AcceptRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        return _commands.AcceptRequestAsync(userId, requestId, cancellationToken);
    }

    public Task RejectRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        return _commands.RejectRequestAsync(userId, requestId, cancellationToken);
    }

    public Task CancelRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken)
    {
        return _commands.CancelRequestAsync(userId, requestId, cancellationToken);
    }
}
