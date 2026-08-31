using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Services
{
    public class LoginService
    {
        private readonly AppDbContext _context;

        public LoginService(AppDbContext context)
        {
            _context = context;
        }

        // Common login validation for User, Admin and SuperAdmin
        public async Task<LoginResult> ValidateLoginAsync(
            LoginViewModel model,
            string expectedRole)
        {
            if (string.IsNullOrWhiteSpace(model.LoginId) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Login Id and Password are required."
                };
            }

            // User can login using Email, Username, Mobile or CountryCode + Mobile
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    (x.Email == model.LoginId ||
                     x.UserName == model.LoginId ||
                     x.Mobile == model.LoginId ||
                     (x.MobileCountryCode + x.Mobile) == model.LoginId)
                    && x.IsDeleted == false
                    && x.IsActive == true);

            if (user == null)
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid login id or password."
                };
            }

            var hasher = new PasswordHasher<User>();

            var passwordResult = hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password
            );

            if (passwordResult == PasswordVerificationResult.Failed)
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Invalid login id or password."
                };
            }

            // Separate login page role validation
            if (user.Role != expectedRole)
            {
                return new LoginResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Only {expectedRole} can login from this page."
                };
            }

            return new LoginResult
            {
                IsSuccess = true,
                User = user
            };
        }
    }

    public class LoginResult
    {
        public bool IsSuccess { get; set; }

        public string ErrorMessage { get; set; } = "";

        public User? User { get; set; }
    }
}