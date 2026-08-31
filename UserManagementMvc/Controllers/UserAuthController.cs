using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using UserManagementMvc.Models;
using UserManagementMvc.ViewModels;
using System.Text.Json;
using UserManagementMvc.Models.ViewModels;

namespace UserManagementMvc.Controllers
{
    public class UserAuthController : Controller
    {
        private readonly AppDbContext _context;

        public UserAuthController(AppDbContext context)
        {
            _context = context;
        }

        // Login Page
      
        [HttpGet]
        public IActionResult Login()
        {
            Response.Headers["Cache-Control"] =
                "no-cache, no-store, must-revalidate";

            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            if (HttpContext.Session.GetInt32("UserId") != null)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View(new LoginViewModel());
        }


        // Login POST
        // User/Admin: 3 wrong attempts = block
        // SuperAdmin: lockout rule apply नहीं होगा
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            Response.Headers["Cache-Control"] =
                "no-cache, no-store, must-revalidate";

            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string loginId = model.LoginId.Trim();

            // Login Email, Username या Mobile से हो सकता है
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.IsDeleted == false &&
                (
                    x.Email == loginId ||
                    x.UserName == loginId ||
                    x.Mobile == loginId ||
                    (x.MobileCountryCode + x.Mobile) == loginId
                ));

            // Random/non-existing ID पर किसी user का counter नहीं बढ़ेगा
            if (user == null)
            {
                ViewBag.Error = "Invalid login ID or password.";
                return View(model);
            }

            // Inactive account login नहीं करेगा
            if (user.IsActive == false)
            {
                ViewBag.Error =
                    "Your account is inactive. Please contact the administrator.";

                return View(model);
            }

            bool isSuperAdmin =
                string.Equals(
                    user.Role,
                    "SuperAdmin",
                    StringComparison.OrdinalIgnoreCase
                );

            // User/Admin account पहले से blocked है
            if (!isSuperAdmin && user.IsBlocked)
            {
                ViewBag.Error =
                    "Your ID is blocked because of multiple failed login attempts. " +
                    "Only the Super Admin can unblock your account.";

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                ViewBag.Error =
                    "Password is not configured for this account.";

                return View(model);
            }

            var hasher = new PasswordHasher<User>();

