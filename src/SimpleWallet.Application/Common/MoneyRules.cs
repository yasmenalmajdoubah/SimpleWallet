namespace SimpleWallet.Application.Common;

public static class MoneyRules
{
    public const decimal MaxAmount = 1_000_000m;

    public static void EnsureAmount(decimal amount)
    {
        if (amount <= 0)
            throw new WalletRuleException(400, "المبلغ لازم يكون أكبر من صفر.");

        if (amount > MaxAmount)
            throw new WalletRuleException(400, "المبلغ أكبر من الحد المسموح.");

        if (amount != decimal.Round(amount, 2, MidpointRounding.AwayFromZero))
            throw new WalletRuleException(400, "المبلغ لازم يكون بخانتين عشريتين.");
    }

    public static string? NormalizeNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return null;

        var trimmed = note.Trim();
        if (trimmed.Length > 300)
            throw new WalletRuleException(400, "الملاحظة طويلة.");

        return trimmed;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
