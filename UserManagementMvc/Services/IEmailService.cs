namespace UserManagementMvc.Services;

public interface IEmailService
{
    Task SendEmailAsync(
        string toEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string htmlBody,
        IReadOnlyCollection<EmailAttachment>? attachments = null);
}