using Microsoft.AspNetCore.Mvc;
using UserManagementMvc.Services;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers
{
    public class AdminAuthController : Controller
    {
        private readonly LoginService _loginService;

        public AdminAuthController(LoginService loginService)
        {
            _loginService = loginService;
        }

        // Admin Login Page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Admin Login Submit
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var result = await _loginService.ValidateLoginAsync(model, "Admin");

            if (!result.IsSuccess)
            {
                ViewBag.Error = result.ErrorMessage;
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", result.User!.Id);
            HttpContext.Session.SetString("UserName", result.User.Name);
            HttpContext.Session.SetString("UserRole", result.User.Role ?? "Admin");

            return RedirectToAction("Index", "Dashboard");
        }
    }
}