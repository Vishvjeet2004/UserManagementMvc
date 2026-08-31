namespace UserManagementMvc.Models
{
    public class ChatEmoji
    {
        public int Id { get; set; }

        public string Emoji { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}