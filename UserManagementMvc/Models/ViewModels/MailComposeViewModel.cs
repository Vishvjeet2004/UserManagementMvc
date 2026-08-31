using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels;

public class MailComposeViewModel
{
    public int? DraftId { get; set; }

    [Required]
    [EmailAddress]
    public string RecipientEmail { get; set; } = "";

    [Required]
    [StringLength(255)]
    public string Subject { get; set; } = "";

    [Required]
    public string Body { get; set; } = "";

    public int? ReplyToMessageId { get; set; }
}