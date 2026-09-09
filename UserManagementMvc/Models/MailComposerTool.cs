namespace UserManagementMvc.Models;

public partial class MailComposerTool
{
    public int Id { get; set; }

    public string ToolKey { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public bool IsEnabled { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}