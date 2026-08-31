using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userName =
                HttpContext.Session.GetString("UserName");

            if (string.IsNullOrWhiteSpace(userName))
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            var userRole =
                HttpContext.Session.GetString("UserRole");

            var model = new DashboardViewModel
            {
                UserName = userName,
                UserRole = userRole,

                TotalUsers = await _context.Users
                    .CountAsync(x => x.IsDeleted != true),

                ActiveUsers = await _context.Users
                    .CountAsync(x =>
                        x.IsDeleted != true &&
                        x.IsActive == true),

                BlockedUsers = await _context.Users
                    .CountAsync(x =>
                        x.IsDeleted != true &&
                        x.IsBlocked == true),

                DeletedUsers = await _context.Users
                    .CountAsync(x =>
                        x.IsDeleted == true),

                AdminUsers = await _context.Users
                    .CountAsync(x =>
                        x.IsDeleted != true &&
                        x.Role == "Admin"),

                SuperAdminUsers = await _context.Users
                    .CountAsync(x =>
                        x.IsDeleted != true &&
                        x.Role == "SuperAdmin"),

                NormalUsers = await _context.Users
                    .CountAsync(x =>
                        x.IsDeleted != true &&
                        (x.Role == "User" || x.Role == null)),

                TotalAuditLogs = await _context.Auditlogs
                    .CountAsync()
            };

            return View(model);
        }
    }
}