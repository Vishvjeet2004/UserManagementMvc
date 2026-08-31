using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels
{
    public class RegisterSecurityQuestionViewModel
    {
        // Master table question id
        // 0 means Other option
        public int? SecurityQuestionMasterId { get; set; }

        // Used only when user selects Other
        public string? QuestionText { get; set; }

        [Required]
        public string Answer { get; set; } = "";
    }
}