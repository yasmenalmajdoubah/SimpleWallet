using SimpleWallet.Application.Wallets;

namespace SimpleWallet.Application.Abstractions;

public interface IWalletQueries
{
    Task<WalletHomeResponse?> GetHomeAsync(Guid userId, CancellationToken cancellationToken);
    Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<MoneyRequestResponse>> GetRequestsAsync(Guid userId, CancellationToken cancellationToken);
}