            var passwordResult = hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password
            );

        
            // Wrong Password
        
            if (passwordResult == PasswordVerificationResult.Failed)
            {
                // SuperAdmin को lock नहीं करेंगे
                if (isSuperAdmin)
                {
                    ViewBag.Error = "Invalid login ID or password.";
                    return View(model);
                }

                user.FailedLoginAttempts++;

                // Third failed attempt
                if (user.FailedLoginAttempts >= 3)
                {
                    user.FailedLoginAttempts = 3;
                    user.IsBlocked = true;
                    user.BlockedAt = DateTime.Now;
                    user.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    ViewBag.Error =
                        "Your ID has been blocked after 3 failed login attempts. " +
                        "Only the Super Admin can unblock your account.";

                    return View(model);
                }

                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                int remainingAttempts =
                    3 - user.FailedLoginAttempts;

                if (remainingAttempts == 1)
                {
                    ViewBag.Error =
                        "Invalid password. This is your last chance. " +
                        "One more failed attempt will block your ID.";
                }
                else
                {
                    ViewBag.Error =
                        $"Invalid password. You have {remainingAttempts} attempts remaining.";
                }

                return View(model);
            }

            
            // Correct Password
            // Reset failed attempts
            
            if (!isSuperAdmin && user.FailedLoginAttempts > 0)
            {
                user.FailedLoginAttempts = 0;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            // Session values
            HttpContext.Session.SetInt32(
                "UserId",
                user.Id
            );

            HttpContext.Session.SetString(
                "UserName",
                user.Name ?? ""
            );

            HttpContext.Session.SetString(
                "UserRole",
                user.Role ?? "User"
            );

            HttpContext.Session.SetString(
                "UserDepartment",
                user.Department?.Trim() ?? ""
            );

            return RedirectToAction("Index", "Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            await LoadSecurityQuestionMasters();

            return View("~/Views/Auth/Register.cshtml", new User());
        }


        [HttpGet]
        public async Task<IActionResult> CheckEmailAvailability(
    string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new
                {
                    valid = false,
                    available = false,
                    message = "Email address is required."
                });
            }

            string cleanEmail =
                email.Trim().ToLower();

            string emailPattern =
                @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";

            bool validFormat =
                System.Text.RegularExpressions.Regex.IsMatch(
                    cleanEmail,
                    emailPattern);

            if (!validFormat)
            {
                return Json(new
                {
                    valid = false,
                    available = false,
                    message = "Please enter a valid email address."
                });
            }

            bool emailExists =
                await _context.Users.AnyAsync(x =>
                    x.Email == cleanEmail);

            if (emailExists)
            {
                return Json(new
                {
                    valid = true,
                    available = false,
                    message = "This email is already registered."
                });
            }

            return Json(new
            {
                valid = true,
                available = true,
                message = "Email is available."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
    User user,
    string Password,
    string? ConfirmPassword,
    List<RegisterSecurityQuestionViewModel>? SecurityQuestions)
        {
            await LoadSecurityQuestionMasters();

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                ViewBag.Error = "Name is required.";
                return View("~/Views/Auth/Register.cshtml", user);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ViewBag.Error = "Email is required.";
                return View("~/Views/Auth/Register.cshtml", user);
            }

            user.Email = user.Email.Trim().ToLower();

            string emailPattern =
                @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    user.Email,
                    emailPattern))
            {
                ViewBag.Error =
                    "Please enter a valid email address.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            if (user.DateOfBirth == null)
            {
                ViewBag.Error =
                    "Date of birth is required.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            if (user.DateOfBirth.Value.Date > DateTime.Today)
            {
                ViewBag.Error =
                    "Date of birth cannot be a future date.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            DateTime minimumAllowedDate =
                DateTime.Today.AddYears(-18);

            if (user.DateOfBirth.Value.Date >
                minimumAllowedDate)
            {
                ViewBag.Error =
                    "You must be at least 18 years old to register.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            if (string.IsNullOrWhiteSpace(user.Mobile))
            {
                ViewBag.Error =
                    "Mobile number is required.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error =
                    "Password is required.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            if (string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ViewBag.Error =
                    "Confirm password is required.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            if (Password != ConfirmPassword)
            {
                ViewBag.Error =
                    "Password and confirm password do not match.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            user.MobileCountryCode =
                string.IsNullOrWhiteSpace(user.MobileCountryCode)
                    ? "+91"
                    : user.MobileCountryCode;

            int requiredLength =
                GetMobileLength(user.MobileCountryCode);

            if (user.Mobile.Length != requiredLength)
            {
                ViewBag.Error =
                    $"Mobile number must be {requiredLength} digits for selected country.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            bool emailExists =
                await _context.Users.AnyAsync(x =>
                    x.Email == user.Email);

            if (emailExists)
            {
                ViewBag.Error =
                    "Email is already registered.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            bool mobileExists =
                await _context.Users.AnyAsync(x =>
                    x.MobileCountryCode ==
                        user.MobileCountryCode &&
                    x.Mobile == user.Mobile);

            if (mobileExists)
            {
                ViewBag.Error =
                    "Mobile number is already registered.";

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                user.UserName =
                    await GenerateUniqueUserName(user.Name);
            }
            else
            {
                user.UserName =
                    CleanUserName(user.UserName);

                bool userNameExists =
                    await _context.Users.AnyAsync(x =>
                        x.UserName == user.UserName);

                if (userNameExists)
                {
                    ViewBag.Error =
                        "Username already exists.";

                    return View(
                        "~/Views/Auth/Register.cshtml",
                        user);
                }
            }

            string? securityQuestionError =
                await ValidateSecurityQuestions(
                    SecurityQuestions);

            if (securityQuestionError != null)
            {
                ViewBag.Error =
                    securityQuestionError;

                return View(
                    "~/Views/Auth/Register.cshtml",
                    user);
            }

            var hasher =
                new PasswordHasher<User>();

            string passwordHash =
                hasher.HashPassword(
                    user,
                    Password);

            var pendingRegistration =
                new PendingRegistrationViewModel
                {
                    Name = user.Name.Trim(),

                    Email = user.Email,

                    Mobile = user.Mobile,

                    MobileCountryCode =
                        user.MobileCountryCode,

                    UserName =
                        user.UserName,

                    PasswordHash =
                        passwordHash,

                    Department =
                        string.IsNullOrWhiteSpace(
                            user.Department)
                            ? "General"
                            : user.Department,

                    DateOfBirth =
                        user.DateOfBirth,

                    SecurityQuestions =
                        SecurityQuestions!
                };

            string pendingJson =
                JsonSerializer.Serialize(
                    pendingRegistration);

            HttpContext.Session.SetString(
                "PendingRegistration",
                pendingJson);

            return RedirectToAction(
                "SendRegistrationOtp",
                "EmailOtp");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string? LoginId)
        {
            if (string.IsNullOrWhiteSpace(LoginId))
            {
                ViewBag.Error = "Please enter username, email or mobile.";

                return View(
                    "~/Views/Auth/ForgotPassword.cshtml");
            }

            string loginId =
                LoginId.Trim();

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.IsDeleted == false &&
                        x.IsActive == true &&
                        (
                            x.Email == loginId ||
                            x.UserName == loginId ||
                            x.Mobile == loginId ||
                            (x.MobileCountryCode + x.Mobile) == loginId
                        ));

            if (user == null)
            {
                ViewBag.Error =
                    "No active account was found with these details.";

                return View(
                    "~/Views/Auth/ForgotPassword.cshtml");
            }

            return View(
                "~/Views/Auth/ForgotPasswordMethod.cshtml",
                new ForgotPasswordMethodViewModel
                {
                    UserId = user.Id,
                    MaskedEmail = MaskEmail(user.Email)
                });
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectForgotPasswordMethod(
     ForgotPasswordMethodViewModel model)
        {
            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.UserId &&
                        x.IsDeleted == false &&
                        x.IsActive == true);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(
                    "ForgotPassword");
            }

            if (model.Method == "SecurityQuestion")
            {
                await LoadForgotPasswordQuestionData(user);

                return View(
                    "~/Views/Auth/ForgotPasswordVerify.cshtml");
            }

            if (model.Method == "Otp")
            {
                // IMPORTANT:
                // User mil gaya hai.
                // Ab OTP controller OTP send karega.

                return RedirectToAction(
                    "SendPasswordResetOtp",
                    "EmailOtp",
                    new
                    {
                        userId = user.Id
                    });
            }

            TempData["Error"] =
                "Please select a recovery method.";

            return View(
                "~/Views/Auth/ForgotPasswordMethod.cshtml",
                new ForgotPasswordMethodViewModel
                {
                    UserId = user.Id,
                    MaskedEmail =
                        MaskEmail(user.Email)
                });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(
            int UserId,
            List<RegisterSecurityQuestionViewModel>? SecurityQuestions,
            string NewPassword,
            string? ConfirmPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x =>
                x.Id == UserId &&
                x.IsDeleted == false &&
                x.IsActive == true);

            if (user == null)
            {
                ViewBag.Error = "User not found.";
                return View("~/Views/Auth/ForgotPassword.cshtml");
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ViewBag.Error = "New password is required.";
                await LoadForgotPasswordQuestionData(user);
                return View("~/Views/Auth/ForgotPasswordVerify.cshtml");
            }

            if (!string.IsNullOrWhiteSpace(ConfirmPassword) &&
                NewPassword != ConfirmPassword)
            {
                ViewBag.Error = "New password and confirm password do not match.";
                await LoadForgotPasswordQuestionData(user);
                return View("~/Views/Auth/ForgotPasswordVerify.cshtml");
            }

            var hasher = new PasswordHasher<User>();

            var savedQuestions = await _context.UserSecurityQuestions
                .Where(x => x.UserId == user.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            if (savedQuestions.Count > 0)
            {
                if (SecurityQuestions == null ||
                    SecurityQuestions.Count < savedQuestions.Count)
                {
                    ViewBag.Error = "Please answer all security questions.";
                    await LoadForgotPasswordQuestionData(user);
                    return View("~/Views/Auth/ForgotPasswordVerify.cshtml");
                }

                for (int i = 0; i < savedQuestions.Count; i++)
                {
                    string enteredAnswer = SecurityQuestions[i].Answer?.Trim().ToLower() ?? "";

                    if (string.IsNullOrWhiteSpace(enteredAnswer))
                    {
                        ViewBag.Error = "Please answer all security questions.";
                        await LoadForgotPasswordQuestionData(user);
                        return View("~/Views/Auth/ForgotPasswordVerify.cshtml");
                    }

                    var answerResult = hasher.VerifyHashedPassword(
                        user,
                        savedQuestions[i].SecurityAnswerHash,
                        enteredAnswer
                    );

                    if (answerResult == PasswordVerificationResult.Failed)
                    {
                        ViewBag.Error = "Security answer is incorrect.";
                        await LoadForgotPasswordQuestionData(user);
                        return View("~/Views/Auth/ForgotPasswordVerify.cshtml");
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(user.SecurityAnswerHash))
                {
                    ViewBag.Error = "Security questions are not set for this account.";
                    return View("~/Views/Auth/ForgotPassword.cshtml");
                }

                if (SecurityQuestions == null ||
                    SecurityQuestions.Count == 0 ||
                    string.IsNullOrWhiteSpace(SecurityQuestions[0].Answer))
                {
                    ViewBag.Error = "Please enter security answer.";
                    await LoadForgotPasswordQuestionData(user);
                    return View("~/Views/Auth/ForgotPasswordVerify.cshtml");
                }

                string oldAnswer = SecurityQuestions[0].Answer.Trim().ToLower();

                var oldAnswerResult = hasher.VerifyHashedPassword(
                    user,
                    user.SecurityAnswerHash,
                    oldAnswer
                );

                if (oldAnswerResult == PasswordVerificationResult.Failed)
                {
                    ViewBag.Error = "Security answer is incorrect.";
                    await LoadForgotPasswordQuestionData(user);
                    return View("~/Views/Auth/ForgotPasswordVerify.cshtml");
                }
            }

            user.PasswordHash = hasher.HashPassword(user, NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Password reset successful. Please login.";
            return RedirectToAction("Login", "UserAuth");
        }

        // Logout
        
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            Response.Headers["Cache-Control"] =
                "no-cache, no-store, must-revalidate";

            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return RedirectToAction("Login", "UserAuth");
        }

        private async Task LoadSecurityQuestionMasters()
        {
            ViewBag.SecurityQuestionMasters = await _context.SecurityQuestionMasters
                .Where(x => x.IsActive == true)
                .OrderBy(x => x.QuestionText)
                .ToListAsync();
        }

        private async Task<string?> ValidateSecurityQuestions(
            List<RegisterSecurityQuestionViewModel>? questions)
        {
            if (questions == null || questions.Count < 3)
                return "Please select 3 security questions.";

            foreach (var item in questions)
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

            var masterIds = questions
                .Where(x => x.SecurityQuestionMasterId > 0)
                .Select(x => x.SecurityQuestionMasterId!.Value)
                .ToList();

            if (masterIds.Count != masterIds.Distinct().Count())
                return "Please select different security questions.";

            var customQuestions = questions
                .Where(x => x.SecurityQuestionMasterId == 0)
                .Select(x => x.QuestionText!.Trim().ToLower())
                .ToList();

            if (customQuestions.Count != customQuestions.Distinct().Count())
                return "Please enter different custom security questions.";

            return null;
        }

        private async Task SaveUserSecurityQuestions(
            User user,
            List<RegisterSecurityQuestionViewModel> questions)
        {
            var hasher = new PasswordHasher<User>();

            var selectedMasterIds = questions
                .Where(x => x.SecurityQuestionMasterId > 0)
                .Select(x => x.SecurityQuestionMasterId!.Value)
                .ToList();

            var masterQuestions = await _context.SecurityQuestionMasters
                .Where(x => selectedMasterIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.QuestionText);

            foreach (var item in questions)
            {
                var newQuestion = new Usersecurityquestion
                {
                    UserId = user.Id,
                    SecurityAnswerHash = hasher.HashPassword(
                        user,
                        item.Answer.Trim().ToLower()
                    ),
                    CreatedAt = DateTime.Now
                };

                if (item.SecurityQuestionMasterId == 0)
                {
                    newQuestion.SecurityQuestionMasterId = null;
                    newQuestion.QuestionText = item.QuestionText!.Trim();
                }
                else
                {
                    newQuestion.SecurityQuestionMasterId = item.SecurityQuestionMasterId;

                    if (masterQuestions.TryGetValue(item.SecurityQuestionMasterId!.Value, out string? questionText))
                    {
                        newQuestion.QuestionText = questionText;
                    }
                    else
                    {
                        newQuestion.QuestionText = "Security question";
                    }
                }

                _context.UserSecurityQuestions.Add(newQuestion);
            }

            user.SecurityQuestion = null;
            user.SecurityAnswerHash = null;
        }

        private async Task LoadForgotPasswordQuestionData(User user)
        {
            ViewBag.UserId = user.Id;

            var savedQuestions = await _context.UserSecurityQuestions
                .Where(x => x.UserId == user.Id)
                .OrderBy(x => x.Id)
                .ToListAsync();

            var questionTexts = new List<string>();

            foreach (var item in savedQuestions)
            {
                if (!string.IsNullOrWhiteSpace(item.QuestionText))
                {
                    questionTexts.Add(item.QuestionText);
                }
                else if (item.SecurityQuestionMasterId != null)
                {
                    var questionText = await _context.SecurityQuestionMasters
                        .Where(x => x.Id == item.SecurityQuestionMasterId)
                        .Select(x => x.QuestionText)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(questionText))
                        questionTexts.Add(questionText);
                }
            }

            if (questionTexts.Count == 0 &&
                !string.IsNullOrWhiteSpace(user.SecurityQuestion))
            {
                questionTexts.Add(user.SecurityQuestion);
            }

            ViewBag.CurrentSecurityQuestions = questionTexts;
        }

        private string CleanUserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "user";
            }

            string cleaned = value.Trim().ToLower();

            cleaned = Regex.Replace(cleaned, @"[^a-z0-9]", "");

            if (string.IsNullOrWhiteSpace(cleaned))
            {
                cleaned = "user";
            }

            if (cleaned.Length > 20)
            {
                cleaned = cleaned.Substring(0, 20);
            }

            return cleaned;
        }

        private async Task<string> GenerateUniqueUserName(string name)
        {
            string baseUserName = CleanUserName(name);

            bool baseExists = await _context.Users.AnyAsync(x =>
                x.UserName == baseUserName);

            if (!baseExists)
            {
                return baseUserName;
            }

            for (int i = 1; i <= 100; i++)
            {
                int randomNumber = Random.Shared.Next(100, 9999);
                string finalUserName = baseUserName + randomNumber;

                bool exists = await _context.Users.AnyAsync(x =>
                    x.UserName == finalUserName);

                if (!exists)
                {
                    return finalUserName;
                }
            }

            return baseUserName + DateTime.Now.Ticks.ToString().Substring(10);
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return "Registered email";
            }

            int atIndex = email.IndexOf('@');

            if (atIndex <= 0)
            {
                return "Registered email";
            }

            string localPart = email.Substring(0, atIndex);
            string domain = email.Substring(atIndex);

            if (localPart.Length == 1)
            {
                return localPart[0] + "*****" + domain;
            }

            if (localPart.Length == 2)
            {
                return localPart[0] + "*****" +
                       localPart[1] + domain;
            }

            return localPart[0] +
                   new string('*', Math.Min(localPart.Length - 2, 8)) +
                   localPart[^1] +
                   domain;
        }
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