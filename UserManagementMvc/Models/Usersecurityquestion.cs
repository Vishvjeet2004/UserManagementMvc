using System;

namespace UserManagementMvc.Models;

public partial class Usersecurityquestion
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? SecurityQuestionMasterId { get; set; }

    public string? QuestionText { get; set; }

    public string SecurityAnswerHash { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Securityquestionmaster? SecurityQuestionMaster { get; set; }

    public virtual User User { get; set; } = null!;
}