using System;

namespace UserManagementMvc.Models;

public partial class Sitecontentsection
{
    public int Id { get; set; }

    public string PageName { get; set; } = null!;

    public string SectionTitle { get; set; } = null!;

    public string? SectionContent { get; set; }

    public int? DisplayOrder { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}