using System;

namespace UserManagementMvc.Models
{
    public partial class Auditlog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } = null!;

        public string? Description { get; set; }

        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual User? User { get; set; }
    }
}