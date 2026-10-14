namespace SimpleWallet.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public decimal SalaryAmount { get; set; }
    public DateTime? LastSalaryAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public Wallet? Wallet { get; set; }
}
