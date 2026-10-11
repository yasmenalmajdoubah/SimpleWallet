using Microsoft.Extensions.DependencyInjection;
using SimpleWallet.Application.Auth;
using SimpleWallet.Application.Wallets;

namespace SimpleWallet.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<WalletService>();
        return services;
    }
}
