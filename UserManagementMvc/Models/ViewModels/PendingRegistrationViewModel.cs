using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Models.ViewModels
{
    internal class PendingRegistrationViewModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string MobileCountryCode { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string Department { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public List<RegisterSecurityQuestionViewModel> SecurityQuestions { get; set; }
    }
}