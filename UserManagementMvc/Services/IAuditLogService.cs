using System.Threading.Tasks;

namespace UserManagementMvc.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(
            int? userId,
            string action,
            string? description = null);
    }
}