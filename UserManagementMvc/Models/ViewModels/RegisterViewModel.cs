using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string Name { get; set; } = "";

        public string? UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string MobileCountryCode { get; set; } = "+91";

        [Required]
        public string Mobile { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";

        [Required]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; } = "";

        // 3 security questions will be stored here
        public List<RegisterSecurityQuestionViewModel> SecurityQuestions { get; set; } = new()
        {
            new RegisterSecurityQuestionViewModel(),
            new RegisterSecurityQuestionViewModel(),
            new RegisterSecurityQuestionViewModel()
        };
    }
}