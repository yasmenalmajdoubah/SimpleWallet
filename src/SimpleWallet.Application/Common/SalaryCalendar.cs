namespace SimpleWallet.Application.Common;

public static class SalaryCalendar
{
    public static bool CanCollect(decimal salaryAmount, DateTime? lastSalaryAt, DateTime now)
    {
        if (salaryAmount <= 0)
            return false;

        return lastSalaryAt is not DateTime last || last.Year != now.Year || last.Month != now.Month;
    }
}
