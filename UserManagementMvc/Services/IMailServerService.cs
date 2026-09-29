namespace UserManagementMvc.Services;

public interface IMailServerService
{
    Task<List<ServerMailMessage>> GetInboxAsync(int userId);

    Task<ServerMailMessage?> GetMessageAsync(
        string messageId,
        int userId);

    Task<ServerMailAttachmentContent?> GetAttachmentAsync(
        string attachmentId,
        int userId);

    Task<bool> MarkAsReadAsync(
        string messageId,
        int userId);

    Task<bool> ToggleStarAsync(
        string messageId,
        int userId);

    Task<bool> DeleteFromInboxAsync(
        string messageId,
        int userId);
}
