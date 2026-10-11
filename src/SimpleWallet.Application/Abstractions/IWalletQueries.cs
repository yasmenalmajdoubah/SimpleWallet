using SimpleWallet.Application.Wallets;

namespace SimpleWallet.Application.Abstractions;

public interface IWalletQueries
{
    Task<WalletBalanceResponse?> GetBalanceAsync(Guid userId, CancellationToken cancellationToken);
    Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
}
