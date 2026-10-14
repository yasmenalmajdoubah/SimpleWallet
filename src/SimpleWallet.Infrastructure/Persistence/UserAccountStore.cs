using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SimpleWallet.Application.Abstractions;
using SimpleWallet.Application.Common;
using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Infrastructure.Persistence;

public sealed class UserAccountStore : IUserAccountStore
{
    private readonly AppDbContext _db;

    public UserAccountStore(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _db.Users.SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public async Task CreateAsync(User user, Wallet wallet, CancellationToken cancellationToken)
    {
        _db.Users.Add(user);
        _db.Wallets.Add(wallet);
        _db.BankCards.Add(DemoCard.Create(wallet.Id, user.FullName));
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new WalletRuleException(400, "هذا الإيميل مستخدم.");
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sql && (sql.Number == 2601 || sql.Number == 2627);
    }
}
