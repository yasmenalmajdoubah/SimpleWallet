using SimpleWallet.Application.Wallets;

namespace SimpleWallet.Application.Abstractions;

public interface IWalletCommands
{
    Task<MoneyMovementResponse> DepositAsync(Guid userId, decimal amount, string? note, CancellationToken cancellationToken);
    Task<MoneyMovementResponse> TransferAsync(Guid userId, string toEmail, decimal amount, string? note, CancellationToken cancellationToken);
    Task<SalaryStateResponse> SetSalaryAsync(Guid userId, decimal amount, CancellationToken cancellationToken);
    Task<MoneyMovementResponse> CollectSalaryAsync(Guid userId, CancellationToken cancellationToken);
    Task<MoneyMovementResponse> MoveAsync(Guid userId, bool toSavings, decimal amount, CancellationToken cancellationToken);
    Task<CardResponse> SetCardFrozenAsync(Guid userId, bool frozen, CancellationToken cancellationToken);
    Task<MoneyMovementResponse> PayWithCardAsync(Guid userId, decimal amount, string merchant, CancellationToken cancellationToken);
    Task<MoneyMovementResponse> PayBillAsync(Guid userId, string billName, string reference, decimal amount, CancellationToken cancellationToken);
    Task CreateRequestAsync(Guid userId, string payerEmail, decimal amount, string? note, CancellationToken cancellationToken);
    Task<MoneyMovementResponse> AcceptRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken);
    Task RejectRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken);
    Task CancelRequestAsync(Guid userId, Guid requestId, CancellationToken cancellationToken);
}
