using Microsoft.AspNetCore.Mvc;
using UserManagementMvc.Services;

namespace UserManagementMvc.Controllers
{
    public class ExportController : Controller
    {
        private readonly IExportService _exportService;
        private readonly IAuditLogService _auditLogService;

        public ExportController(
            IExportService exportService,
            IAuditLogService auditLogService)
        {
            _exportService = exportService;
            _auditLogService = auditLogService;
        }


       
        // SUPER ADMIN CHECK
       
        private bool IsSuperAdmin()
        {
            return string.Equals(
                HttpContext.Session.GetString("UserRole"),
                "SuperAdmin",
                StringComparison.OrdinalIgnoreCase);
        }


        // PDF EXPORT


        [HttpGet]
        public async Task<IActionResult> UsersPdf(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter)
        {
            // Login check
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }


            // Only SuperAdmin
            if (!IsSuperAdmin())
            {
                return Forbid();
            }


            try
            {
                byte[] pdf =
                    await _exportService
                        .ExportUsersToPdfAsync(
                            search,
                            roleFilter,
                            departmentFilter,
                            statusFilter);


                await _auditLogService.LogAsync(
                    userId,
                    "Export PDF",
                    "SuperAdmin exported user list as PDF."
                );


                string fileName =
                    $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";


                return File(
                    pdf,
                    "application/pdf",
                    fileName);
            }
            catch (Exception ex)
            {
                // IMPORTANT:
                // Abhi actual error visible karne ke liye
                // exception throw kar rahe hain.

                throw new Exception(
                    "PDF export failed. Check InnerException for the exact reason.",
                    ex);
            }
        }


       
        // EXCEL EXPORT
        

        [HttpGet]
        public async Task<IActionResult> UsersExcel(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter)
        {
            // Login check
            int? userId =
                HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }


            // Only SuperAdmin
            if (!IsSuperAdmin())
            {
                return Forbid();
            }


            try
            {
                byte[] excel =
                    await _exportService
                        .ExportUsersToExcelAsync(
                            search,
                            roleFilter,
                            departmentFilter,
                            statusFilter);


                await _auditLogService.LogAsync(
                    userId,
                    "Export Excel",
                    "SuperAdmin exported user list as Excel."
                );


                string fileName =
                    $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";


                return File(
                    excel,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Excel export failed.",
                    ex);
            }
        }
    }
}