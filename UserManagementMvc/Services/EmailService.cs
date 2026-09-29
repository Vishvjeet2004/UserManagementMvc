using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services;

public sealed class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IOptions<SmtpSettings> settings,
        ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string toEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string htmlBody,
        IReadOnlyCollection<EmailAttachment>? attachments = null)
    {
        ValidateSettings(toEmail);

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

        AddRecipients(message.To, toEmail);
        AddRecipients(message.Cc, ccEmails);
        AddRecipients(message.Bcc, bccEmails);

        message.Subject = subject ?? "";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody ?? ""
        };

        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                if (attachment == null ||
                    string.IsNullOrWhiteSpace(attachment.FilePath))
                {
                    continue;
                }

                if (!File.Exists(attachment.FilePath))
                {
                    throw new FileNotFoundException(
                        $"Attachment file was not found: {attachment.FilePath}");
                }

                string contentType =
                    string.IsNullOrWhiteSpace(attachment.ContentType)
                        ? "application/octet-stream"
                        : attachment.ContentType;

                bodyBuilder.Attachments.Add(
                    Path.GetFileName(attachment.FileName),
                    await File.ReadAllBytesAsync(attachment.FilePath),
                    ContentType.Parse(contentType));
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _settings.SenderEmail,
                _settings.Password);

            _logger.LogInformation(
                "Sending email to {Recipient} with {AttachmentCount} attachment(s).",
                toEmail,
                attachments?.Count ?? 0);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        catch
        {
            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
            catch
            {
            }

            throw;
        }
    }

    private void ValidateSettings(string toEmail)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
        {
            throw new ArgumentException(
                "Recipient email address is required.",
                nameof(toEmail));
        }

        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            throw new InvalidOperationException(
                "SMTP Host is not configured.");
        }

        if (_settings.Port <= 0)
        {
            throw new InvalidOperationException(
                "SMTP Port is not configured correctly.");
        }

        if (string.IsNullOrWhiteSpace(_settings.SenderEmail))
        {
            throw new InvalidOperationException(
                "SMTP SenderEmail is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Password))
        {
            throw new InvalidOperationException(
                "SMTP Password is not configured.");
        }
    }

    private static void AddRecipients(
        InternetAddressList list,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        foreach (var item in value.Split(
                     new[] { ',', ';', '\r', '\n' },
                     StringSplitOptions.RemoveEmptyEntries |
                     StringSplitOptions.TrimEntries))
        {
            if (!string.IsNullOrWhiteSpace(item))
            {
                list.Add(MailboxAddress.Parse(item));
            }
        }
    }
}
