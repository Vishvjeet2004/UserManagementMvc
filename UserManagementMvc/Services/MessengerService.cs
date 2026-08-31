using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Services
{
    public class MessengerService
    {
        private readonly AppDbContext _context;

        public MessengerService(AppDbContext context)
        {
            _context = context;
        }

        // SEARCH USERS + RECENT CONVERSATIONS
        public async Task<List<UserSearchViewModel>> SearchUsersAsync(
            int currentUserId,
            string? search)
        {
            var query = _context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Id != currentUserId &&
                    x.IsDeleted != true &&
                    x.IsActive != false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(x =>
                    EF.Functions.Like(x.Name, search + "%") ||
                    (x.UserName != null &&
                     EF.Functions.Like(x.UserName, search + "%")) ||
                    EF.Functions.Like(x.Email, "%" + search + "%"));
            }

            var users = await query
                .Select(x => new UserSearchViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    UserName = x.UserName ?? "",
                    Department = x.Department,

                    LastSeen = x.LastSeenAt.HasValue
                        ? x.LastSeenAt.Value.ToString(
                            "dd MMM yyyy, hh\\:mm tt")
                        : null
                })
                .ToListAsync();

            var latestMessages = await _context.ChatMessages
                .AsNoTracking()
                .Where(x =>
                    (
                        x.SenderUserId == currentUserId &&
                        !x.IsDeletedBySender
                    )
                    ||
                    (
                        x.RecipientUserId == currentUserId &&
                        !x.IsDeletedByRecipient
                    ))
                .OrderByDescending(x => x.SentAt)
                .Take(1000)
                .ToListAsync();

            var latestByUser =
                latestMessages
                    .GroupBy(x =>
                        x.SenderUserId == currentUserId
                            ? x.RecipientUserId
                            : x.SenderUserId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First());

            var unreadCounts =
                await _context.ChatMessages
                    .AsNoTracking()
                    .Where(x =>
                        x.RecipientUserId == currentUserId &&
                        x.SeenAt == null &&
                        !x.IsDeletedByRecipient)
                    .GroupBy(x => x.SenderUserId)
                    .Select(g => new
                    {
                        UserId = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(
                        x => x.UserId,
                        x => x.Count);

            foreach (var user in users)
            {
                if (latestByUser.TryGetValue(
                    user.Id,
                    out var latest))
                {
                    user.LastMessage =
                        string.IsNullOrWhiteSpace(
                            latest.MessageText)
                            ? "Attachment"
                            : latest.MessageText;

                    user.LastMessageAt =
                        latest.SentAt.ToString(
                            "dd MMM, hh\\:mm tt");
                }

                if (unreadCounts.TryGetValue(
                    user.Id,
                    out var unread))
                {
                    user.UnreadCount = unread;
                }
            }

            return users
                .OrderByDescending(x =>
                    x.LastMessageAt != null)
                .ThenByDescending(x =>
                    x.LastMessageAt)
                .ThenBy(x => x.Name)
                .Take(100)
                .ToList();
        }

        // GET SINGLE USER
        public async Task<UserSearchViewModel?> GetUserByIdAsync(
            int currentUserId,
            int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Id == userId &&
                    x.Id != currentUserId &&
                    x.IsDeleted != true &&
                    x.IsActive != false)
                .Select(x => new UserSearchViewModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    UserName = x.UserName ?? "",
                    Department = x.Department,

                    LastSeen = x.LastSeenAt.HasValue
                        ? x.LastSeenAt.Value.ToString(
                            "dd MMM yyyy, hh\\:mm tt")
                        : null
                })
                .FirstOrDefaultAsync();
        }

        // GET CONVERSATION
        public async Task<List<ChatMessageViewModel>>
            GetConversationAsync(
                int currentUserId,
                int otherUserId)
        {
            var messages = await _context.ChatMessages
                .AsNoTracking()
                .Where(x =>
                    (
                        x.SenderUserId == currentUserId &&
                        x.RecipientUserId == otherUserId &&
                        !x.IsDeletedBySender
                    )
                    ||
                    (
                        x.SenderUserId == otherUserId &&
                        x.RecipientUserId == currentUserId &&
                        !x.IsDeletedByRecipient
                    ))
                .OrderBy(x => x.SentAt)
                .ToListAsync();

            return messages.Select(x =>
            {
                bool mine =
                    x.SenderUserId == currentUserId;

                string status;

                if (!mine)
                {
                    status = "received";
                }
                else if (x.SeenAt.HasValue)
                {
                    status = "seen";
                }
                else if (x.DeliveredAt.HasValue)
                {
                    status = "delivered";
                }
                else
                {
                    status = "sent";
                }

                return new ChatMessageViewModel
                {
                    Id = x.Id,

                    SenderUserId =
                        x.SenderUserId,

                    RecipientUserId =
                        x.RecipientUserId,

                    MessageText =
                        x.MessageText,

                    SentAt =
                        x.SentAt.ToString("hh\\:mm tt"),

                    DeliveredAt =
                        x.DeliveredAt?
                            .ToString("hh\\:mm tt"),

                    SeenAt =
                        x.SeenAt?
                            .ToString("hh\\:mm tt"),

                    IsMine = mine,

                    Status = status,

                    AttachmentUrl =
                        x.AttachmentUrl,

                    AttachmentName =
                        x.AttachmentName,

                    AttachmentContentType =
                        x.AttachmentContentType,

                    AttachmentSize =
                        x.AttachmentSize
                };
            }).ToList();
        }

        // GET ACTIVE EMOJIS
        public async Task<List<ChatEmoji>> GetActiveEmojisAsync()
        {
            return await _context.ChatEmojis
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        // SEND MESSAGE
        public async Task<ChatMessage?> SendMessageAsync(
            int senderUserId,
            int recipientUserId,
            string? messageText,
            string? attachmentUrl = null,
            string? attachmentName = null,
            string? attachmentContentType = null,
            long? attachmentSize = null)
        {
            if (senderUserId == recipientUserId)
                return null;

            bool hasText =
                !string.IsNullOrWhiteSpace(messageText);

            bool hasAttachment =
                !string.IsNullOrWhiteSpace(attachmentUrl);

            if (!hasText && !hasAttachment)
                return null;

            var recipientExists =
                await _context.Users.AnyAsync(x =>
                    x.Id == recipientUserId &&
                    x.IsDeleted != true &&
                    x.IsActive != false);

            if (!recipientExists)
                return null;

            var message = new ChatMessage
            {
                SenderUserId =
                    senderUserId,

                RecipientUserId =
                    recipientUserId,

                MessageText =
                    messageText?.Trim() ?? "",

                SentAt =
                    DateTime.Now,

                AttachmentUrl =
                    attachmentUrl,

                AttachmentName =
                    attachmentName,

                AttachmentContentType =
                    attachmentContentType,

                AttachmentSize =
                    attachmentSize
            };

            _context.ChatMessages.Add(message);

            await _context.SaveChangesAsync();

            return message;
        }

        // MARK DELIVERED
        public async Task<bool> MarkDeliveredAsync(
            int currentUserId,
            int otherUserId)
        {
            var messages =
                await _context.ChatMessages
                    .Where(x =>
                        x.SenderUserId == otherUserId &&
                        x.RecipientUserId == currentUserId &&
                        x.DeliveredAt == null &&
                        !x.IsDeletedByRecipient)
                    .ToListAsync();

            if (messages.Count == 0)
                return false;

            DateTime now = DateTime.Now;

            foreach (var message in messages)
            {
                message.DeliveredAt = now;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        // MARK SEEN
        public async Task<bool> MarkSeenAsync(
            int currentUserId,
            int otherUserId)
        {
            var messages =
                await _context.ChatMessages
                    .Where(x =>
                        x.SenderUserId == otherUserId &&
                        x.RecipientUserId == currentUserId &&
                        x.SeenAt == null &&
                        !x.IsDeletedByRecipient)
                    .ToListAsync();

            if (messages.Count == 0)
                return false;

            DateTime now = DateTime.Now;

            foreach (var message in messages)
            {
                if (!message.DeliveredAt.HasValue)
                {
                    message.DeliveredAt = now;
                }

                message.SeenAt = now;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        // DELETE FOR ME
        public async Task<bool> DeleteForMeAsync(
            int currentUserId,
            int messageId)
        {
            var message =
                await _context.ChatMessages
                    .FirstOrDefaultAsync(x =>
                        x.Id == messageId &&
                        (
                            x.SenderUserId == currentUserId ||
                            x.RecipientUserId == currentUserId
                        ));

            if (message == null)
                return false;

            if (message.SenderUserId == currentUserId)
            {
                message.IsDeletedBySender = true;
            }

            if (message.RecipientUserId == currentUserId)
            {
                message.IsDeletedByRecipient = true;
            }

            await _context.SaveChangesAsync();

            return true;
        }

        // UPDATE LAST SEEN
        public async Task UpdateLastSeenAsync(int userId)
        {
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null)
                return;

            user.LastSeenAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}