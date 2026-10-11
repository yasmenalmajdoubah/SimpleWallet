using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Application.Abstractions;

public interface IUserAccountStore
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);
    Task CreateAsync(User user, Wallet wallet, CancellationToken cancellationToken);
}
