using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace UserManagementMvc.Services;

public sealed class MailServerService : IMailServerService
{
    private const int MaxInboxMessages = 50;

    private readonly IConfiguration _configuration;

    public MailServerService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<List<ServerMailMessage>> GetInboxAsync(
        int userId)
    {
        var result = new List<ServerMailMessage>();

        if (userId <= 0)
        {
            return result;
        }

        var password =
            GetImapPassword();

        var host =
            _configuration[
                "MailServerSettings:ImapHost"];

        var port =
            _configuration.GetValue<int>(
                "MailServerSettings:ImapPort");

        var useSsl =
            _configuration.GetValue<bool>(
                "MailServerSettings:ImapUseSsl");

        var username =
            _configuration[
                "MailServerSettings:MailboxEmail"];

        var folderName =
            _configuration[
                "MailServerSettings:InboxFolder"]
            ?? "INBOX";

        ValidateSettings(
            host,
            port,
            username,
            password);

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(
                host,
                port,
                useSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                username,
                password);

            var inbox =
                await OpenFolderAsync(
                    client,
                    folderName);

            if (!inbox.IsOpen)
            {
                await inbox.OpenAsync(
                    FolderAccess.ReadOnly);
            }

            var allUids =
                await inbox.SearchAsync(
                    SearchQuery.All);

            if (allUids.Count == 0)
            {
                return result;
            }

            // Keep only the newest server-side messages before fetching message summaries.
            var latestUids =
                allUids
                    .OrderByDescending(
                        x => x.Id)
                    .Take(MaxInboxMessages)
                    .ToList();

            var summaries =
                await inbox.FetchAsync(
                    latestUids,
                    MessageSummaryItems.UniqueId |
                    MessageSummaryItems.Envelope |
                    MessageSummaryItems.Flags |
                    MessageSummaryItems.PreviewText);

            foreach (var summary in summaries
                         .OrderByDescending(
                             GetMessageDate))
            {
                var envelope =
                    summary.Envelope;

                var sender =
                    envelope?.From?
                        .Mailboxes
                        .FirstOrDefault()?
                        .Address
                    ?? "";

                var recipient =
                    envelope?.To?
                        .Mailboxes
                        .FirstOrDefault()?
                        .Address
                    ?? username;

                var cc =
                    envelope?.Cc?
                        .Mailboxes
                        .Select(x => x.Address)
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x))
                        .ToList();

                var bcc =
                    envelope?.Bcc?
                        .Mailboxes
                        .Select(x => x.Address)
                        .Where(x =>
                            !string.IsNullOrWhiteSpace(x))
                        .ToList();

                var sentAt =
                    GetMessageDate(summary);

