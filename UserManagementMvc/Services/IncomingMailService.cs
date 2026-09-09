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

        if (string.IsNullOrWhiteSpace(settings.Host) ||
            settings.Port <= 0 ||
            string.IsNullOrWhiteSpace(settings.Username) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            _logger.LogError(
                "Incoming mail configuration is incomplete.");

            return;
        }

        _logger.LogInformation(
            "Incoming mail service started for {Username}.",
            settings.Username);

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

        _logger.LogInformation(
            "Connected to IMAP server {Host}:{Port}.",
            settings.Host,
            settings.Port);

        await client.AuthenticateAsync(
            settings.Username,
            settings.Password,
            cancellationToken);

        _logger.LogInformation(
            "IMAP authentication successful for {Username}.",
            settings.Username);

        var inbox = client.Inbox;

        await inbox.OpenAsync(
            FolderAccess.ReadOnly,
            cancellationToken);

        var messageIds = await inbox.SearchAsync(
            SearchQuery.DeliveredAfter(
                DateTime.UtcNow.AddDays(-7)),
            cancellationToken);

        _logger.LogInformation(
            "Found {Count} recent messages in Gmail Inbox.",
            messageIds.Count);

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
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to import IMAP message {Uid}.",
                    uid);
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
            _logger.LogWarning(
                "Incoming email skipped because sender email is empty.");

            return;
        }

        senderEmail = senderEmail.Trim().ToLower();

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
                .Where(
                    x => !string.IsNullOrWhiteSpace(x))
                .Select(
                    x => x.Trim().ToLower())
                .Distinct()
                .ToList();

        if (recipientEmails.Count == 0)
        {
            _logger.LogWarning(
                "Incoming email skipped because no recipient was found. Subject: {Subject}",
                subject);

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
            _logger.LogInformation(
                "Incoming email already exists. ExternalMessageId: {ExternalMessageId}",
                externalMessageId);

            return;
        }

        // Load active users first to avoid MySQL translation issues with local collection Contains.
        var activeUsers =
            await context.Users
                .AsNoTracking()
                .Where(
                    x =>
                        x.IsDeleted != true &&
                        x.IsActive == true &&
                        x.Email != null)
                .ToListAsync(cancellationToken);

        // Match Gmail recipients with application users in memory.
        var users =
            activeUsers
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x.Email) &&
                        recipientEmails.Contains(
                            x.Email.Trim().ToLower()))
                .ToList();

        if (users.Count == 0)
        {
            _logger.LogWarning(
                "No active application user found for incoming email. Recipients: {Recipients}",
                string.Join(", ", recipientEmails));

            return;
        }

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
            "Incoming email imported successfully. Sender: {SenderEmail}, Recipients: {Recipients}, Subject: {Subject}",
            senderEmail,
            string.Join(
                ", ",
                users.Select(x => x.Email)),
            subject);
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