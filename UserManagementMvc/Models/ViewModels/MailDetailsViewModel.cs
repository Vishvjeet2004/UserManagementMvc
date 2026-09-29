using UserManagementMvc.Models;
using UserManagementMvc.Services;

namespace UserManagementMvc.ViewModels;

public class MailDetailsViewModel
{
    public int Id { get; set; }

    public string SenderEmail { get; set; } = "";

    public string RecipientEmail { get; set; } = "";

    public string Subject { get; set; } = "";

    public string Body { get; set; } = "";

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public string MessageType { get; set; } = "";

    public bool IsStarred { get; set; }

    public string? CcEmails { get; set; }

    public string? BccEmails { get; set; }

    public List<MailAttachment> Attachments { get; set; } = new();

    public MailMessage? DatabaseMessage { get; set; }

    public ServerMailMessage? ServerMessage { get; set; }

    public bool IsServerMessage =>
        ServerMessage != null;
}