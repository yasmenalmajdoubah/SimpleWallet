using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleWallet.Application.Abstractions;
using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Infrastructure.Persistence;

public static class DevelopmentDatabase
{
    public const string DemoPassword = "Wallet123!";

    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        if (await db.Users.AnyAsync())
            return;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
        var commands = scope.ServiceProvider.GetRequiredService<IWalletCommands>();

        var ahmed = CreateUser("أحمد", "ahmed@example.com", hasher);
        var sara = CreateUser("سارة", "sara@example.com", hasher);
        await users.CreateAsync(ahmed.User, ahmed.Wallet, CancellationToken.None);
        await users.CreateAsync(sara.User, sara.Wallet, CancellationToken.None);
        await commands.DepositAsync(ahmed.User.Id, 100m, "رصيد تجريبي", CancellationToken.None);
    }

    private static (User User, Wallet Wallet) CreateUser(string fullName, string email, IPasswordHasher hasher)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = hasher.Hash(DemoPassword),
            CreatedAt = DateTime.UtcNow
        };
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Balance = 0,
            Currency = "JOD"
        };
        return (user, wallet);
    }
}
