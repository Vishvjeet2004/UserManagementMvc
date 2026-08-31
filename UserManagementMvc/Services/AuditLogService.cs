using Microsoft.AspNetCore.Http;
using UserManagementMvc.Models;

namespace UserManagementMvc.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(
            int? userId,
            string action,
            string? description = null)
        {
            string? ipAddress =
                _httpContextAccessor.HttpContext?
                    .Connection.RemoteIpAddress?
                    .ToString();

            var log = new Auditlog
            {
                UserId = userId,
                Action = action,
                Description = description,
                IpAddress = ipAddress,
                CreatedAt = DateTime.Now
            };

            _context.Auditlogs.Add(log);

            await _context.SaveChangesAsync();
        }
    }
}