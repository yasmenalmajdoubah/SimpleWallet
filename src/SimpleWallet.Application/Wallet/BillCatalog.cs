namespace SimpleWallet.Application.Wallets;

public static class BillCatalog
{
    public static readonly IReadOnlyList<BillResponse> All =
    [
        new("electricity", "كهرباء"),
        new("water", "مياه"),
        new("internet", "إنترنت"),
        new("mobile", "موبايل"),
        new("rent", "إيجار")
    ];

    public static BillResponse? Find(string code)
    {
        return All.FirstOrDefault(bill => bill.Code == code);
    }
}
