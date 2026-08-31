using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using UserManagementMvc.Models;
using UserManagementMvc.Models.ViewModels;
using UserManagementMvc.Services;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers
{
    public class EmailOtpController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailOtpController> _logger;

        public EmailOtpController(
            AppDbContext context,
            IEmailService emailService,
            ILogger<EmailOtpController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        // OTP generate
        // 6 digit secure OTP banata hai

        private string GenerateOtp()
        {
            return RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();
        }

        
        // OTP ko hash karke database me save karne ke liye
        // Plain OTP database me save nahi hoga
        
        private string HashOtp(
            User user,
            string otp)
        {
            var hasher = new PasswordHasher<User>();

            return hasher.HashPassword(
                user,
                otp);
        }

        
        // OTP verify karne ke liye
        
        private bool VerifyOtpHash(
            User user,
            string otpHash,
            string enteredOtp)
        {
            var hasher = new PasswordHasher<User>();

            var result = hasher.VerifyHashedPassword(
                user,
                otpHash,
                enteredOtp);

            return result !=
                   PasswordVerificationResult.Failed;
        }

        
        // Naya OTP create karke email par send karta hai
        // Registration aur PasswordReset dono ke liye use hoga
        
        private async Task SendOtpAsync(
            User user,
            string purpose)
        {
            // Is user ke same purpose ke purane unused OTP close kar do
            var oldOtps = await _context.Emailotps
                .Where(x =>
                    x.UserId == user.Id &&
                    x.Purpose == purpose &&
                    x.IsUsed == false)
                .ToListAsync();

            foreach (var oldOtp in oldOtps)
            {
                oldOtp.IsUsed = true;
            }

            // Secure 6 digit OTP
            string otp = GenerateOtp();

            var otpRecord = new Emailotp
            {
                UserId = user.Id,
                Email = user.Email,
                Purpose = purpose,

                // OTP 5 minute tak valid rahega
                ExpiresAt = DateTime.Now.AddMinutes(5),

                CreatedAt = DateTime.Now,

                IsUsed = false,

                AttemptCount = 0
            };

            // Plain OTP nahi, uska hash save hoga
            otpRecord.OtpHash =
                HashOtp(user, otp);

            _context.Emailotps.Add(otpRecord);

            await _context.SaveChangesAsync();

            string subject;

            if (purpose == "Registration")
            {
                subject =
                    "Registration Email Verification";
            }
            else
            {
                subject =
                    "Password Reset Verification";
            }

            string safeName =
                WebUtility.HtmlEncode(user.Name);

            string body = $@"
                <div style='
                    font-family: Arial, sans-serif;
                    max-width: 600px;
                    margin: auto;
                    padding: 20px;'>

                    <h2>
                        {subject}
                    </h2>

                    <p>
                        Hello {safeName},
                    </p>

                    <p>
                        Your verification OTP is:
                    </p>

                    <div style='
                        font-size: 32px;
                        font-weight: bold;
                        letter-spacing: 8px;
                        margin: 25px 0;'>

                        {otp}

                    </div>

                    <p>
                        This OTP is valid for 5 minutes.
                    </p>

                    <p>
                        Do not share this OTP with anyone.
                    </p>

                    <hr />

                    <small>
                        User Management System
                    </small>

                </div>";

            await _emailService.SendEmailAsync(
                user.Email,
                subject,
                body);
        }


        // Registration ke baad yahan redirect karenge
        // OTP generate hoga aur registered email par jayega

        [HttpGet]
        public async Task<IActionResult> SendRegistrationOtp()
        {
            string? pendingJson =
                HttpContext.Session.GetString(
                    "PendingRegistration");

            if (string.IsNullOrWhiteSpace(
                pendingJson))
            {
                TempData["Error"] =
                    "Registration session has expired. Please register again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            var pending =
                JsonSerializer.Deserialize<
                    PendingRegistrationViewModel>(
                        pendingJson);

            if (pending == null ||
                string.IsNullOrWhiteSpace(
                    pending.Email))
            {
                HttpContext.Session.Remove(
                    "PendingRegistration");

                TempData["Error"] =
                    "Registration information could not be found. Please register again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            try
            {
                string otp =
                    GenerateOtp();

                var temporaryUser =
                    new User
                    {
                        Name = pending.Name,
                        Email = pending.Email,
                        PasswordHash = pending.PasswordHash
                    };

                string otpHash =
                    HashOtp(
                        temporaryUser,
                        otp);

                HttpContext.Session.SetString(
                    "RegistrationOtpHash",
                    otpHash);

                HttpContext.Session.SetString(
                    "RegistrationOtpEmail",
                    pending.Email);

                HttpContext.Session.SetString(
                    "RegistrationOtpExpiresAt",
                    DateTime.Now
                        .AddMinutes(5)
                        .ToString("O"));

                HttpContext.Session.SetInt32(
                    "RegistrationOtpAttempts",
                    0);

                string safeName =
                    WebUtility.HtmlEncode(
                        pending.Name);

                string body = $@"
            <div style='
                font-family: Arial, sans-serif;
                max-width: 600px;
                margin: auto;
                padding: 20px;'>

                <h2>
                    Registration Email Verification
                </h2>

                <p>
                    Hello {safeName},
                </p>

                <p>
                    Your registration verification OTP is:
                </p>

                <div style='
                    font-size: 32px;
                    font-weight: bold;
                    letter-spacing: 8px;
                    margin: 25px 0;'>

                    {otp}

                </div>

                <p>
                    This OTP is valid for 5 minutes.
                </p>

                <p>
                    Do not share this OTP with anyone.
                </p>

                <hr />

                <small>
                    User Management System
                </small>

            </div>";

                await _emailService.SendEmailAsync(
                    pending.Email,
                    "Registration Email Verification",
                    body);

                TempData["Success"] =
                    "A 6-digit verification OTP has been sent to your email address.";

                return RedirectToAction(
                    "Verify",
                    new
                    {
                        userId = 0,
                        purpose = "Registration"
                    });
            }
            catch
            {
                TempData["Error"] =
                    "Verification OTP could not be sent. Please try again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }
        }

        // Password reset ke liye OTP send
        // Forgot Password se is action par redirect karenge

        [HttpGet]
        public async Task<IActionResult> SendPasswordResetOtp(
    int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == userId &&
                    x.IsDeleted == false &&
                    x.IsActive == true);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(
                    "ForgotPassword",
                    "UserAuth");
            }

            try
            {
                await SendOtpAsync(
                    user,
                    "PasswordReset");

                TempData["Success"] =
                    "OTP has been sent to your registered email.";

                return RedirectToAction(
                    "Verify",
                    new
                    {
                        userId = user.Id,
                        purpose = "PasswordReset"
                    });
            }
            catch (Exception ex)
            {
                // IMPORTANT:
                // Actual email/SMTP error server console/log me milega.
                _logger.LogError(
                    ex,
                    "Password reset OTP could not be sent for UserId {UserId}",
                    user.Id);

                TempData["Error"] =
                    "OTP could not be sent to your email. " +
                    "Please check the email configuration or try again.";

                // IMPORTANT:
                // Ab user ko ID search page par nahi bhejenge.
                // Wapas Password Recovery method page par bhejenge.

                return View(
                    "~/Views/Auth/ForgotPasswordMethod.cshtml",
                    new ForgotPasswordMethodViewModel
                    {
                        UserId = user.Id,
                        MaskedEmail = MaskEmailForRecovery(user.Email)
                    });
            }
        }


        // OTP enter karne wala page
        // Verify.cshtml Views/Auth folder ke andar hai

        [HttpGet]
        public async Task<IActionResult> Verify(
    int userId,
    string purpose)
        {
            if (purpose == "Registration")
            {
                string? pendingJson =
                    HttpContext.Session.GetString(
                        "PendingRegistration");

                string? otpHash =
                    HttpContext.Session.GetString(
                        "RegistrationOtpHash");

                if (string.IsNullOrWhiteSpace(
                        pendingJson) ||
                    string.IsNullOrWhiteSpace(
                        otpHash))
                {
                    TempData["Error"] =
                        "Registration verification session has expired. Please register again.";

                    return RedirectToAction(
                        "Register",
                        "UserAuth");
                }

                var model =
                    new VerifyOtpViewModel
                    {
                        UserId = 0,
                        Purpose = "Registration"
                    };

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            if (purpose != "PasswordReset")
            {
                TempData["Error"] =
                    "Invalid verification request.";

                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            var user =
                await _context.Users
                    .FirstOrDefaultAsync(x =>
                        x.Id == userId &&
                        x.IsDeleted == false);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            var resetModel =
                new VerifyOtpViewModel
                {
                    UserId = user.Id,
                    Purpose = "PasswordReset"
                };

            return View(
                "~/Views/Auth/VerifyOtp.cshtml",
                resetModel);
        }


        // User ne jo OTP enter kiya hai use verify karta hai

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(
            VerifyOtpViewModel model)
        {
            if (model.Purpose == "Registration")
            {
                return await VerifyRegistrationOtp(
                    model);
            }
            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            string purpose =
                model.Purpose?.Trim() ?? "";

            if (purpose != "Registration" &&
                purpose != "PasswordReset")
            {
                ViewBag.Error =
                    "Invalid verification request.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == model.UserId &&
                    x.IsDeleted == false);

            if (user == null)
            {
                ViewBag.Error =
                    "User account was not found.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            var otpRecord = await _context.Emailotps
                .Where(x =>
                    x.UserId == user.Id &&
                    x.Purpose == purpose &&
                    x.IsUsed == false)
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                ViewBag.Error =
                    "No active OTP was found. Please request a new OTP.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            // OTP expire ho gaya
            if (otpRecord.ExpiresAt < DateTime.Now)
            {
                otpRecord.IsUsed = true;

                await _context.SaveChangesAsync();

                ViewBag.Error =
                    "OTP has expired. Please request a new OTP.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            // Ek OTP par maximum 5 wrong attempts
            if (otpRecord.AttemptCount >= 5)
            {
                otpRecord.IsUsed = true;

                await _context.SaveChangesAsync();

                ViewBag.Error =
                    "Too many incorrect OTP attempts. Please request a new OTP.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            bool isCorrect = VerifyOtpHash(
                user,
                otpRecord.OtpHash,
                model.Otp.Trim());

            if (!isCorrect)
            {
                otpRecord.AttemptCount++;

                if (otpRecord.AttemptCount >= 5)
                {
                    otpRecord.IsUsed = true;
                }

                await _context.SaveChangesAsync();

                int attemptsLeft = 5 - otpRecord.AttemptCount;

                ModelState.Remove("Otp");
                model.Otp = "";

                if (attemptsLeft > 0)
                {
                    ViewBag.Error =
                        $"The OTP you entered is incorrect. You have {attemptsLeft} attempt(s) remaining.";
                }
                else
                {
                    ViewBag.Error =
                        "This OTP has been disabled after 5 incorrect attempts. Please request a new OTP.";
                }

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            // Correct OTP ko dobara use nahi kar sakte
            otpRecord.IsUsed = true;

        
            // Registration verification
        
            if (purpose == "Registration")
            {
                user.IsActive = true;

                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Email verified successfully. Your username is {user.UserName}. Please login.";

                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

        
            // Forgot password OTP verification
           
            if (purpose == "PasswordReset")
            {
                await _context.SaveChangesAsync();

                // Password reset page sirf verified OTP ke baad open hoga
                HttpContext.Session.SetInt32(
                    "OtpResetUserId",
                    user.Id);

                HttpContext.Session.SetString(
                    "OtpResetVerified",
                    "true");

                return RedirectToAction(
                    "ResetPassword");
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Login",
                "UserAuth");
        }


        private async Task<IActionResult>
    VerifyRegistrationOtp(
        VerifyOtpViewModel model)
        {
            if (string.IsNullOrWhiteSpace(
                model.Otp))
            {
                ViewBag.Error =
                    "Please enter the 6-digit OTP.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            if (model.Otp.Length != 6 ||
                !model.Otp.All(char.IsDigit))
            {
                ModelState.Remove("Otp");

                model.Otp = "";

                ViewBag.Error =
                    "Please enter a valid 6-digit OTP.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            string? pendingJson =
                HttpContext.Session.GetString(
                    "PendingRegistration");

            string? otpHash =
                HttpContext.Session.GetString(
                    "RegistrationOtpHash");

            string? expiresAtText =
                HttpContext.Session.GetString(
                    "RegistrationOtpExpiresAt");

            if (string.IsNullOrWhiteSpace(
                    pendingJson) ||
                string.IsNullOrWhiteSpace(
                    otpHash) ||
                string.IsNullOrWhiteSpace(
                    expiresAtText))
            {
                TempData["Error"] =
                    "Registration verification session has expired. Please register again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            var pending =
                JsonSerializer.Deserialize<
                    PendingRegistrationViewModel>(
                        pendingJson);

            if (pending == null)
            {
                TempData["Error"] =
                    "Registration information could not be found. Please register again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            if (!DateTime.TryParse(
                    expiresAtText,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out DateTime expiresAt))
            {
                TempData["Error"] =
                    "Invalid OTP session. Please register again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            if (DateTime.Now > expiresAt)
            {
                HttpContext.Session.Remove(
                    "RegistrationOtpHash");

                ViewBag.Error =
                    "This OTP has expired. Please request a new OTP.";

                ModelState.Remove("Otp");

                model.Otp = "";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            int attempts =
                HttpContext.Session.GetInt32(
                    "RegistrationOtpAttempts") ?? 0;

            if (attempts >= 5)
            {
                ViewBag.Error =
                    "This OTP has been disabled after too many incorrect attempts. Please request a new OTP.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            var temporaryUser =
                new User
                {
                    Name = pending.Name,
                    Email = pending.Email,
                    PasswordHash = pending.PasswordHash
                };

            bool correct =
                VerifyOtpHash(
                    temporaryUser,
                    otpHash,
                    model.Otp.Trim());

            if (!correct)
            {
                attempts++;

                HttpContext.Session.SetInt32(
                    "RegistrationOtpAttempts",
                    attempts);

                int attemptsLeft =
                    5 - attempts;

                ModelState.Remove("Otp");

                model.Otp = "";

                if (attemptsLeft > 0)
                {
                    ViewBag.Error =
                        $"The OTP you entered is incorrect. You have {attemptsLeft} attempt(s) remaining.";
                }
                else
                {
                    HttpContext.Session.Remove(
                        "RegistrationOtpHash");

                    ViewBag.Error =
                        "This OTP has been disabled after 5 incorrect attempts. Please request a new OTP.";
                }

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            bool emailExists =
                await _context.Users.AnyAsync(x =>
                    x.Email == pending.Email);

            if (emailExists)
            {
                ClearRegistrationSession();

                TempData["Error"] =
                    "This email address is already registered.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            bool userNameExists =
                await _context.Users.AnyAsync(x =>
                    x.UserName == pending.UserName);

            if (userNameExists)
            {
                ClearRegistrationSession();

                TempData["Error"] =
                    "This username is no longer available. Please register again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            bool mobileExists =
                await _context.Users.AnyAsync(x =>
                    x.MobileCountryCode ==
                        pending.MobileCountryCode &&
                    x.Mobile ==
                        pending.Mobile);

            if (mobileExists)
            {
                ClearRegistrationSession();

                TempData["Error"] =
                    "This mobile number is already registered.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            var user =
                new User
                {
                    Name =
                        pending.Name,

                    Email =
                        pending.Email,

                    Mobile =
                        pending.Mobile,

                    MobileCountryCode =
                        pending.MobileCountryCode,

                    UserName =
                        pending.UserName,

                    PasswordHash =
                        pending.PasswordHash,

                    Role =
                        "User",

                    Department =
                        string.IsNullOrWhiteSpace(
                            pending.Department)
                            ? "General"
                            : pending.Department,

                    DateOfBirth =
                        pending.DateOfBirth,

                    IsActive =
                        true,

                    IsDeleted =
                        false,

                    CreatedAt =
                        DateTime.Now,

                    UpdatedAt =
                        null,

                    FailedLoginAttempts =
                        0,

                    IsBlocked =
                        false,

                    BlockedAt =
                        null
                };

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync();

            try
            {
                _context.Users.Add(user);

                await _context.SaveChangesAsync();

                await SaveRegistrationSecurityQuestions(
                    user,
                    pending.SecurityQuestions);

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();

                ViewBag.Error =
                    "Registration could not be completed. Please try again.";

                return View(
                    "~/Views/Auth/VerifyOtp.cshtml",
                    model);
            }

            ClearRegistrationSession();

            TempData["Success"] =
                $"Email verified and registration completed successfully. Your username is {user.UserName}. Please login.";

            return RedirectToAction(
                "Login",
                "UserAuth");
        }

        // OTP resend

        [HttpPost]
        [ValidateAntiForgeryToken]


        public async Task<IActionResult> Resend(
            int userId,
            string purpose)
        {
            if (purpose == "Registration")
            {
                return await ResendRegistrationOtp();
            }
            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == userId &&
                    x.IsDeleted == false);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            if (purpose != "Registration" &&
                purpose != "PasswordReset")
            {
                TempData["Error"] =
                    "Invalid OTP request.";

                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            // Registration pehle hi verify ho chuka hai
            if (purpose == "Registration" &&
                user.IsActive == true)
            {
                TempData["Success"] =
                    "Your email is already verified.";

                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            try
            {
                await SendOtpAsync(
                    user,
                    purpose);

                TempData["Success"] =
                    "A new OTP has been sent to your email.";
            }
            catch
            {
                TempData["Error"] =
                    "OTP could not be sent. Please try again.";
            }

            return RedirectToAction(
                "Verify",
                new
                {
                    userId = user.Id,
                    purpose = purpose
                });
        }

        private async Task<IActionResult>
    ResendRegistrationOtp()
        {
            string? pendingJson =
                HttpContext.Session.GetString(
                    "PendingRegistration");

            if (string.IsNullOrWhiteSpace(
                pendingJson))
            {
                TempData["Error"] =
                    "Registration session has expired. Please register again.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            var pending =
                JsonSerializer.Deserialize<
                    PendingRegistrationViewModel>(
                        pendingJson);

            if (pending == null)
            {
                TempData["Error"] =
                    "Registration information could not be found.";

                return RedirectToAction(
                    "Register",
                    "UserAuth");
            }

            try
            {
                string otp =
                    GenerateOtp();

                var temporaryUser =
                    new User
                    {
                        Name = pending.Name,
                        Email = pending.Email,
                        PasswordHash = pending.PasswordHash
                    };

                string otpHash =
                    HashOtp(
                        temporaryUser,
                        otp);

                HttpContext.Session.SetString(
                    "RegistrationOtpHash",
                    otpHash);

                HttpContext.Session.SetString(
                    "RegistrationOtpExpiresAt",
                    DateTime.Now
                        .AddMinutes(5)
                        .ToString("O"));

                HttpContext.Session.SetInt32(
                    "RegistrationOtpAttempts",
                    0);

                string safeName =
                    WebUtility.HtmlEncode(
                        pending.Name);

                string body = $@"
            <div style='
                font-family:Arial,sans-serif;
                max-width:600px;
                margin:auto;
                padding:20px;'>

                <h2>
                    Registration Email Verification
                </h2>

                <p>
                    Hello {safeName},
                </p>

                <p>
                    Your new verification OTP is:
                </p>

                <div style='
                    font-size:32px;
                    font-weight:bold;
                    letter-spacing:8px;
                    margin:25px 0;'>

                    {otp}

                </div>

                <p>
                    This OTP is valid for 5 minutes.
                </p>

                <p>
                    Your previous OTP is no longer valid.
                </p>

                <hr />

                <small>
                    User Management System
                </small>

            </div>";

                await _emailService.SendEmailAsync(
                    pending.Email,
                    "New Registration Verification OTP",
                    body);

                TempData["Success"] =
                    "A new OTP has been sent to your email address.";

                return RedirectToAction(
                    "Verify",
                    new
                    {
                        userId = 0,
                        purpose = "Registration"
                    });
            }
            catch
            {
                TempData["Error"] =
                    "OTP could not be sent. Please try again.";

                return RedirectToAction(
                    "Verify",
                    new
                    {
                        userId = 0,
                        purpose = "Registration"
                    });
            }
        }


        private async Task SaveRegistrationSecurityQuestions(
    User user,
    List<RegisterSecurityQuestionViewModel> questions)
        {
            var hasher =
                new PasswordHasher<User>();

            var selectedMasterIds =
                questions
                    .Where(x => x.SecurityQuestionMasterId > 0)
                    .Select(x => x.SecurityQuestionMasterId!.Value)
                    .ToList();

            var masterQuestions =
                await _context.SecurityQuestionMasters
                    .Where(x => selectedMasterIds.Contains(x.Id))
                    .ToDictionaryAsync(
                        x => x.Id,
                        x => x.QuestionText);

            foreach (var item in questions)
            {
                var newQuestion =
                    new Usersecurityquestion
                    {
                        UserId = user.Id,

                        SecurityAnswerHash =
                            hasher.HashPassword(
                                user,
                                item.Answer.Trim().ToLower()),

                        CreatedAt = DateTime.Now
                    };

                if (item.SecurityQuestionMasterId == 0)
                {
                    newQuestion.SecurityQuestionMasterId =
                        null;

                    newQuestion.QuestionText =
                        item.QuestionText?.Trim();
                }
                else
                {
                    newQuestion.SecurityQuestionMasterId =
                        item.SecurityQuestionMasterId;

                    if (masterQuestions.TryGetValue(
                        item.SecurityQuestionMasterId!.Value,
                        out string? questionText))
                    {
                        newQuestion.QuestionText =
                            questionText;
                    }
                    else
                    {
                        newQuestion.QuestionText =
                            "Security question";
                    }
                }

                _context.UserSecurityQuestions.Add(
                    newQuestion);
            }

            user.SecurityQuestion = null;
            user.SecurityAnswerHash = null;
        }

        private void ClearRegistrationSession()
        {
            HttpContext.Session.Remove(
                "PendingRegistration");

            HttpContext.Session.Remove(
                "RegistrationOtpHash");

            HttpContext.Session.Remove(
                "RegistrationOtpEmail");

            HttpContext.Session.Remove(
                "RegistrationOtpExpiresAt");

            HttpContext.Session.Remove(
                "RegistrationOtpAttempts");
        }

        // OTP verify hone ke baad password reset page

        [HttpGet]
        public IActionResult ResetPassword()
        {
            int? userId =
                HttpContext.Session.GetInt32(
                    "OtpResetUserId");

            string? verified =
                HttpContext.Session.GetString(
                    "OtpResetVerified");

            if (userId == null ||
                verified != "true")
            {
                TempData["Error"] =
                    "Please verify the OTP before resetting your password.";

                return RedirectToAction(
                    "ForgotPassword",
                    "UserAuth");
            }

            var model =
                new ResetPasswordOtpViewModel
                {
                    UserId = userId.Value
                };

            return View(
                "~/Views/Auth/ResetPasswordOtp.cshtml",
                model);
        }

        
        // New password database me save
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordOtpViewModel model)
        {
            int? verifiedUserId =
                HttpContext.Session.GetInt32(
                    "OtpResetUserId");

            string? verified =
                HttpContext.Session.GetString(
                    "OtpResetVerified");

            if (verifiedUserId == null ||
                verified != "true" ||
                verifiedUserId.Value != model.UserId)
            {
                TempData["Error"] =
                    "Password reset verification has expired.";

                return RedirectToAction(
                    "ForgotPassword",
                    "UserAuth");
            }

            if (string.IsNullOrWhiteSpace(
                model.NewPassword))
            {
                ViewBag.Error =
                    "New password is required.";

                return View(
                    "~/Views/Auth/ResetPasswordOtp.cshtml",
                    model);
            }

            if (model.NewPassword !=
                model.ConfirmPassword)
            {
                ViewBag.Error =
                    "New password and confirm password do not match.";

                return View(
                    "~/Views/Auth/ResetPasswordOtp.cshtml",
                    model);
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x =>
                    x.Id == model.UserId &&
                    x.IsDeleted == false &&
                    x.IsActive == true);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(
                    "ForgotPassword",
                    "UserAuth");
            }

            var hasher =
                new PasswordHasher<User>();

            user.PasswordHash =
                hasher.HashPassword(
                    user,
                    model.NewPassword);

            user.UpdatedAt =
                DateTime.Now;

            // Successful reset ke baad failed attempts clear
            user.FailedLoginAttempts = 0;

            await _context.SaveChangesAsync();

            // Reset permission ko dobara use nahi kar sakte
            HttpContext.Session.Remove(
                "OtpResetUserId");

            HttpContext.Session.Remove(
                "OtpResetVerified");

            TempData["Success"] =
                "Password reset successfully. Please login with your new password.";

            return RedirectToAction(
                "Login",
                "UserAuth");
        }
        private string MaskEmailForRecovery(string email)
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

            string localPart =
                email.Substring(0, atIndex);

            string domain =
                email.Substring(atIndex);

            if (localPart.Length == 1)
            {
                return localPart + "*****" + domain;
            }

            if (localPart.Length == 2)
            {
                return localPart[0] +
                       "*****" +
                       localPart[1] +
                       domain;
            }

            return localPart[0] +
                   new string(
                       '*',
                       Math.Min(
                           localPart.Length - 2,
                           8)) +
                   localPart[^1] +
                   domain;
        }
    }
}