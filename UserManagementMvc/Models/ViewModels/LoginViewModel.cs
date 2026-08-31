using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Login ID is required.")]
        public string LoginId { get; set; } = "";

        public string MobileCountryCode { get; set; } = "+91";

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = "";
    }
}