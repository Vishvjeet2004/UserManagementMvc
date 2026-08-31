namespace UserManagementMvc.ViewModels
{
    public class ChatMessageViewModel
    {
        public int Id { get; set; }

        public int SenderUserId { get; set; }

        public int RecipientUserId { get; set; }

        public string MessageText { get; set; } = "";

        public string SentAt { get; set; } = "";

        public string? DeliveredAt { get; set; }

        public string? SeenAt { get; set; }

        public bool IsMine { get; set; }

        public string Status { get; set; } = "sent";

        // Attachment
        public string? AttachmentUrl { get; set; }

        public string? AttachmentName { get; set; }

        public string? AttachmentContentType { get; set; }

        public long? AttachmentSize { get; set; }
    }
}