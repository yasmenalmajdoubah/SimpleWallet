namespace SimpleWallet.Application.Common;

public class WalletRuleException : Exception
{
    public WalletRuleException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
