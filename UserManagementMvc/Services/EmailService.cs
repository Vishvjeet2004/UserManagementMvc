using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services
{
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
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException(
                    "Recipient email address is required.",
                    nameof(toEmail));
            }

            if (string.IsNullOrWhiteSpace(
                _settings.Host))
            {
                throw new InvalidOperationException(
                    "SMTP Host is not configured.");
            }

            if (_settings.Port <= 0)
            {
                throw new InvalidOperationException(
                    "SMTP Port is not configured correctly.");
            }

            if (string.IsNullOrWhiteSpace(
                _settings.SenderEmail))
            {
                throw new InvalidOperationException(
                    "SMTP SenderEmail is not configured.");
            }

            if (string.IsNullOrWhiteSpace(
                _settings.Password))
            {
                throw new InvalidOperationException(
                    "SMTP Password is not configured.");
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _settings.SenderName,
                    _settings.SenderEmail));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };

            message.Body =
                bodyBuilder.ToMessageBody();

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

                _logger.LogInformation(
                    "SMTP connection successful.");

                await client.AuthenticateAsync(
                    _settings.SenderEmail,
                    _settings.Password);

                _logger.LogInformation(
                    "SMTP authentication successful for {Email}.",
                    _settings.SenderEmail);

                await client.SendAsync(message);

                _logger.LogInformation(
                    "Email successfully sent to {Email}.",
                    toEmail);

                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Email sending failed. Host={Host}, Port={Port}, Sender={Sender}, Recipient={Recipient}",
                    _settings.Host,
                    _settings.Port,
                    _settings.SenderEmail,
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
                    // Disconnect failure ko ignore karo.
                }

                throw;
            }
        }
    }
}