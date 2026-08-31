namespace UserManagementMvc.ViewModels
{
    public class DashboardViewModel
    {
        public string? UserName { get; set; }

        public string? UserRole { get; set; }

        public int TotalUsers { get; set; }

        public int ActiveUsers { get; set; }

        public int BlockedUsers { get; set; }

        public int DeletedUsers { get; set; }

        public int AdminUsers { get; set; }

        public int SuperAdminUsers { get; set; }

        public int NormalUsers { get; set; }

        public int TotalAuditLogs { get; set; }
    }
}