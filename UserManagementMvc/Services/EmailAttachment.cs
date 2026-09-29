namespace UserManagementMvc.Services;

public sealed class EmailAttachment
{
    public string FilePath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string ContentType { get; set; } = "application/octet-stream";
}
