using Ganss.Xss;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services;

public class MailService : IMailService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly MailHtmlSanitizer _htmlSanitizer;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public MailService(
        AppDbContext context,
        IEmailService emailService,
        MailHtmlSanitizer htmlSanitizer,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _htmlSanitizer = htmlSanitizer;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<bool> SendMailAsync(
        int senderUserId,
        string recipientEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string body,
        int? parentMessageId = null,
        IReadOnlyList<IFormFile>? attachments = null)
    {
        var sender =
            await GetActiveUserAsync(
                senderUserId);

        if (sender == null)
        {
            return false;
        }

        recipientEmail =
            recipientEmail?.Trim() ?? "";

        subject =
            subject?.Trim() ?? "";

        string safeBody =
            _htmlSanitizer.Sanitize(
                body ?? "");

        if (
            string.IsNullOrWhiteSpace(
                recipientEmail) ||
            string.IsNullOrWhiteSpace(
                subject) ||
            string.IsNullOrWhiteSpace(
                GetPlainText(safeBody)))
        {
            return false;
        }

        var recipient =
            await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Email == recipientEmail &&
                    x.IsDeleted != true &&
                    x.IsActive == true);

        string messageType =
            recipient != null
                ? "Internal"
                : "External";

        var mail =
            new MailMessage
            {
                SenderUserId =
                    sender.Id,

                SenderEmail =
                    sender.Email,

                RecipientUserId =
                    recipient?.Id,

                RecipientEmail =
                    recipientEmail,

                CcEmails =
                    NormalizeRecipientList(
                        ccEmails),

                BccEmails =
                    NormalizeRecipientList(
                        bccEmails),

                Subject =
                    subject,

                Body =
                    safeBody,

                IsRead = false,

                IsStarred = false,

                IsDraft = false,

                DraftSavedAt = null,

                IsDeletedBySender = false,

                IsDeletedByRecipient = false,

                IsPermanentlyDeletedBySender = false,

                IsPermanentlyDeletedByRecipient = false,

                SentAt =
                    DateTime.Now,

                ReadAt = null,

                MessageType =
                    messageType,

                ParentMessageId =
                    parentMessageId
            };

        _context.MailMessages.Add(mail);

        await _context.SaveChangesAsync();

        var createdFiles =
            new List<string>();

        try
        {
            await SaveAttachmentsAsync(
                mail,
                attachments,
                createdFiles);

            await _context.SaveChangesAsync();

            var emailAttachments =
                BuildEmailAttachments(
                    mail.Attachments);

            string htmlBody =
                BuildEmailHtml(
                    sender.Name,
                    safeBody);

            await _emailService.SendEmailAsync(
                mail.RecipientEmail,
                mail.CcEmails,
                mail.BccEmails,
                mail.Subject,
                htmlBody,
                emailAttachments);

            return true;
        }
        catch
        {
            _context.MailMessages.Remove(
                mail);

            await _context.SaveChangesAsync();

            DeletePhysicalFiles(
                createdFiles);

            return false;
        }
    }

    public async Task<int?> SaveDraftAsync(
        int senderUserId,
        string recipientEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string body,
        int? draftId = null,
        IReadOnlyList<IFormFile>? attachments = null)
    {
        var sender =
            await GetActiveUserAsync(
                senderUserId);

        if (sender == null)
        {
            return null;
        }

        MailMessage? draft = null;

        if (draftId.HasValue)
        {
            draft =
                await _context.MailMessages
                    .Include(x =>
                        x.Attachments)
                    .FirstOrDefaultAsync(x =>
                        x.Id ==
                        draftId.Value &&
                        x.SenderUserId ==
                        senderUserId &&
                        x.IsDraft &&
                        !x.IsPermanentlyDeletedBySender);

            if (draft == null)
            {
                return null;
            }
        }

        string safeBody =
            _htmlSanitizer.Sanitize(
                body ?? "");

        if (draft == null)
        {
            draft =
                new MailMessage
                {
                    SenderUserId =
                        senderUserId,

                    SenderEmail =
                        sender.Email,

                    RecipientEmail =
                        recipientEmail?.Trim() ?? "",

                    CcEmails =
                        NormalizeRecipientList(
                            ccEmails),

                    BccEmails =
                        NormalizeRecipientList(
                            bccEmails),

                    Subject =
                        subject?.Trim() ?? "",

                    Body =
                        safeBody,

                    IsRead = false,

                    IsStarred = false,

                    IsDraft = true,

                    DraftSavedAt =
                        DateTime.Now,

                    IsDeletedBySender = false,

                    IsDeletedByRecipient = false,

                    IsPermanentlyDeletedBySender = false,

                    IsPermanentlyDeletedByRecipient = false,

                    SentAt =
                        DateTime.Now,

                    ReadAt = null,

                    MessageType =
                        "Draft"
                };

            _context.MailMessages.Add(
                draft);
        }
        else
        {
            draft.RecipientEmail =
                recipientEmail?.Trim() ?? "";

            draft.CcEmails =
                NormalizeRecipientList(
                    ccEmails);

            draft.BccEmails =
                NormalizeRecipientList(
                    bccEmails);

            draft.Subject =
                subject?.Trim() ?? "";

            draft.Body =
                safeBody;

            draft.DraftSavedAt =
                DateTime.Now;
        }

        await _context.SaveChangesAsync();

        var createdFiles =
            new List<string>();

        try
        {
            await SaveAttachmentsAsync(
                draft,
                attachments,
                createdFiles);

            await _context.SaveChangesAsync();
        }
        catch
        {
            DeletePhysicalFiles(
                createdFiles);

            return null;
        }

        return draft.Id;
    }

    public async Task<bool> SendDraftAsync(
        int draftId,
        int userId)
    {
        var draft =
            await _context.MailMessages
                .Include(x =>
                    x.Attachments)
                .FirstOrDefaultAsync(x =>
                    x.Id == draftId &&
                    x.SenderUserId == userId &&
                    x.IsDraft &&
                    !x.IsPermanentlyDeletedBySender);

        if (draft == null)
        {
            return false;
        }

        var sender =
            await GetActiveUserAsync(
                userId);

        if (sender == null)
        {
            return false;
        }

        draft.RecipientEmail =
            draft.RecipientEmail?.Trim() ?? "";

        draft.Subject =
            draft.Subject?.Trim() ?? "";

        draft.Body =
            _htmlSanitizer.Sanitize(
                draft.Body ?? "");

        if (
            string.IsNullOrWhiteSpace(
                draft.RecipientEmail) ||
            string.IsNullOrWhiteSpace(
                draft.Subject) ||
            string.IsNullOrWhiteSpace(
                GetPlainText(draft.Body)))
        {
            return false;
        }

        var recipient =
            await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Email ==
                        draft.RecipientEmail &&
                    x.IsDeleted != true &&
                    x.IsActive == true);

        draft.SenderEmail =
            sender.Email;

        draft.RecipientUserId =
            recipient?.Id;

        draft.MessageType =
            recipient != null
                ? "Internal"
                : "External";

        draft.IsDraft =
            false;

        draft.DraftSavedAt =
            null;

        draft.SentAt =
            DateTime.Now;

        draft.IsDeletedBySender =
            false;

        draft.IsDeletedByRecipient =
            false;

        draft.IsPermanentlyDeletedBySender =
            false;

        draft.IsPermanentlyDeletedByRecipient =
            false;

        await _context.SaveChangesAsync();

        try
        {
            string htmlBody =
                BuildEmailHtml(
                    sender.Name,
                    draft.Body);

            var emailAttachments =
                BuildEmailAttachments(
                    draft.Attachments);

            await _emailService.SendEmailAsync(
                draft.RecipientEmail,
                draft.CcEmails,
                draft.BccEmails,
                draft.Subject,
                htmlBody,
                emailAttachments);

            return true;
        }
        catch
        {
            draft.IsDraft =
                true;

            draft.DraftSavedAt =
                DateTime.Now;

            await _context.SaveChangesAsync();

            return false;
        }
    }

    public async Task<List<MailMessage>> GetInboxAsync(
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x =>
                x.RecipientUserId == userId &&
                !x.IsDraft &&
                !x.IsDeletedByRecipient &&
                !x.IsPermanentlyDeletedByRecipient)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync();
    }

    public async Task<List<MailMessage>> GetSentAsync(
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x =>
                x.SenderUserId == userId &&
                !x.IsDraft &&
                !x.IsDeletedBySender &&
                !x.IsPermanentlyDeletedBySender)
            .OrderByDescending(x => x.SentAt)
            .ToListAsync();
    }

    public async Task<List<MailMessage>> GetStarredAsync(
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x =>
                x.IsStarred &&
                !x.IsDraft &&
                (
                    (
                        x.RecipientUserId == userId &&
                        !x.IsDeletedByRecipient &&
                        !x.IsPermanentlyDeletedByRecipient
                    )
                    ||
                    (
                        x.SenderUserId == userId &&
                        !x.IsDeletedBySender &&
                        !x.IsPermanentlyDeletedBySender
                    )
                ))
            .OrderByDescending(x => x.SentAt)
            .ToListAsync();
    }

    public async Task<List<MailMessage>> GetDraftsAsync(
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x =>
                x.SenderUserId == userId &&
                x.IsDraft &&
                !x.IsPermanentlyDeletedBySender)
            .OrderByDescending(x => x.DraftSavedAt)
            .ToListAsync();
    }

    public async Task<List<MailMessage>> GetTrashAsync(
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x =>
                !x.IsDraft &&
                (
                    (
                        x.RecipientUserId == userId &&
                        x.IsDeletedByRecipient &&
                        !x.IsPermanentlyDeletedByRecipient
                    )
                    ||
                    (
                        x.SenderUserId == userId &&
                        x.IsDeletedBySender &&
                        !x.IsPermanentlyDeletedBySender
                    )
                ))
            .OrderByDescending(x => x.SentAt)
            .ToListAsync();
    }

    public async Task<MailMessage?> GetDraftAsync(
        int draftId,
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x =>
                x.Id == draftId &&
                x.SenderUserId == userId &&
                x.IsDraft &&
                !x.IsPermanentlyDeletedBySender);
    }

    public async Task<MailMessage?> GetMessageAsync(
        int messageId,
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                !x.IsDraft &&
                (
                    (
                        x.RecipientUserId == userId &&
                        !x.IsDeletedByRecipient &&
                        !x.IsPermanentlyDeletedByRecipient
                    )
                    ||
                    (
                        x.SenderUserId == userId &&
                        !x.IsDeletedBySender &&
                        !x.IsPermanentlyDeletedBySender
                    )
                ));
    }

    public async Task<MailAttachment?> GetAttachmentAsync(
        int attachmentId,
        int userId)
    {
        return await _context.MailAttachments
            .Include(x => x.MailMessage)
            .FirstOrDefaultAsync(x =>
                x.Id == attachmentId &&
                (
                    x.MailMessage.SenderUserId == userId ||
                    x.MailMessage.RecipientUserId == userId
                ));
    }

    public async Task<bool> MarkAsReadAsync(
        int messageId,
        int userId)
    {
        var mail =
            await _context.MailMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == messageId &&
                    x.RecipientUserId == userId &&
                    !x.IsDraft);

        if (mail == null)
        {
            return false;
        }

        mail.IsRead =
            true;

        mail.ReadAt =
            DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleStarAsync(
        int messageId,
        int userId)
    {
        var mail =
            await _context.MailMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == messageId &&
                    !x.IsDraft &&
                    (
                        x.SenderUserId == userId ||
                        x.RecipientUserId == userId
                    ));

        if (mail == null)
        {
            return false;
        }

        mail.IsStarred =
            !mail.IsStarred;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteFromInboxAsync(
        int messageId,
        int userId)
    {
        var mail =
            await _context.MailMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == messageId &&
                    x.RecipientUserId == userId &&
                    !x.IsDraft);

        if (mail == null)
        {
            return false;
        }

        mail.IsDeletedByRecipient =
            true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteFromSentAsync(
        int messageId,
        int userId)
    {
        var mail =
            await _context.MailMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == messageId &&
                    x.SenderUserId == userId &&
                    !x.IsDraft);

        if (mail == null)
        {
            return false;
        }

        mail.IsDeletedBySender =
            true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteDraftAsync(
        int draftId,
        int userId)
    {
        var draft =
            await _context.MailMessages
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x =>
                    x.Id == draftId &&
                    x.SenderUserId == userId &&
                    x.IsDraft);

        if (draft == null)
        {
            return false;
        }

        var files =
            draft.Attachments
                .Select(x =>
                    GetPhysicalPath(
                        x.StoragePath))
                .ToList();

        draft.IsPermanentlyDeletedBySender =
            true;

        await _context.SaveChangesAsync();

        DeletePhysicalFiles(files);

        return true;
    }

    public async Task<bool> RestoreAsync(
        int messageId,
        int userId)
    {
        var mail =
            await _context.MailMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == messageId &&
                    !x.IsDraft &&
                    (
                        x.SenderUserId == userId ||
                        x.RecipientUserId == userId
                    ));

        if (mail == null)
        {
            return false;
        }

        if (mail.SenderUserId == userId)
        {
            mail.IsDeletedBySender =
                false;

            mail.IsPermanentlyDeletedBySender =
                false;
        }

        if (mail.RecipientUserId == userId)
        {
            mail.IsDeletedByRecipient =
                false;

            mail.IsPermanentlyDeletedByRecipient =
                false;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PermanentDeleteAsync(
        int messageId,
        int userId)
    {
        var mail =
            await _context.MailMessages
                .Include(x => x.Attachments)
                .FirstOrDefaultAsync(x =>
                    x.Id == messageId &&
                    !x.IsDraft &&
                    (
                        (
                            x.SenderUserId == userId &&
                            x.IsDeletedBySender
                        )
                        ||
                        (
                            x.RecipientUserId == userId &&
                            x.IsDeletedByRecipient
                        )
                    ));

        if (mail == null)
        {
            return false;
        }

        var files =
            mail.Attachments
                .Select(x =>
                    GetPhysicalPath(
                        x.StoragePath))
                .ToList();

        if (mail.SenderUserId == userId)
        {
            mail.IsPermanentlyDeletedBySender =
                true;
        }

        if (mail.RecipientUserId == userId)
        {
            mail.IsPermanentlyDeletedByRecipient =
                true;
        }

        await _context.SaveChangesAsync();

        DeletePhysicalFiles(files);

        return true;
    }

    private async Task<User?> GetActiveUserAsync(
        int userId)
    {
        return await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Id == userId &&
                x.IsDeleted != true &&
                x.IsActive == true);
    }

    private async Task SaveAttachmentsAsync(
        MailMessage mail,
        IReadOnlyList<IFormFile>? files,
        List<string> createdFiles)
    {
        if (
            files == null ||
            files.Count == 0)
        {
            return;
        }

        int maxFiles =
            _configuration.GetValue<int?>(
                "MailAttachmentSettings:MaxFilesPerMail")
            ?? 10;

        int maxSizeMb =
            _configuration.GetValue<int?>(
                "MailAttachmentSettings:MaxFileSizeMb")
            ?? 25;

        long maxSize =
            maxSizeMb *
            1024L *
            1024L;

        var allowedExtensions =
            _configuration
                .GetSection(
                    "MailAttachmentSettings:AllowedExtensions")
                .Get<string[]>()
            ?? Array.Empty<string>();

        var validFiles =
            files
                .Where(x =>
                    x != null &&
                    x.Length > 0)
                .ToList();

        if (validFiles.Count > maxFiles)
        {
            throw new InvalidOperationException(
                $"Maximum {maxFiles} attachments are allowed.");
        }

        string root =
            Path.Combine(
                _environment.ContentRootPath,
                "App_Data",
                "MailAttachments");

        Directory.CreateDirectory(root);

        foreach (var file in validFiles)
        {
            string originalName =
                Path.GetFileName(
                    file.FileName);

            string extension =
                Path.GetExtension(
                    originalName)
                .ToLowerInvariant();

            if (file.Length > maxSize)
            {
                throw new InvalidOperationException(
                    $"File {originalName} exceeds the {maxSizeMb} MB limit.");
            }

            if (
                !allowedExtensions.Any(x =>
                    string.Equals(
                        x,
                        extension,
                        StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"File type {extension} is not allowed.");
            }

            string storedName =
                $"{Guid.NewGuid():N}{extension}";

            string physicalPath =
                Path.Combine(
                    root,
                    storedName);

            await using (
                var stream =
                    new FileStream(
                        physicalPath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            createdFiles.Add(
                physicalPath);

            string relativePath =
                Path.Combine(
                    "App_Data",
                    "MailAttachments",
                    storedName)
                .Replace(
                    Path.DirectorySeparatorChar,
                    '/');

            var attachment =
                new MailAttachment
                {
                    MailMessageId =
                        mail.Id,

                    OriginalFileName =
                        originalName,

                    StoredFileName =
                        storedName,

                    ContentType =
                        string.IsNullOrWhiteSpace(
                            file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType,

                    FileSize =
                        file.Length,

                    StoragePath =
                        relativePath,

                    IsInline =
                        false,

                    ContentId =
                        null,

                    CreatedAt =
                        DateTime.Now
                };

            mail.Attachments.Add(
                attachment);
        }
    }

    private List<EmailAttachment>
        BuildEmailAttachments(
            IEnumerable<MailAttachment> attachments)
    {
        var result =
            new List<EmailAttachment>();

        foreach (var attachment in attachments)
        {
            if (string.IsNullOrWhiteSpace(attachment.StoragePath))
            {
                continue;
            }

            string physicalPath =
                GetPhysicalPath(attachment.StoragePath);

            if (!File.Exists(physicalPath))
            {
                throw new FileNotFoundException(
                    $"Mail attachment file was not found: {physicalPath}");
            }

            result.Add(
                new EmailAttachment
                {
                    FilePath = physicalPath,
                    FileName = attachment.OriginalFileName,
                    ContentType =
                        attachment.ContentType ??
                        "application/octet-stream"
                });
        }

        return result;
    }

    private string GetPhysicalPath(
        string relativePath)
    {
        string clean =
            relativePath.Replace(
                '/',
                Path.DirectorySeparatorChar);

        return Path.Combine(
            _environment.ContentRootPath,
            clean);
    }

    private void DeletePhysicalFiles(
        IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Physical file cleanup failure is intentionally ignored.
            }
        }
    }

    private static string NormalizeRecipientList(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        return string.Join(
            ", ",
            value.Split(
                    new[]
                    {
                        ',',
                        ';',
                        '\r',
                        '\n'
                    },
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase));
    }

    private static string GetPlainText(
        string html)
    {
        var sanitizer =
            new HtmlSanitizer();

        string safe =
            sanitizer.Sanitize(
                html ?? "");

        return System.Text.RegularExpressions.Regex
            .Replace(
                safe,
                "<[^>]+>",
                " ")
            .Trim();
    }

    private static string BuildEmailHtml(
        string senderName,
        string body)
    {
        string safeSenderName =
            System.Net.WebUtility.HtmlEncode(
                senderName ?? "");

        return
            "<!DOCTYPE html>" +
            "<html>" +
            "<body style=\"font-family:Arial,sans-serif;color:#202124;line-height:1.6;\">" +
            "<div>" + (body ?? "") + "</div>" +
            "<hr style=\"border:0;border-top:1px solid #ddd;margin:24px 0;\">" +
            "<div style=\"font-size:12px;color:#777;\">" +
            "Sent by " + safeSenderName +
            " through User Management System." +
            "</div>" +
            "</body>" +
            "</html>";
    }
}