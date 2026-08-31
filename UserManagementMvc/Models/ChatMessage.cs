using System;

namespace UserManagementMvc.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int SenderUserId { get; set; }

        public int RecipientUserId { get; set; }

        public string MessageText { get; set; } = "";

        public DateTime SentAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? SeenAt { get; set; }

        public bool IsDeletedBySender { get; set; }

        public bool IsDeletedByRecipient { get; set; }

        // Attachment
        public string? AttachmentUrl { get; set; }

        public string? AttachmentName { get; set; }

        public string? AttachmentContentType { get; set; }

        public long? AttachmentSize { get; set; }

        public virtual User? SenderUser { get; set; }

        public virtual User? RecipientUser { get; set; }
    }
}