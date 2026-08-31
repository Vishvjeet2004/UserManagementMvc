using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels
{
    public class ResetPasswordOtpViewModel
    {
        public int UserId { get; set; }

        [Required]
        public string NewPassword { get; set; } = "";

        [Required]
        public string ConfirmPassword { get; set; } = "";
    }
}