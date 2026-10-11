using SimpleWallet.Application.Abstractions;
using SimpleWallet.Application.Common;
using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Application.Auth;

public sealed class AuthService
{
    private readonly IUserAccountStore _users;
    private readonly IPasswordHasher _passwords;
    private readonly ITokenIssuer _tokens;

    public AuthService(IUserAccountStore users, IPasswordHasher passwords, ITokenIssuer tokens)
    {
        _users = users;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var fullName = request.FullName.Trim();
        if (fullName.Length == 0)
            throw new WalletRuleException(400, "الاسم مطلوب.");

        var email = MoneyRules.NormalizeEmail(request.Email);
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = _passwords.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };
        var wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Balance = 0,
            Currency = "JOD"
        };

        await _users.CreateAsync(user, wallet, cancellationToken);
        return new AuthResponse(_tokens.CreateToken(user), user.FullName, user.Email);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = MoneyRules.NormalizeEmail(request.Email);
        var user = await _users.FindByEmailAsync(email, cancellationToken);
        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
            throw new WalletRuleException(401, "الإيميل أو كلمة السر غير صحيحة.");

        return new AuthResponse(_tokens.CreateToken(user), user.FullName, user.Email);
    }
}
