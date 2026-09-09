using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services;

public class IncomingMailService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IncomingMailService> _logger;
    private readonly IWebHostEnvironment _environment;

    public IncomingMailService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<IncomingMailService> logger,
        IWebHostEnvironment environment)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
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

        senderEmail =
            senderEmail.Trim().ToLowerInvariant();

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
                    x => x.Trim().ToLowerInvariant())
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
                .ToListAsync(
                    cancellationToken);

        // Match incoming Gmail recipients with application users in memory.
        var users =
            activeUsers
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(x.Email) &&
                        recipientEmails.Contains(
                            x.Email.Trim().ToLowerInvariant()))
                .ToList();

        if (users.Count == 0)
        {
            _logger.LogWarning(
                "No active application user found for incoming email. Recipients: {Recipients}",
                string.Join(
                    ", ",
                    recipientEmails));

            return;
        }

        var mails =
            new List<MailMessage>();

        foreach (var user in users)
        {
            var mail = new MailMessage
            {
                SenderUserId = null,
                SenderEmail = senderEmail,
                RecipientUserId = user.Id,
                RecipientEmail = user.Email,
                CcEmails = null,
                BccEmails = null,
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

            mails.Add(mail);

            context.MailMessages.Add(mail);
        }

        await context.SaveChangesAsync(
            cancellationToken);

        // Save normal attachments as well as Gmail inline/embedded images.
        await SaveIncomingAttachmentsAsync(
            message,
            mails,
            context,
            cancellationToken);

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

    private async Task SaveIncomingAttachmentsAsync(
        MimeMessage message,
        IReadOnlyCollection<MailMessage> mails,
        AppDbContext context,
        CancellationToken cancellationToken)
    {
        var attachmentParts =
            GetAttachmentParts(message);

        if (attachmentParts.Count == 0)
        {
            _logger.LogInformation(
                "Incoming email has no supported file attachments or inline images. Subject: {Subject}",
                message.Subject);

            return;
        }

        var attachmentSettings =
            _configuration
                .GetSection("MailAttachmentSettings");

        int maxFiles =
            attachmentSettings
                .GetValue<int?>("MaxFilesPerMail")
            ?? 10;

        long maxFileSize =
            (
                attachmentSettings
                    .GetValue<long?>("MaxFileSizeMb")
                ?? 25
            ) *
            1024L *
            1024L;

        var allowedExtensions =
            attachmentSettings
                .GetSection("AllowedExtensions")
                .Get<string[]>()
            ?? Array.Empty<string>();

        var storageDirectory =
            Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "MailAttachments");

        Directory.CreateDirectory(
            storageDirectory);

        int savedAttachmentCount = 0;

        foreach (var attachment in attachmentParts)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (savedAttachmentCount >= maxFiles)
            {
                _logger.LogWarning(
                    "Maximum attachment count reached for incoming email. Subject: {Subject}",
                    message.Subject);

                break;
            }

            string originalFileName =
                GetSafeFileName(
                    attachment.FileName);

            // Gmail inline images can sometimes arrive without a normal filename.
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                originalFileName =
                    CreateInlineImageFileName(
                        attachment);
            }

            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                originalFileName =
                    "attachment";
            }

            string extension =
                Path.GetExtension(
                    originalFileName)
                .ToLowerInvariant();

            // If the inline image has no extension, derive it from its MIME content type.
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension =
                    GetExtensionFromContentType(
                        attachment.ContentType?.MimeType);

                if (!string.IsNullOrWhiteSpace(extension))
                {
                    originalFileName += extension;
                }
            }

            if (allowedExtensions.Length > 0 &&
                !IsExtensionAllowed(
                    extension,
                    allowedExtensions))
            {
                _logger.LogWarning(
                    "Incoming attachment skipped because extension is not allowed. File: {FileName}, ContentType: {ContentType}",
                    originalFileName,
                    attachment.ContentType?.MimeType);

                continue;
            }

            string storedFileName =
                $"{Guid.NewGuid():N}{extension}";

            string physicalPath =
                Path.Combine(
                    storageDirectory,
                    storedFileName);

            long fileSize;

            try
            {
                await using var fileStream =
                    new FileStream(
                        physicalPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        81920,
                        FileOptions.Asynchronous);

                await attachment.Content.DecodeToAsync(
                    fileStream,
                    cancellationToken);

                fileSize =
                    fileStream.Length;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to save incoming attachment {FileName}.",
                    originalFileName);

                TryDeleteFile(
                    physicalPath);

                continue;
            }

            if (fileSize <= 0)
            {
                _logger.LogWarning(
                    "Incoming attachment skipped because the file is empty. File: {FileName}",
                    originalFileName);

                TryDeleteFile(
                    physicalPath);

                continue;
            }

            if (fileSize > maxFileSize)
            {
                _logger.LogWarning(
                    "Incoming attachment skipped because file size exceeds the configured limit. File: {FileName}, Size: {FileSize}",
                    originalFileName,
                    fileSize);

                TryDeleteFile(
                    physicalPath);

                continue;
            }

            bool isInline =
                IsInlineAttachment(
                    attachment);

            foreach (var mail in mails)
            {
                var mailAttachment =
                    new MailAttachment
                    {
                        MailMessageId =
                            mail.Id,

                        OriginalFileName =
                            originalFileName,

                        StoredFileName =
                            storedFileName,

                        ContentType =
                            attachment
                                .ContentType
                                ?.MimeType
                            ?? "application/octet-stream",

                        FileSize =
                            fileSize,

                        StoragePath =
                            physicalPath,

                        IsInline =
                            isInline,

                        ContentId =
                            CleanContentId(
                                attachment.ContentId),

                        CreatedAt =
                            DateTime.Now
                    };

                context.MailAttachments.Add(
                    mailAttachment);
            }

            savedAttachmentCount++;

            _logger.LogInformation(
                "Incoming attachment saved successfully. File: {FileName}, ContentType: {ContentType}, Size: {FileSize}, Inline: {IsInline}, ContentId: {ContentId}, Recipients: {RecipientCount}",
                originalFileName,
                attachment.ContentType?.MimeType,
                fileSize,
                isInline,
                attachment.ContentId,
                mails.Count);
        }

        _logger.LogInformation(
            "Incoming email attachment processing completed. Saved {SavedCount} of {DetectedCount} detected parts.",
            savedAttachmentCount,
            attachmentParts.Count);
    }

    private static List<MimePart> GetAttachmentParts(
        MimeMessage message)
    {
        var parts =
            new List<MimePart>();

        var visited =
            new HashSet<MimePart>();

        CollectMimeParts(
            message.Body,
            parts,
            visited);

        return parts;
    }

    private static void CollectMimeParts(
        MimeEntity? entity,
        List<MimePart> parts,
        HashSet<MimePart> visited)
    {
        if (entity == null)
        {
            return;
        }

        if (entity is MimePart mimePart)
        {
            if (visited.Add(mimePart) &&
                IsFileOrImagePart(mimePart))
            {
                parts.Add(mimePart);
            }

            return;
        }

        if (entity is Multipart multipart)
        {
            foreach (var child in multipart)
            {
                CollectMimeParts(
                    child,
                    parts,
                    visited);
            }
        }
    }

    private static bool IsFileOrImagePart(
        MimePart part)
    {
        bool hasFileName =
            !string.IsNullOrWhiteSpace(
                part.FileName);

        bool hasContentId =
            !string.IsNullOrWhiteSpace(
                part.ContentId);

        string mediaType =
            part.ContentType?.MediaType
            ?? "";

        bool isImage =
            mediaType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase);

        string disposition =
            part.ContentDisposition?.Disposition
            ?? "";

        bool isAttachment =
            string.Equals(
                disposition,
                "attachment",
                StringComparison.OrdinalIgnoreCase);

        bool isInline =
            string.Equals(
                disposition,
                "inline",
                StringComparison.OrdinalIgnoreCase);

        // Normal files, explicit attachments, inline images and CID images are supported.
        return hasFileName ||
               hasContentId ||
               isImage ||
               isAttachment ||
               isInline;
    }

    private static bool IsInlineAttachment(
        MimePart attachment)
    {
        string disposition =
            attachment.ContentDisposition?.Disposition
            ?? "";

        bool inlineDisposition =
            string.Equals(
                disposition,
                "inline",
                StringComparison.OrdinalIgnoreCase);

        bool hasContentId =
            !string.IsNullOrWhiteSpace(
                attachment.ContentId);

        bool isImage =
            attachment.ContentType?.MediaType
                ?.StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase)
            ?? false;

        return inlineDisposition ||
               hasContentId ||
               isImage;
    }

    private static string CreateInlineImageFileName(
        MimePart attachment)
    {
        string extension =
            GetExtensionFromContentType(
                attachment.ContentType?.MimeType);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        return $"inline-image{extension}";
    }

    private static string GetExtensionFromContentType(
        string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return "";
        }

        return contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/bmp" => ".bmp",
            "image/svg+xml" => ".svg",
            "application/pdf" => ".pdf",
            "text/plain" => ".txt",
            "application/zip" => ".zip",
            "application/msword" => ".doc",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
            "application/vnd.ms-excel" => ".xls",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
            "application/vnd.ms-powerpoint" => ".ppt",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
            _ => ""
        };
    }

    private static bool IsExtensionAllowed(
        string extension,
        IEnumerable<string> allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        return allowedExtensions.Any(
            allowed =>
                string.Equals(
                    allowed?.Trim(),
                    extension,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static string CleanContentId(
        string? contentId)
    {
        if (string.IsNullOrWhiteSpace(contentId))
        {
            return "";
        }

        return contentId
            .Trim()
            .Trim('<', '>');
    }

    private static string GetSafeFileName(
        string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return "";
        }

        string cleanedFileName =
            Path.GetFileName(
                fileName.Trim());

        foreach (char invalidCharacter in
                 Path.GetInvalidFileNameChars())
        {
            cleanedFileName =
                cleanedFileName.Replace(
                    invalidCharacter,
                    '_');
        }

        return cleanedFileName;
    }

    private static void TryDeleteFile(
        string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore cleanup errors because the original processing error is already logged.
        }
    }

    private static string GetMessageBody(
        MimeMessage message)
    {
        if (!string.IsNullOrWhiteSpace(
                message.HtmlBody))
        {
            return message.HtmlBody;
        }

        if (!string.IsNullOrWhiteSpace(
                message.TextBody))
        {
            return message.TextBody;
        }

        return "";
    }
}