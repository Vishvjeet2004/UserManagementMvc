using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;

        public AuthController(AppDbContext context)
        {
            _context = context;
        }

        
        // Register Page
        
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            await LoadSecurityQuestions();

            var model = new RegisterViewModel();

            return View(model);
        }

        
        // Register Submit
        
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            await LoadSecurityQuestions();

            EnsureThreeSecurityQuestions(model);

            if (!ModelState.IsValid)
                return View(model);

            int requiredLength = GetMobileLength(model.MobileCountryCode);

            if (model.Mobile.Length != requiredLength)
            {
                ViewBag.Error = $"Mobile number must be {requiredLength} digits for selected country.";
                return View(model);
            }

            string? questionError = await ValidateSecurityQuestions(model);

            if (questionError != null)
            {
                ViewBag.Error = questionError;
                return View(model);
            }

            var emailExists = await _context.Users.AnyAsync(x => x.Email == model.Email);

            if (emailExists)
            {
                ViewBag.Error = "Email already exists.";
                return View(model);
            }

            var mobileExists = await _context.Users.AnyAsync(x =>
                x.MobileCountryCode == model.MobileCountryCode &&
                x.Mobile == model.Mobile);

            if (mobileExists)
            {
                ViewBag.Error = "Mobile number already exists.";
                return View(model);
            }

            string finalUserName;

            if (string.IsNullOrWhiteSpace(model.UserName))
            {
                finalUserName = "user" + DateTime.Now.Ticks.ToString().Substring(10);
            }
            else
            {
                finalUserName = model.UserName.Trim();
            }

            var userNameExists = await _context.Users.AnyAsync(x => x.UserName == finalUserName);

            if (userNameExists)
            {
                ViewBag.Error = "Username already exists.";
                return View(model);
            }

            var user = new User
            {
                Name = model.Name,
                UserName = finalUserName,
                Email = model.Email,
                MobileCountryCode = model.MobileCountryCode,
                Mobile = model.Mobile,
                Role = "User",
                Department = "General",
                IsDeleted = false,
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, model.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            await SaveUserSecurityQuestions(user, model);

            TempData["Success"] = "Registration successful. Please login.";
            return RedirectToAction("Login", "UserAuth");
        }

        
        // Forgot Password Start Page
        
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordStartViewModel());
        }

        
        // Forgot Password Start Submit
        
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordStartViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await FindUserByLoginId(model.LoginId);

            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View(model);
            }

            var verifyModel = new ForgotPasswordVerifyViewModel
            {
                UserId = user.Id,
                LoginId = model.LoginId,
                Questions = await GetForgotPasswordQuestions(user.Id)
            };

            if (verifyModel.Questions.Count == 0)
            {
                ViewBag.Error = "Security questions are not set for this account.";
                return View(model);
            }

            return View("ForgotPasswordVerify", verifyModel);
        }

        
        // Forgot Password Verify Submit
        
        [HttpPost]
        public async Task<IActionResult> ForgotPasswordVerify(ForgotPasswordVerifyViewModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Id == model.UserId &&
                x.IsDeleted == false &&
                x.IsActive == true);

            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View(model);
            }

            if (!ModelState.IsValid)
            {
                model.Questions = await GetForgotPasswordQuestions(user.Id);
                return View(model);
            }

            var hasher = new PasswordHasher<User>();

            foreach (var item in model.Questions)
            {
                var savedQuestion = await _context.UserSecurityQuestions
                    .FirstOrDefaultAsync(x =>
                        x.Id == item.UserSecurityQuestionId &&
                        x.UserId == user.Id);

                if (savedQuestion == null)
                {
                    ViewBag.Error = "Invalid security question.";
                    model.Questions = await GetForgotPasswordQuestions(user.Id);
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(item.Answer))
                {
                    ViewBag.Error = "Please answer all security questions.";
                    model.Questions = await GetForgotPasswordQuestions(user.Id);
                    return View(model);
                }

                var answerResult = hasher.VerifyHashedPassword(
                    user,
                    savedQuestion.SecurityAnswerHash,
                    item.Answer.Trim().ToLower()
                );

                if (answerResult == PasswordVerificationResult.Failed)
                {
                    ViewBag.Error = "One or more security answers are incorrect.";
                    model.Questions = await GetForgotPasswordQuestions(user.Id);
                    return View(model);
                }
            }

            user.PasswordHash = hasher.HashPassword(user, model.NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successful. Please login.";
            return RedirectToAction("Login", "UserAuth");
        }

        
        // Logout
        
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Index", "Home");
        }

                // Load security questions from database
        
        private async Task LoadSecurityQuestions()
        {
            ViewBag.SecurityQuestions = await _context.SecurityQuestionMasters
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.QuestionText)
                .ToListAsync();
        }

        //  model always has 3 security questions
        
        private void EnsureThreeSecurityQuestions(RegisterViewModel model)
        {
            if (model.SecurityQuestions == null)
            {
                model.SecurityQuestions = new List<RegisterSecurityQuestionViewModel>();
            }

            while (model.SecurityQuestions.Count < 3)
            {
                model.SecurityQuestions.Add(new RegisterSecurityQuestionViewModel());
            }
        }

        // Validate 3 security questions
        
        private async Task<string?> ValidateSecurityQuestions(RegisterViewModel model)
        {
            EnsureThreeSecurityQuestions(model);

            foreach (var item in model.SecurityQuestions)
            {
                if (item.SecurityQuestionMasterId == null)
                    return "Please select all 3 security questions.";

                if (string.IsNullOrWhiteSpace(item.Answer))
                    return "Please enter answer for all security questions.";

                if (item.SecurityQuestionMasterId == 0 &&
                    string.IsNullOrWhiteSpace(item.QuestionText))
                {
                    return "Please enter your custom security question.";
                }

                if (item.SecurityQuestionMasterId > 0)
                {
                    var exists = await _context.SecurityQuestionMasters.AnyAsync(x =>
                        x.Id == item.SecurityQuestionMasterId &&
                        x.IsActive == true);

                    if (!exists)
                        return "Selected security question is not valid.";
                }
            }

            var masterQuestionIds = model.SecurityQuestions
                .Where(x => x.SecurityQuestionMasterId > 0)
                .Select(x => x.SecurityQuestionMasterId!.Value)
                .ToList();

            if (masterQuestionIds.Count != masterQuestionIds.Distinct().Count())
            {
                return "Please select different security questions.";
            }

            var customQuestions = model.SecurityQuestions
                .Where(x => x.SecurityQuestionMasterId == 0)
                .Select(x => x.QuestionText!.Trim().ToLower())
                .ToList();

            if (customQuestions.Count != customQuestions.Distinct().Count())
            {
                return "Please enter different custom security questions.";
            }

            return null;
        }


        // Save user's 3 security questions
      
        private async Task SaveUserSecurityQuestions(User user, RegisterViewModel model)
        {
            var hasher = new PasswordHasher<User>();

            foreach (var item in model.SecurityQuestions)
            {
                var userQuestion = new Usersecurityquestion
                {
                    UserId = user.Id,
                    CreatedAt = DateTime.Now,
                    SecurityAnswerHash = hasher.HashPassword(
                        user,
                        item.Answer.Trim().ToLower()
                    )
                };

                if (item.SecurityQuestionMasterId == 0)
                {
                    userQuestion.SecurityQuestionMasterId = null;
                    userQuestion.QuestionText = item.QuestionText!.Trim();
                }
                else
                {
                    userQuestion.SecurityQuestionMasterId = item.SecurityQuestionMasterId;
                    userQuestion.QuestionText = null;
                }

                _context.UserSecurityQuestions.Add(userQuestion);
            }

            await _context.SaveChangesAsync();
        }

            
        // Find user by username/email/mobile
       
        private async Task<User?> FindUserByLoginId(string loginId)
        {
            return await _context.Users.FirstOrDefaultAsync(x =>
                (x.Email == loginId ||
                 x.UserName == loginId ||
                 x.Mobile == loginId ||
                 (x.MobileCountryCode + x.Mobile) == loginId)
                && x.IsDeleted == false
                && x.IsActive == true);
        }

        
        // Get saved questions for forgot password
        
        private async Task<List<ForgotPasswordQuestionViewModel>> GetForgotPasswordQuestions(int userId)
        {
            var savedQuestions = await _context.UserSecurityQuestions
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.Id)
                .ToListAsync();

            var result = new List<ForgotPasswordQuestionViewModel>();

            foreach (var item in savedQuestions)
            {
                string questionText;

                if (item.SecurityQuestionMasterId != null)
                {
                    questionText = await _context.SecurityQuestionMasters
                        .Where(x => x.Id == item.SecurityQuestionMasterId)
                        .Select(x => x.QuestionText)
                        .FirstOrDefaultAsync() ?? "Security Question";
                }
                else
                {
                    questionText = item.QuestionText ?? "Security Question";
                }

                result.Add(new ForgotPasswordQuestionViewModel
                {
                    UserSecurityQuestionId = item.Id,
                    QuestionText = questionText,
                    Answer = ""
                });
            }

            return result;
        }

        
        // Mobile length country-wise
        
        private int GetMobileLength(string countryCode)
        {
            return countryCode switch
            {
                "+91" => 10,
                "+1" => 10,
                "+44" => 10,
                "+977" => 10,
                "+971" => 9,
                "+92" => 10,
                "+880" => 10,
                _ => 10
            };
        }
    }
}