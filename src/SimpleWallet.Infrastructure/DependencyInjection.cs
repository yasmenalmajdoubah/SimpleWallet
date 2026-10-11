using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimpleWallet.Application.Abstractions;
using SimpleWallet.Infrastructure.Identity;
using SimpleWallet.Infrastructure.Persistence;

namespace SimpleWallet.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Wallet")
            ?? throw new InvalidOperationException("Connection string 'Wallet' is missing.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
        services.Configure<JwtOptions>(options =>
        {
            var section = configuration.GetSection(JwtOptions.SectionName);
            options.Issuer = section["Issuer"] ?? "";
            options.Audience = section["Audience"] ?? "";
            options.Key = section["Key"] ?? "";
            if (int.TryParse(section["ExpiresMinutes"], out var minutes))
                options.ExpiresMinutes = minutes;
        });
        services.AddScoped<IUserAccountStore, UserAccountStore>();
        services.AddScoped<IWalletCommands, WalletCommandStore>();
        services.AddScoped<IWalletQueries, WalletQueryStore>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        return services;
    }
}
