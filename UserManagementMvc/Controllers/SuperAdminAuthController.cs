using Microsoft.AspNetCore.Mvc;
using UserManagementMvc.Services;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers
{
    public class SuperAdminAuthController : Controller
    {
        private readonly LoginService _loginService;

        public SuperAdminAuthController(LoginService loginService)
        {
            _loginService = loginService;
        }

        // SuperAdmin Login Page
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // SuperAdmin Login Submit
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var result = await _loginService.ValidateLoginAsync(model, "SuperAdmin");

            if (!result.IsSuccess)
            {
                ViewBag.Error = result.ErrorMessage;
                return View(model);
            }

            HttpContext.Session.SetInt32("UserId", result.User!.Id);
            HttpContext.Session.SetString("UserName", result.User.Name);
            HttpContext.Session.SetString("UserRole", result.User.Role ?? "SuperAdmin");

            return RedirectToAction("Index", "Dashboard");
        }
    }
}