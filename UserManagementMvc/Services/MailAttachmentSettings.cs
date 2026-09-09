namespace UserManagementMvc.Services;

public class MailAttachmentSettings
{
    public int MaxFileSizeMb { get; set; } = 25;

    public int MaxFilesPerMail { get; set; } = 10;

    public List<string> AllowedExtensions { get; set; }
        = new();
}