namespace UserManagementMvc.ViewModels
{
    public class ForgotPasswordMethodViewModel
    {
        public int UserId { get; set; }

        public string? Method { get; set; }

        public string? MaskedEmail { get; set; }
    }
}