using Microsoft.AspNetCore.SignalR;
using UserManagementMvc.Services;

namespace UserManagementMvc.Hubs
{
    public class MessengerHub : Hub
    {
        private readonly MessengerService _messengerService;

        public MessengerHub(
            MessengerService messengerService)
        {
            _messengerService =
                messengerService;
        }

        private int? GetCurrentUserId()
        {
            return Context
                .GetHttpContext()?
                .Session
                .GetInt32("UserId");
        }

        private static string UserGroup(
            int userId)
        {
            return $"User_{userId}";
        }

         
        // CONNECT
         

        public override async Task OnConnectedAsync()
        {
            var userId =
                GetCurrentUserId();

            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    UserGroup(userId.Value));

                await _messengerService
                    .UpdateLastSeenAsync(
                        userId.Value);
            }

            await base.OnConnectedAsync();
        }

         
        // DISCONNECT
         

        public override async Task OnDisconnectedAsync(
            Exception? exception)
        {
            var userId =
                GetCurrentUserId();

            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(
                    Context.ConnectionId,
                    UserGroup(userId.Value));

                await _messengerService
                    .UpdateLastSeenAsync(
                        userId.Value);
            }

            await base.OnDisconnectedAsync(
                exception);
        }

         
        // SEND MESSAGE
         

        public async Task SendMessage(
            int recipientUserId,
            string? messageText,
            string? attachmentUrl = null,
            string? attachmentName = null,
            string? attachmentContentType = null,
            long? attachmentSize = null)
        {
            var senderUserId =
                GetCurrentUserId();

            if (!senderUserId.HasValue)
                return;

            if (senderUserId.Value ==
                recipientUserId)
                return;

            // Only allow our own upload folder
            if (!string.IsNullOrWhiteSpace(
                    attachmentUrl) &&
                !attachmentUrl.StartsWith(
                    "/uploads/chat/",
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var message =
                await _messengerService
                    .SendMessageAsync(
                        senderUserId.Value,
                        recipientUserId,
                        messageText,
                        attachmentUrl,
                        attachmentName,
                        attachmentContentType,
                        attachmentSize);

            if (message == null)
                return;

            var data = new
            {
                id = message.Id,

                senderUserId =
                    message.SenderUserId,

                recipientUserId =
                    message.RecipientUserId,

                messageText =
                    message.MessageText,

                sentAt =
                    message.SentAt.ToString(
                        "hh:mm tt"),

                status = "sent",

                attachmentUrl =
                    message.AttachmentUrl,

                attachmentName =
                    message.AttachmentName,

                attachmentContentType =
                    message.AttachmentContentType,

                attachmentSize =
                    message.AttachmentSize
            };

            // Recipient receives message
            await Clients.Group(
                UserGroup(recipientUserId))
                .SendAsync(
                    "ReceiveMessage",
                    data);

            // Sender receives confirmation
            await Clients.Caller.SendAsync(
                "MessageSent",
                data);

            // Update recipient conversation list
            await Clients.Group(
                UserGroup(recipientUserId))
                .SendAsync(
                    "ConversationUpdated",
                    new
                    {
                        userId =
                            senderUserId.Value
                    });

            // Update sender conversation list
            await Clients.Group(
                UserGroup(senderUserId.Value))
                .SendAsync(
                    "ConversationUpdated",
                    new
                    {
                        userId =
                            recipientUserId
                    });
        }

         
        // MARK DELIVERED
         

        public async Task MarkDelivered(
            int otherUserId)
        {
            var currentUserId =
                GetCurrentUserId();

            if (!currentUserId.HasValue)
                return;

            bool updated =
                await _messengerService
                    .MarkDeliveredAsync(
                        currentUserId.Value,
                        otherUserId);

            if (updated)
            {
                await Clients.Group(
                    UserGroup(otherUserId))
                    .SendAsync(
                        "MessagesDelivered",
                        currentUserId.Value);

                await Clients.Group(
                    UserGroup(currentUserId.Value))
                    .SendAsync(
                        "ConversationUpdated",
                        new
                        {
                            userId =
                                otherUserId
                        });
            }
        }

         
        // MARK SEEN
         

        public async Task MarkSeen(
            int otherUserId)
        {
            var currentUserId =
                GetCurrentUserId();

            if (!currentUserId.HasValue)
                return;

            bool updated =
                await _messengerService
                    .MarkSeenAsync(
                        currentUserId.Value,
                        otherUserId);

            if (updated)
            {
                await Clients.Group(
                    UserGroup(otherUserId))
                    .SendAsync(
                        "MessagesSeen",
                        currentUserId.Value);

                // Refresh recipient's unread count
                await Clients.Group(
                    UserGroup(currentUserId.Value))
                    .SendAsync(
                        "ConversationUpdated",
                        new
                        {
                            userId =
                                otherUserId
                        });
            }
        }
    }
}