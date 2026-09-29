namespace UserManagementMvc.Services;

public sealed class ServerMailMessage
{
    public string Id { get; set; } = "";

    public uint Uid { get; set; }

    public string FolderName { get; set; } = "INBOX";

    public string SenderEmail { get; set; } = "";

    public string RecipientEmail { get; set; } = "";

    public string? CcEmails { get; set; }

    public string? BccEmails { get; set; }

    public string Subject { get; set; } = "";

    public string Body { get; set; } = "";

    public bool IsRead { get; set; }

    public bool IsStarred { get; set; }

    public bool IsDraft { get; set; }

    public DateTime SentAt { get; set; }

    public string MessageType { get; set; } = "External";

    public List<ServerMailAttachment> Attachments { get; set; } = new();
}