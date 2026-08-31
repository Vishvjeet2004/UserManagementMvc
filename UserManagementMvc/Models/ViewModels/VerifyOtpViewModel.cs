using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels
{
    public class VerifyOtpViewModel
    {
        public int UserId { get; set; }

        public string Purpose { get; set; } = "";

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = "";
    }
}