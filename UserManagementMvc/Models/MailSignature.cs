namespace UserManagementMvc.Models;

public partial class MailSignature
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string SignatureHtml { get; set; } = null!;

    public bool IsActive { get; set; }

    public bool IsDefault { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}