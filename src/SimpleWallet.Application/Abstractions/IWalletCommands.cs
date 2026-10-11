using SimpleWallet.Application.Wallets;

namespace SimpleWallet.Application.Abstractions;

public interface IWalletCommands
{
    Task<MoneyMovementResponse> DepositAsync(Guid userId, decimal amount, string? note, CancellationToken cancellationToken);
    Task<MoneyMovementResponse> TransferAsync(Guid userId, string toEmail, decimal amount, string? note, CancellationToken cancellationToken);
}
