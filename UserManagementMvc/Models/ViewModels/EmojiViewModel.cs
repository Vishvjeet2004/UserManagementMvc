namespace UserManagementMvc.ViewModels;

public class EmojiViewModel
{
    public int Id { get; set; }

    public string EmojiText { get; set; } = "";

    public string? Name { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }
}