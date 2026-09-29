namespace UserManagementMvc.Services;

public sealed class ServerMailAttachment
{
    public string Id { get; set; } = "";

    public string OriginalFileName { get; set; } = "";

    public string ContentType { get; set; } =
        "application/octet-stream";

    public long FileSize { get; set; }

    public bool IsInline { get; set; }

    public string? ContentId { get; set; }
}