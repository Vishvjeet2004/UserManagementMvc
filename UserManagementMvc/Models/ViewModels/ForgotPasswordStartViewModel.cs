using System.ComponentModel.DataAnnotations;

namespace UserManagementMvc.ViewModels
{
    public class ForgotPasswordStartViewModel
    {
        [Required]
        public string LoginId { get; set; } = "";
    }
}