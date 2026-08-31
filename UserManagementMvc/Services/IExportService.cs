using System.Threading.Tasks;

namespace UserManagementMvc.Services
{
    public interface IExportService
    {
        Task<byte[]> ExportUsersToExcelAsync(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter);

        Task<byte[]> ExportUsersToPdfAsync(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter);
    }
}