using SimpleWallet.Domain.Entities;

namespace SimpleWallet.Application.Abstractions;

public interface ITokenIssuer
{
    string CreateToken(User user);
}
