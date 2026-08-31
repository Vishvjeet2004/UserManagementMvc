using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services;

public class MailService : IMailService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public MailService(
        AppDbContext context,
        IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<bool> SendMailAsync(
        int senderUserId,
        string recipientEmail,
        string subject,
        string body,
        int? parentMessageId = null)
    {
        var sender = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == senderUserId &&
                x.IsDeleted == false &&
                x.IsActive == true);

        if (sender == null)
        {
            return false;
        }

        recipientEmail = recipientEmail.Trim();
        subject = subject.Trim();

        if (string.IsNullOrWhiteSpace(recipientEmail) ||
            string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        var recipient = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email == recipientEmail &&
                x.IsDeleted == false &&
                x.IsActive == true);

        string messageType =
            recipient != null
                ? "Internal"
                : "External";

        var mail = new MailMessage
        {
            SenderUserId = sender.Id,
            SenderEmail = sender.Email,
            RecipientUserId = recipient?.Id,
            RecipientEmail = recipientEmail,
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
            SentAt = DateTime.Now,
            MessageType = messageType,
            ParentMessageId = parentMessageId
        };

        _context.MailMessages.Add(mail);

        await _context.SaveChangesAsync();

        try
        {
            string htmlBody = BuildHtmlBody(
                sender.Name,
                body);

            await _emailService.SendEmailAsync(
                recipientEmail,
                subject,
                htmlBody);

            return true;
        }
        catch
        {
            _context.MailMessages.Remove(mail);

            await _context.SaveChangesAsync();

            return false;
        }
    }

    public async Task<int?> SaveDraftAsync(
        int senderUserId,
        string recipientEmail,
        string subject,
        string body,
        int? draftId = null)
    {
        var sender = await _context.Users
            .FirstOrDefaultAsync(x =>
                x.Id == senderUserId &&
                x.IsDeleted == false &&
                x.IsActive == true);

        if (sender == null)
        {
            return null;
        }

        MailMessage? draft = null;

        if (draftId.HasValue)
        {
            draft = await _context.MailMessages
                .FirstOrDefaultAsync(x =>
                    x.Id == draftId.Value &&
                    x.SenderUserId == senderUserId &&
                    x.IsDraft &&
                    !x.IsPermanentlyDeletedBySender);

            if (draft == null)
            {
                return null;
            }
        }

        if (draft == null)
        {
            draft = new MailMessage
            {
                SenderUserId = senderUserId,
                SenderEmail = sender.Email,
                RecipientEmail = recipientEmail?.Trim() ?? "",
                Subject = subject?.Trim() ?? "",
                Body = body ?? "",
                IsRead = false,
                IsStarred = false,
                IsDraft = true,
                DraftSavedAt = DateTime.Now,
                IsDeletedBySender = false,
                IsDeletedByRecipient = false,
                IsPermanentlyDeletedBySender = false,
                IsPermanentlyDeletedByRecipient = false,
                SentAt = DateTime.Now,
                MessageType = "Draft"
            };

            _context.MailMessages.Add(draft);
        }
        else
        {
            draft.RecipientEmail = recipientEmail?.Trim() ?? "";
            draft.Subject = subject?.Trim() ?? "";
            draft.Body = body ?? "";
            draft.DraftSavedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return draft.Id;
    }

    public async Task<bool> SendDraftAsync(
        int draftId,
        int userId)
    {
        var draft = await _context.MailMessages
            .FirstOrDefaultAsync(x =>
                x.Id == draftId &&
                x.SenderUserId == userId &&
                x.IsDraft &&
                !x.IsPermanentlyDeletedBySender);

        if (draft == null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(draft.RecipientEmail) ||
            string.IsNullOrWhiteSpace(draft.Subject) ||
            string.IsNullOrWhiteSpace(draft.Body))
        {
            return false;
        }

        var sender = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == userId &&
                x.IsDeleted == false &&
                x.IsActive == true);

        if (sender == null)
        {
            return false;
        }

        var recipient = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email == draft.RecipientEmail &&
                x.IsDeleted == false &&
                x.IsActive == true);

        draft.SenderEmail = sender.Email;
        draft.RecipientUserId = recipient?.Id;
        draft.MessageType =
            recipient != null
                ? "Internal"
                : "External";

        draft.IsDraft = false;
        draft.DraftSavedAt = null;
        draft.SentAt = DateTime.Now;
        draft.IsDeletedBySender = false;
        draft.IsDeletedByRecipient = false;
        draft.IsPermanentlyDeletedBySender = false;
        draft.IsPermanentlyDeletedByRecipient = false;

        await _context.SaveChangesAsync();

        try
        {
            string htmlBody = BuildHtmlBody(
                sender.Name,
                draft.Body);

            await _emailService.SendEmailAsync(
                draft.RecipientEmail,
                draft.Subject,
                htmlBody);

            return true;
        }
        catch
        {
            draft.IsDraft = true;
            draft.DraftSavedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return false;
        }
    }

    public async Task<List<MailMessage>> GetInboxAsync(
        int userId)
    {
        return await _context.MailMessages
            .AsNoTracking()
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

    public async Task<MailMessage?> GetMessageAsync(
        int messageId,
        int userId)
    {
        return await _context.MailMessages
            .FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                !x.IsDraft &&
                (
                    (
                        x.SenderUserId == userId &&
                        !x.IsDeletedBySender &&
                        !x.IsPermanentlyDeletedBySender
                    )
                    ||
                    (
                        x.RecipientUserId == userId &&
                        !x.IsDeletedByRecipient &&
                        !x.IsPermanentlyDeletedByRecipient
                    )
                ));
    }

    public async Task<MailMessage?> GetDraftAsync(
        int draftId,
        int userId)
    {
        return await _context.MailMessages
            .FirstOrDefaultAsync(x =>
                x.Id == draftId &&
                x.SenderUserId == userId &&
                x.IsDraft &&
                !x.IsPermanentlyDeletedBySender);
    }

    public async Task<bool> MarkAsReadAsync(
        int messageId,
        int userId)
    {
        var mail = await _context.MailMessages
            .FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                x.RecipientUserId == userId &&
                !x.IsDraft);

        if (mail == null)
        {
            return false;
        }

        mail.IsRead = true;
        mail.ReadAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleStarAsync(
        int messageId,
        int userId)
    {
        var mail = await _context.MailMessages
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

        mail.IsStarred = !mail.IsStarred;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteFromInboxAsync(
        int messageId,
        int userId)
    {
        var mail = await _context.MailMessages
            .FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                x.RecipientUserId == userId &&
                !x.IsDraft);

        if (mail == null)
        {
            return false;
        }

        mail.IsDeletedByRecipient = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteFromSentAsync(
        int messageId,
        int userId)
    {
        var mail = await _context.MailMessages
            .FirstOrDefaultAsync(x =>
                x.Id == messageId &&
                x.SenderUserId == userId &&
                !x.IsDraft);

        if (mail == null)
        {
            return false;
        }

        mail.IsDeletedBySender = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteDraftAsync(
        int draftId,
        int userId)
    {
        var draft = await _context.MailMessages
            .FirstOrDefaultAsync(x =>
                x.Id == draftId &&
                x.SenderUserId == userId &&
                x.IsDraft);

        if (draft == null)
        {
            return false;
        }

        draft.IsPermanentlyDeletedBySender = true;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RestoreAsync(
        int messageId,
        int userId)
    {
        var mail = await _context.MailMessages
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
            mail.IsDeletedBySender = false;
            mail.IsPermanentlyDeletedBySender = false;
        }

        if (mail.RecipientUserId == userId)
        {
            mail.IsDeletedByRecipient = false;
            mail.IsPermanentlyDeletedByRecipient = false;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> PermanentDeleteAsync(
        int messageId,
        int userId)
    {
        var mail = await _context.MailMessages
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

        if (mail.SenderUserId == userId)
        {
            mail.IsPermanentlyDeletedBySender = true;
        }

        if (mail.RecipientUserId == userId)
        {
            mail.IsPermanentlyDeletedByRecipient = true;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    private static string BuildHtmlBody(
        string senderName,
        string body)
    {
        string safeBody = body
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("\r\n", "<br>")
            .Replace("\n", "<br>");

        return $"""
            <html>
            <body>
                <p>{safeBody}</p>
                <hr>
                <p>
                    Sent by {senderName}
                    through User Management System.
                </p>
            </body>
            </html>
            """;
    }
}