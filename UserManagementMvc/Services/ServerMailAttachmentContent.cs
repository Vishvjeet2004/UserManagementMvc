namespace UserManagementMvc.Services;

public sealed class ServerMailAttachmentContent
{
    public string FileName { get; set; } = "";

    public string ContentType { get; set; } =
        "application/octet-stream";

    public byte[] Content { get; set; } = Array.Empty<byte>();
}