                result.Add(
                    new ServerMailMessage
                    {
                        Id =
                            BuildMessageId(
                                summary.UniqueId),

                        Uid =
                            summary.UniqueId.Id,

                        FolderName =
                            inbox.FullName,

                        SenderEmail =
                            sender,

                        RecipientEmail =
                            recipient,

                        CcEmails =
                            cc != null &&
                            cc.Count > 0
                                ? string.Join(
                                    ", ",
                                    cc)
                                : null,

                        BccEmails =
                            bcc != null &&
                            bcc.Count > 0
                                ? string.Join(
                                    ", ",
                                    bcc)
                                : null,

                        Subject =
                            envelope?.Subject
                            ?? "(No Subject)",

                        // Preview text is loaded instead of the complete MIME body.
                        Body =
                            summary.PreviewText
                            ?? "",

                        IsRead =
                            summary.Flags.HasValue &&
                            summary.Flags.Value.HasFlag(
                                MessageFlags.Seen),

                        IsStarred =
                            summary.Flags.HasValue &&
                            summary.Flags.Value.HasFlag(
                                MessageFlags.Flagged),

                        IsDraft =
                            false,

                        SentAt =
                            sentAt,

                        MessageType =
                            "External",

                        Attachments =
                            new List<ServerMailAttachment>()
                    });
            }

            return result
                .OrderByDescending(
                    x => x.SentAt)
                .Take(MaxInboxMessages)
                .ToList();
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    true);
            }
        }
    }

    public async Task<ServerMailMessage?> GetMessageAsync(
        string messageId,
        int userId)
    {
        if (userId <= 0 ||
            string.IsNullOrWhiteSpace(messageId))
        {
            return null;
        }

        if (!TryParseMessageId(
                messageId,
                out var uid))
        {
            return null;
        }

        var password =
            GetImapPassword();

        var host =
            _configuration[
                "MailServerSettings:ImapHost"];

        var port =
            _configuration.GetValue<int>(
                "MailServerSettings:ImapPort");

        var useSsl =
            _configuration.GetValue<bool>(
                "MailServerSettings:ImapUseSsl");

        var username =
            _configuration[
                "MailServerSettings:MailboxEmail"];

        var folderName =
            _configuration[
                "MailServerSettings:InboxFolder"]
            ?? "INBOX";

        ValidateSettings(
            host,
            port,
            username,
            password);

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(
                host,
                port,
                useSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                username,
                password);

            var inbox =
                await OpenFolderAsync(
                    client,
                    folderName);

            if (!inbox.IsOpen)
            {
                await inbox.OpenAsync(
                    FolderAccess.ReadOnly);
            }

            var summaries =
                await inbox.FetchAsync(
                    new[]
                    {
                        uid
                    },
                    MessageSummaryItems.UniqueId |
                    MessageSummaryItems.Flags);

            var summary =
                summaries.FirstOrDefault();

            if (summary == null)
            {
                return null;
            }

            // Download the complete MIME message only when the user opens it.
            var message =
                await inbox.GetMessageAsync(
                    uid);

            return BuildServerMessage(
                message,
                uid,
                inbox.FullName,
                summary.Flags);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    true);
            }
        }
    }

    public async Task<ServerMailAttachmentContent?> GetAttachmentAsync(
        string attachmentId,
        int userId)
    {
        if (userId <= 0 ||
            string.IsNullOrWhiteSpace(attachmentId))
        {
            return null;
        }

        if (!TryParseAttachmentId(
                attachmentId,
                out var uid,
                out var partIndex))
        {
            return null;
        }

        var password =
            GetImapPassword();

        var host =
            _configuration[
                "MailServerSettings:ImapHost"];

        var port =
            _configuration.GetValue<int>(
                "MailServerSettings:ImapPort");

        var useSsl =
            _configuration.GetValue<bool>(
                "MailServerSettings:ImapUseSsl");

        var username =
            _configuration[
                "MailServerSettings:MailboxEmail"];

        var folderName =
            _configuration[
                "MailServerSettings:InboxFolder"]
            ?? "INBOX";

        ValidateSettings(
            host,
            port,
            username,
            password);

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(
                host,
                port,
                useSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                username,
                password);

            var inbox =
                await OpenFolderAsync(
                    client,
                    folderName);

            if (!inbox.IsOpen)
            {
                await inbox.OpenAsync(
                    FolderAccess.ReadOnly);
            }

            var message =
                await inbox.GetMessageAsync(
                    uid);

            var parts =
                message.BodyParts
                    .OfType<MimePart>()
                    .ToList();

            if (partIndex < 0 ||
                partIndex >= parts.Count)
            {
                return null;
            }

            var part =
                parts[partIndex];

            using var memory =
                new MemoryStream();

            await part.Content.DecodeToAsync(
                memory);

            return new ServerMailAttachmentContent
            {
                FileName =
                    string.IsNullOrWhiteSpace(
                        part.FileName)
                        ? "attachment"
                        : part.FileName,

                ContentType =
                    part.ContentType?.MimeType
                    ?? "application/octet-stream",

                Content =
                    memory.ToArray()
            };
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    true);
            }
        }
    }

    public async Task<bool> MarkAsReadAsync(
        string messageId,
        int userId)
    {
        if (userId <= 0 ||
            !TryParseMessageId(
                messageId,
                out var uid))
        {
            return false;
        }

        var password =
            GetImapPassword();

        var host =
            _configuration[
                "MailServerSettings:ImapHost"];

        var port =
            _configuration.GetValue<int>(
                "MailServerSettings:ImapPort");

        var useSsl =
            _configuration.GetValue<bool>(
                "MailServerSettings:ImapUseSsl");

        var username =
            _configuration[
                "MailServerSettings:MailboxEmail"];

        var folderName =
            _configuration[
                "MailServerSettings:InboxFolder"]
            ?? "INBOX";

        ValidateSettings(
            host,
            port,
            username,
            password);

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(
                host,
                port,
                useSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                username,
                password);

            var inbox =
                await OpenFolderAsync(
                    client,
                    folderName);

            if (!inbox.IsOpen)
            {
                await inbox.OpenAsync(
                    FolderAccess.ReadWrite);
            }

            await inbox.AddFlagsAsync(
                uid,
                MessageFlags.Seen,
                true);

            return true;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    true);
            }
        }
    }

    public async Task<bool> ToggleStarAsync(
        string messageId,
        int userId)
    {
        if (userId <= 0 ||
            !TryParseMessageId(
                messageId,
                out var uid))
        {
            return false;
        }

        var password =
            GetImapPassword();

        var host =
            _configuration[
                "MailServerSettings:ImapHost"];

        var port =
            _configuration.GetValue<int>(
                "MailServerSettings:ImapPort");

        var useSsl =
            _configuration.GetValue<bool>(
                "MailServerSettings:ImapUseSsl");

        var username =
            _configuration[
                "MailServerSettings:MailboxEmail"];

        var folderName =
            _configuration[
                "MailServerSettings:InboxFolder"]
            ?? "INBOX";

        ValidateSettings(
            host,
            port,
            username,
            password);

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(
                host,
                port,
                useSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                username,
                password);

            var inbox =
                await OpenFolderAsync(
                    client,
                    folderName);

            if (!inbox.IsOpen)
            {
                await inbox.OpenAsync(
                    FolderAccess.ReadWrite);
            }

            var summaries =
                await inbox.FetchAsync(
                    new[]
                    {
                        uid
                    },
                    MessageSummaryItems.UniqueId |
                    MessageSummaryItems.Flags);

            if (summaries.Count == 0)
            {
                return false;
            }

            var flags =
                summaries[0].Flags;

            var isStarred =
                flags.HasValue &&
                flags.Value.HasFlag(
                    MessageFlags.Flagged);

            if (isStarred)
            {
                await inbox.RemoveFlagsAsync(
                    uid,
                    MessageFlags.Flagged,
                    true);
            }
            else
            {
                await inbox.AddFlagsAsync(
                    uid,
                    MessageFlags.Flagged,
                    true);
            }

            return true;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    true);
            }
        }
    }

    public async Task<bool> DeleteFromInboxAsync(
        string messageId,
        int userId)
    {
        if (userId <= 0 ||
            !TryParseMessageId(
                messageId,
                out var uid))
        {
            return false;
        }

        var password =
            GetImapPassword();

        var host =
            _configuration[
                "MailServerSettings:ImapHost"];

        var port =
            _configuration.GetValue<int>(
                "MailServerSettings:ImapPort");

        var useSsl =
            _configuration.GetValue<bool>(
                "MailServerSettings:ImapUseSsl");

        var username =
            _configuration[
                "MailServerSettings:MailboxEmail"];

        var folderName =
            _configuration[
                "MailServerSettings:InboxFolder"]
            ?? "INBOX";

        var trashFolder =
            _configuration[
                "MailServerSettings:TrashFolder"]
            ?? "[Gmail]/Trash";

        ValidateSettings(
            host,
            port,
            username,
            password);

        using var client = new ImapClient();

        try
        {
            await client.ConnectAsync(
                host,
                port,
                useSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                username,
                password);

            var inbox =
                await OpenFolderAsync(
                    client,
                    folderName);

            if (!inbox.IsOpen)
            {
                await inbox.OpenAsync(
                    FolderAccess.ReadWrite);
            }

            var trash =
                await OpenFolderAsync(
                    client,
                    trashFolder);

            if (!trash.IsOpen)
            {
                await trash.OpenAsync(
                    FolderAccess.ReadWrite);
            }

            await inbox.MoveToAsync(
                new[]
                {
                    uid
                },
                trash);

            return true;
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(
                    true);
            }
        }
    }

    private string? GetImapPassword()
    {
        var mailboxPassword =
            _configuration[
                "MailServerSettings:MailboxPassword"];

        if (!string.IsNullOrWhiteSpace(
                mailboxPassword))
        {
            return mailboxPassword;
        }

        var incomingPassword =
            _configuration[
                "IncomingMailSettings:Password"];

        if (!string.IsNullOrWhiteSpace(
                incomingPassword))
        {
            return incomingPassword;
        }

        return null;
    }

    private async Task<IMailFolder> OpenFolderAsync(
        ImapClient client,
        string folderName)
    {
        if (string.IsNullOrWhiteSpace(
                folderName) ||
            folderName.Equals(
                "INBOX",
                StringComparison.OrdinalIgnoreCase))
        {
            return client.Inbox;
        }

        return client.GetFolder(
            folderName);
    }

    private static DateTime GetMessageDate(
        IMessageSummary summary)
    {
        if (summary.Envelope?.Date is DateTimeOffset envelopeDate)
        {
            return envelopeDate.LocalDateTime;
        }

        if (summary.InternalDate is DateTimeOffset internalDate)
        {
            return internalDate.LocalDateTime;
        }

        return DateTime.MinValue;
    }

    private static ServerMailMessage BuildServerMessage(
        MimeMessage message,
        UniqueId uid,
        string folderName,
        MessageFlags? flags)
    {
        var attachments =
            new List<ServerMailAttachment>();

        var mimeParts =
            message.BodyParts
                .OfType<MimePart>()
                .ToList();

        for (var i = 0;
             i < mimeParts.Count;
             i++)
        {
            var part =
                mimeParts[i];

            if (!part.IsAttachment &&
                string.IsNullOrWhiteSpace(
                    part.FileName))
            {
                continue;
            }

            long fileSize = 0;

            if (part.Content != null)
            {
                try
                {
                    fileSize =
                        part.Content.Stream.Length;
                }
                catch
                {
                    fileSize = 0;
                }
            }

            attachments.Add(
                new ServerMailAttachment
                {
                    Id =
                        BuildAttachmentId(
                            uid,
                            i),

                    OriginalFileName =
                        string.IsNullOrWhiteSpace(
                            part.FileName)
                            ? "attachment"
                            : part.FileName,

                    ContentType =
                        part.ContentType?.MimeType
                        ?? "application/octet-stream",

                    FileSize =
                        fileSize,

                    IsInline =
                        !part.IsAttachment,

                    ContentId =
                        part.ContentId
                });
        }

        DateTime sentAt =
            message.Date == DateTimeOffset.MinValue
                ? DateTime.Now
                : message.Date.LocalDateTime;

        return new ServerMailMessage
        {
            Id =
                BuildMessageId(uid),

            Uid =
                uid.Id,

            FolderName =
                folderName,

            SenderEmail =
                message.From?
                    .Mailboxes
                    .FirstOrDefault()?
                    .Address
                ?? "",

            RecipientEmail =
                message.To?
                    .Mailboxes
                    .FirstOrDefault()?
                    .Address
                ?? "",

            CcEmails =
                message.Cc?
                    .Mailboxes
                    .Select(x => x.Address)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Any() == true
                    ? string.Join(
                        ", ",
                        message.Cc.Mailboxes
                            .Select(x => x.Address)
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x)))
                    : null,

            BccEmails =
                message.Bcc?
                    .Mailboxes
                    .Select(x => x.Address)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x))
                    .Any() == true
                    ? string.Join(
                        ", ",
                        message.Bcc.Mailboxes
                            .Select(x => x.Address)
                            .Where(x =>
                                !string.IsNullOrWhiteSpace(x)))
                    : null,

            Subject =
                message.Subject
                ?? "(No Subject)",

            Body =
                !string.IsNullOrWhiteSpace(
                    message.HtmlBody)
                    ? message.HtmlBody
                    : message.TextBody
                      ?? "",

            IsRead =
                flags.HasValue &&
                flags.Value.HasFlag(
                    MessageFlags.Seen),

            IsStarred =
                flags.HasValue &&
                flags.Value.HasFlag(
                    MessageFlags.Flagged),

            IsDraft =
                false,

            SentAt =
                sentAt,

            MessageType =
                "External",

            Attachments =
                attachments
        };
    }

    private static string BuildMessageId(
        UniqueId uid)
    {
        return $"imap:{uid.Id}";
    }

    private static string BuildAttachmentId(
        UniqueId uid,
        int partIndex)
    {
        return $"imap:{uid.Id}:{partIndex}";
    }

    private static bool TryParseMessageId(
        string messageId,
        out UniqueId uid)
    {
        uid = UniqueId.Invalid;

        var parts =
            messageId.Split(
                ':',
                StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2 ||
            !parts[0].Equals(
                "imap",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!uint.TryParse(
                parts[1],
                out var value) ||
            value == 0)
        {
            return false;
        }

        uid =
            new UniqueId(value);

        return true;
    }

    private static bool TryParseAttachmentId(
        string attachmentId,
        out UniqueId uid,
        out int partIndex)
    {
        uid = UniqueId.Invalid;
        partIndex = -1;

        var parts =
            attachmentId.Split(
                ':',
                StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 3 ||
            !parts[0].Equals(
                "imap",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!uint.TryParse(
                parts[1],
                out var uidValue) ||
            uidValue == 0)
        {
            return false;
        }

        if (!int.TryParse(
                parts[2],
                out partIndex) ||
            partIndex < 0)
        {
            return false;
        }

        uid =
            new UniqueId(uidValue);

        return true;
    }

    private static void ValidateSettings(
        string? host,
        int port,
        string? username,
        string? password)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException(
                "IMAP Host is not configured.");
        }

        if (port <= 0)
        {
            throw new InvalidOperationException(
                "IMAP Port is not configured correctly.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException(
                "Mailbox email is not configured.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "IMAP password is not configured.");
        }
    }
}