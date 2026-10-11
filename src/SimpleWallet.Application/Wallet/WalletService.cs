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

    public Task<WalletBalanceResponse?> GetBalanceAsync(Guid userId, CancellationToken cancellationToken)
    {
        return _queries.GetBalanceAsync(userId, cancellationToken);
    }

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
}
