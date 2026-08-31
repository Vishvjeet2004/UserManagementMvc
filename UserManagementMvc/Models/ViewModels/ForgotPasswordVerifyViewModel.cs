using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels
{
    public class ForgotPasswordVerifyViewModel
    {
        public int UserId { get; set; }

        public string LoginId { get; set; } = "";

        public List<ForgotPasswordQuestionViewModel> Questions { get; set; } = new();

        [Required]
        public string NewPassword { get; set; } = "";

        [Required]
        [Compare("NewPassword", ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ForgotPasswordQuestionViewModel
    {
        public int UserSecurityQuestionId { get; set; }

        public string QuestionText { get; set; } = "";

        public string Answer { get; set; } = "";
    }
}