using Microsoft.AspNetCore.Http;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services;

public interface IMailService
{
    Task<bool> SendMailAsync(
        int senderUserId,
        string recipientEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string body,
        int? parentMessageId = null,
        IReadOnlyList<IFormFile>? attachments = null);

    Task<int?> SaveDraftAsync(
        int senderUserId,
        string recipientEmail,
        string? ccEmails,
        string? bccEmails,
        string subject,
        string body,
        int? draftId = null,
        IReadOnlyList<IFormFile>? attachments = null);

    Task<bool> SendDraftAsync(
        int draftId,
        int userId);

    Task<List<MailMessage>> GetInboxAsync(int userId);
    Task<List<MailMessage>> GetSentAsync(int userId);
    Task<List<MailMessage>> GetStarredAsync(int userId);
    Task<List<MailMessage>> GetDraftsAsync(int userId);
    Task<List<MailMessage>> GetTrashAsync(int userId);

    Task<MailMessage?> GetDraftAsync(
        int draftId,
        int userId);

    Task<MailMessage?> GetMessageAsync(
        int messageId,
        int userId);

    Task<MailAttachment?> GetAttachmentAsync(
        int attachmentId,
        int userId);

    Task<bool> MarkAsReadAsync(
        int messageId,
        int userId);

    Task<bool> ToggleStarAsync(
        int messageId,
        int userId);

    Task<bool> DeleteFromInboxAsync(
        int messageId,
        int userId);

    Task<bool> DeleteFromSentAsync(
        int messageId,
        int userId);

    Task<bool> DeleteDraftAsync(
        int draftId,
        int userId);

    Task<bool> RestoreAsync(
        int messageId,
        int userId);

    Task<bool> PermanentDeleteAsync(
        int messageId,
        int userId);
}
