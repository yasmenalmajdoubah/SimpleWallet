using System.ComponentModel.DataAnnotations;

namespace SimpleWallet.Application.Auth;

public sealed class RegisterRequest
{
    [Required(ErrorMessage = "الاسم مطلوب.")]
    [MaxLength(120, ErrorMessage = "الاسم طويل.")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "الإيميل مطلوب.")]
    [EmailAddress(ErrorMessage = "الإيميل غير صحيح.")]
    [MaxLength(256, ErrorMessage = "الإيميل طويل.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "كلمة السر مطلوبة.")]
    [MinLength(8, ErrorMessage = "كلمة السر لازم تكون 8 أحرف على الأقل.")]
    public string Password { get; set; } = "";
}

public sealed class LoginRequest
{
    [Required(ErrorMessage = "الإيميل مطلوب.")]
    [EmailAddress(ErrorMessage = "الإيميل غير صحيح.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "كلمة السر مطلوبة.")]
    public string Password { get; set; } = "";
}

public sealed record AuthResponse(string Token, string FullName, string Email);
