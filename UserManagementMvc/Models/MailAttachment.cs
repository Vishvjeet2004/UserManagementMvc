namespace UserManagementMvc.Models;

public partial class MailAttachment
{
    public int Id { get; set; }

    public int MailMessageId { get; set; }

    public string OriginalFileName { get; set; } = null!;

    public string StoredFileName { get; set; } = null!;

    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public string StoragePath { get; set; } = null!;

    public bool IsInline { get; set; }

    public string? ContentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual MailMessage MailMessage { get; set; } = null!;
}