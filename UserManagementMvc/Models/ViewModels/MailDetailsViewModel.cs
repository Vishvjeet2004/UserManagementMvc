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
}