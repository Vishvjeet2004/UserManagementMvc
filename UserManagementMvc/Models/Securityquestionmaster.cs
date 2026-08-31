using System;
using System.Collections.Generic;

namespace UserManagementMvc.Models;

public partial class Securityquestionmaster
{
    public int Id { get; set; }

    public string QuestionText { get; set; } = null!;

    public bool? IsActive { get; set; }

    public string? CreatedByRole { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Usersecurityquestion> Usersecurityquestions { get; set; } = new List<Usersecurityquestion>();
}