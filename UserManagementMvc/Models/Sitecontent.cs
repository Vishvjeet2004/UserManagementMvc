using System;

namespace UserManagementMvc.Models;

public partial class Sitecontent
{
    public int Id { get; set; }

    public string ContentKey { get; set; } = null!;

    public string? Title { get; set; }

    public string? Content { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}