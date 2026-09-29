using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using UserManagementMvc.Models;

namespace UserManagementMvc.ViewModels;

public sealed class MailComposeViewModel
{
    public int? DraftId { get; set; }

    [Required]
    [EmailAddress]
    public string RecipientEmail { get; set; } = "";

    public string? CcEmails { get; set; }

    public string? BccEmails { get; set; }

    [Required]
    [StringLength(255)]
    public string Subject { get; set; } = "";

    [Required]
    public string Body { get; set; } = "";

    public int? ReplyToMessageId { get; set; }

    public List<IFormFile> Attachments { get; set; } =
        new();

    public List<MailAttachment> ExistingAttachments { get; set; } =
        new();

    public List<User> Recipients { get; set; } =
        new();

    public List<MailComposerTool> Tools { get; set; } =
        new();

    public List<MailSignature> Signatures { get; set; } =
        new();
}