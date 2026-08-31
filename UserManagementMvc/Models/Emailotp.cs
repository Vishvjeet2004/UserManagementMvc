namespace UserManagementMvc.Models;

public partial class Emailotp
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Email { get; set; } = null!;

    public string OtpHash { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsUsed { get; set; }

    public int AttemptCount { get; set; }

    public virtual User? User { get; set; }
}