using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services;

public class EmailService : IEmailService
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

        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _settings.SenderName,
                _settings.SenderEmail));

        AddRecipients(message.To, toEmail);
        AddRecipients(message.Cc, ccEmails);
        AddRecipients(message.Bcc, bccEmails);

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                if (string.IsNullOrWhiteSpace(attachment.FilePath))
                {
                    continue;
                }

                if (!File.Exists(attachment.FilePath))
                {
                    continue;
                }

                bodyBuilder.Attachments.Add(
                    attachment.FilePath,
                    ParseContentType(attachment.ContentType));
            }
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            _logger.LogInformation(
                "Connecting to SMTP server {Host}:{Port}",
                _settings.Host,
                _settings.Port);

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _settings.SenderEmail,
                _settings.Password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);

            _logger.LogInformation(
                "Email successfully sent to {Email}.",
                toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Email sending failed for {Email}.",
                toEmail);

            try
            {
                if (client.IsConnected)
                {
                    await client.DisconnectAsync(true);
                }
            }
            catch
            {
                // SMTP disconnect failure is intentionally ignored.
            }

            throw;
        }
    }

    private static void AddRecipients(
        InternetAddressList list,
        string? emails)
    {
        if (string.IsNullOrWhiteSpace(emails))
        {
            return;
        }

        var values = emails.Split(
            new[] { ',', ';', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);

        foreach (var email in values)
        {
            if (MailboxAddress.TryParse(email, out var mailbox))
            {
                list.Add(mailbox);
            }
            else
            {
                throw new FormatException(
                    $"Invalid email address: {email}");
            }
        }
    }

    private static ContentType ParseContentType(
        string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return new ContentType(
                "application",
                "octet-stream");
        }

        try
        {
            return ContentType.Parse(contentType);
        }
        catch
        {
            return new ContentType(
                "application",
                "octet-stream");
        }
    }
}