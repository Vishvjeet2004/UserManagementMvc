namespace UserManagementMvc.Models;

public partial class MailMessage
{
    public int Id { get; set; }

    public int? SenderUserId { get; set; }

    public string SenderEmail { get; set; } = null!;

    public int? RecipientUserId { get; set; }

    public string RecipientEmail { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public bool IsRead { get; set; }

    public bool IsStarred { get; set; }

    public bool IsDraft { get; set; }

    public DateTime? DraftSavedAt { get; set; }

    public bool IsDeletedBySender { get; set; }

    public bool IsDeletedByRecipient { get; set; }

    public bool IsPermanentlyDeletedBySender { get; set; }

    public bool IsPermanentlyDeletedByRecipient { get; set; }

    public DateTime SentAt { get; set; }

    public DateTime? ReadAt { get; set; }

    public string MessageType { get; set; } = "Internal";

    public int? ParentMessageId { get; set; }

    public string? ExternalMessageId { get; set; }

    public virtual User? SenderUser { get; set; }

    public virtual User? RecipientUser { get; set; }

    public virtual MailMessage? ParentMessage { get; set; }

    public virtual ICollection<MailMessage> Replies { get; set; }
        = new List<MailMessage>();
}