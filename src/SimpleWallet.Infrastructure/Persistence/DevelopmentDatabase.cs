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
        if (!await db.Users.AnyAsync())
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var users = scope.ServiceProvider.GetRequiredService<IUserAccountStore>();
            var commands = scope.ServiceProvider.GetRequiredService<IWalletCommands>();

            var ahmed = CreateUser("أحمد", "ahmed@example.com", hasher);
            var sara = CreateUser("سارة", "sara@example.com", hasher);
            await users.CreateAsync(ahmed.User, ahmed.Wallet, CancellationToken.None);
            await users.CreateAsync(sara.User, sara.Wallet, CancellationToken.None);
            await commands.DepositAsync(ahmed.User.Id, 100m, "رصيد تجريبي", CancellationToken.None);
        }

        await EnsureCardsAsync(db);
        await EnsureDemoSalaryAsync(db);
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
            SpendingBalance = 0,
            SavingsBalance = 0,
            Currency = "JOD"
        };
        return (user, wallet);
    }

    private static async Task EnsureCardsAsync(AppDbContext db)
    {
        var missing = await db.Wallets
            .Where(wallet => !db.BankCards.Any(card => card.WalletId == wallet.Id))
            .Select(wallet => new { wallet.Id, wallet.User.FullName })
            .ToListAsync();

        foreach (var wallet in missing)
            db.BankCards.Add(DemoCard.Create(wallet.Id, wallet.FullName));

        if (missing.Count > 0)
            await db.SaveChangesAsync();
    }

    private static async Task EnsureDemoSalaryAsync(AppDbContext db)
    {
        var ahmed = await db.Users.SingleOrDefaultAsync(user => user.Email == "ahmed@example.com");
        if (ahmed is null || ahmed.SalaryAmount != 0 || ahmed.LastSalaryAt is not null)
            return;

        ahmed.SalaryAmount = 450m;
        await db.SaveChangesAsync();
    }
}
