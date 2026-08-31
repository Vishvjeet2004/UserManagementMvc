using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services;

public class IncomingMailService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IncomingMailService> _logger;

    public IncomingMailService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<IncomingMailService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var settings = _configuration
            .GetSection("IncomingMailSettings")
            .Get<IncomingMailSettings>();

        if (settings == null)
        {
            _logger.LogError(
                "IncomingMailSettings configuration is missing.");

            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ImportIncomingEmailsAsync(
                    settings,
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Incoming email processing failed.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(
                        Math.Max(
                            settings.PollIntervalSeconds,
                            10)),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ImportIncomingEmailsAsync(
        IncomingMailSettings settings,
        CancellationToken cancellationToken)
    {
        using var client = new ImapClient();

        var socketOption = settings.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;

        await client.ConnectAsync(
            settings.Host,
            settings.Port,
            socketOption,
            cancellationToken);

        await client.AuthenticateAsync(
            settings.Username,
            settings.Password,
            cancellationToken);

        var inbox = client.Inbox;

        await inbox.OpenAsync(
            FolderAccess.ReadWrite,
            cancellationToken);

        var messageIds = await inbox.SearchAsync(
            SearchQuery.NotSeen,
            cancellationToken);

        foreach (var uid in messageIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                var message = await inbox.GetMessageAsync(
                    uid,
                    cancellationToken);

                await SaveIncomingMessageAsync(
                    message,
                    cancellationToken);

                await inbox.AddFlagsAsync(
                    uid,
                    MessageFlags.Seen,
                    true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to import incoming email.");
            }
        }

        await client.DisconnectAsync(
            true,
            cancellationToken);
    }

    private async Task SaveIncomingMessageAsync(
        MimeMessage message,
        CancellationToken cancellationToken)
    {
        string externalMessageId =
            message.MessageId?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(externalMessageId))
        {
            externalMessageId =
                Guid.NewGuid().ToString("N");
        }

        string senderEmail =
            message.From.Mailboxes
                .Select(x => x.Address)
                .FirstOrDefault()
            ?? "";

        if (string.IsNullOrWhiteSpace(senderEmail))
        {
            return;
        }

        string subject =
            string.IsNullOrWhiteSpace(message.Subject)
                ? "(No subject)"
                : message.Subject.Trim();

        string body =
            GetMessageBody(message);

        var recipientEmails =
            message.To.Mailboxes
                .Select(x => x.Address)
                .Concat(
                    message.Cc.Mailboxes
                        .Select(x => x.Address))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToLower())
                .Distinct()
                .ToList();

        if (recipientEmails.Count == 0)
        {
            return;
        }

        using var scope =
            _scopeFactory.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        var existingMessage =
            await context.MailMessages
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.ExternalMessageId ==
                        externalMessageId,
                    cancellationToken);

        if (existingMessage)
        {
            return;
        }

        var users =
            await context.Users
                .Where(
                    x =>
                        x.IsDeleted != true &&
                        x.IsActive == true &&
                        x.Email != null &&
                        recipientEmails.Contains(
                            x.Email.ToLower()))
                .ToListAsync(cancellationToken);

        foreach (var user in users)
        {
            var mail = new MailMessage
            {
                SenderUserId = null,
                SenderEmail = senderEmail,
                RecipientUserId = user.Id,
                RecipientEmail = user.Email,
                Subject = subject,
                Body = body,
                IsRead = false,
                IsStarred = false,
                IsDraft = false,
                DraftSavedAt = null,
                IsDeletedBySender = false,
                IsDeletedByRecipient = false,
                IsPermanentlyDeletedBySender = false,
                IsPermanentlyDeletedByRecipient = false,
                SentAt = message.Date.LocalDateTime,
                ReadAt = null,
                MessageType = "External",
                ParentMessageId = null,
                ExternalMessageId = externalMessageId
            };

            context.MailMessages.Add(mail);
        }

        await context.SaveChangesAsync(
            cancellationToken);

        _logger.LogInformation(
            "Incoming email imported from {SenderEmail}.",
            senderEmail);
    }

    private static string GetMessageBody(
        MimeMessage message)
    {
        if (!string.IsNullOrWhiteSpace(
                message.TextBody))
        {
            return message.TextBody;
        }

        if (!string.IsNullOrWhiteSpace(
                message.HtmlBody))
        {
            return message.HtmlBody;
        }

        return "";
    }
}