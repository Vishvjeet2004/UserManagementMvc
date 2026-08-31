using System;
using System.Collections.Generic;

namespace UserManagementMvc.Models;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Mobile { get; set; }

    public string PasswordHash { get; set; } = null!;

    public string? Role { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? LastSeenAt { get; set; }

    public string? UserName { get; set; }

    public string? SecurityQuestion { get; set; }

    public string? SecurityAnswerHash { get; set; }

    public string? Department { get; set; }

    public bool? IsActive { get; set; }

    public string? MobileCountryCode { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public virtual ICollection<Emailotp> Emailotps { get; set; }
    = new List<Emailotp>();

    public virtual ICollection<Usersecurityquestion> Usersecurityquestions { get; set; } = new List<Usersecurityquestion>();


    public virtual ICollection<ChatMessage> SentChatMessages { get; set; }
    = new List<ChatMessage>();

    public virtual ICollection<ChatMessage> ReceivedChatMessages { get; set; }
        = new List<ChatMessage>();

    



    // Login lockout properties
    public int FailedLoginAttempts { get; set; }

    public bool IsBlocked { get; set; }

    public DateTime? BlockedAt { get; set; }

    public DateTime? UnblockedAt { get; set; }

    public int? UnblockedBy { get; set; }

}
