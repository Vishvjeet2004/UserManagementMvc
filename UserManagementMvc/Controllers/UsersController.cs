using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using UserManagementMvc.Models;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers
{
    public class UsersController : Controller
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // Check karta hai ki koi user login hai ya nahi
        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetInt32("UserId") != null;
        }

        // Admin aur SuperAdmin dono ke liye common check
        private bool IsAdmin()
        {
            string? role = HttpContext.Session.GetString("UserRole");

            return role == "Admin" || role == "SuperAdmin";
        }

        // Sirf SuperAdmin ke liye
        private bool IsSuperAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "SuperAdmin";
        }

        // Login user ki Id session se milti hai
        private int? CurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId");
        }

        // Current logged-in user kisi target user ko manage kar sakta hai ya nahi
        private bool CanManageTargetUser(User targetUser)
        {
            string? currentRole =
                HttpContext.Session.GetString("UserRole");

            if (targetUser == null)
            {
                return false;
            }

            // SuperAdmin sabhi users ko manage kar sakta hai
            if (currentRole == "SuperAdmin")
            {
                return true;
            }

            // Admin kisi SuperAdmin ko manage nahi karega
            if (currentRole == "Admin" &&
                targetUser.Role == "SuperAdmin")
            {
                return false;
            }

            // Admin User aur Admin accounts ko manage kar sakta hai
            if (currentRole == "Admin")
            {
                return true;
            }

            return false;
        }

        // Country code ke hisab se mobile number length
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
        private bool IsValidMobileNumber(string countryCode, string mobile)
        {
            if (string.IsNullOrWhiteSpace(mobile))
                return false;

            return countryCode switch
            {
                // India
                "+91" => Regex.IsMatch(mobile, @"^[6-9][0-9]{9}$"),

                // USA / Canada
                "+1" => Regex.IsMatch(mobile, @"^[2-9][0-9]{9}$"),

                // UK
                "+44" => Regex.IsMatch(mobile, @"^7[0-9]{9}$"),

                // Nepal
                "+977" => Regex.IsMatch(mobile, @"^(97|98)[0-9]{8}$"),

                // UAE
                "+971" => Regex.IsMatch(mobile, @"^5[024568][0-9]{7}$"),

                // Pakistan
                "+92" => Regex.IsMatch(mobile, @"^3[0-9]{9}$"),

                // Bangladesh
                "+880" => Regex.IsMatch(mobile, @"^1[3-9][0-9]{8}$"),

                null => false
            };
        }

        // Username ko simple aur database-friendly format me convert karta hai
        private string CleanUserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "user";
            }

            string cleanedUserName =
                value.Trim().ToLower();

            // Space aur special characters hata rahe hain
            cleanedUserName = Regex.Replace(
                cleanedUserName,
                @"[^a-z0-9]",
                ""
            );

            if (string.IsNullOrWhiteSpace(cleanedUserName))
            {
                cleanedUserName = "user";
            }

            // Username bahut lamba na ho
            if (cleanedUserName.Length > 20)
            {
                cleanedUserName =
                    cleanedUserName.Substring(0, 20);
            }

            return cleanedUserName;
        }

        // Name se related unique username banata hai
        private async Task<string> GenerateUniqueUserName(
            string name)
        {
            string baseUserName = CleanUserName(name);

            bool baseUserNameExists =
                await _context.Users.AnyAsync(x =>
                    x.UserName == baseUserName
                );

            // Agar base username available hai to wahi return hoga
            if (!baseUserNameExists)
            {
                return baseUserName;
            }

            // Duplicate hone par random number add hoga
            for (int attempt = 1; attempt <= 100; attempt++)
            {
                int randomNumber =
                    Random.Shared.Next(100, 9999);

                string finalUserName =
                    baseUserName + randomNumber;

                bool exists =
                    await _context.Users.AnyAsync(x =>
                        x.UserName == finalUserName
                    );

                if (!exists)
                {
                    return finalUserName;
                }
            }

            // Rare case ke liye final backup
            return baseUserName +
                   DateTime.Now.Ticks
                       .ToString()
                       .Substring(10);
        }

        
        // User List / My Profile
        // Admin ko sab users aur normal user ko apni profile
        
        public async Task<IActionResult> Index(
            string? search,
            string? roleFilter,
            string? departmentFilter,
            string? statusFilter,
            int page = 1)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            int? loggedInUserId = CurrentUserId();
            int pageSize = 5;

            IQueryable<User> query =
                _context.Users.Where(x =>
                    x.IsDeleted == false
                );

            // Normal user ko sirf apni profile dikhegi
            if (!IsAdmin())
            {
                query = query.Where(x =>
                    x.Id == loggedInUserId
                );
            }

            // Search by name, email, mobile or username
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    (x.Name != null &&
                     x.Name.Contains(search)) ||

                    (x.Email != null &&
                     x.Email.Contains(search)) ||

                    (x.Mobile != null &&
                     x.Mobile.Contains(search)) ||

                    (x.UserName != null &&
                     x.UserName.Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                query = query.Where(x =>
                    x.Role == roleFilter
                );
            }

            if (!string.IsNullOrWhiteSpace(departmentFilter))
            {
                query = query.Where(x =>
                    x.Department == departmentFilter
                );
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                bool isActive =
                    statusFilter == "Active";

                query = query.Where(x =>
                    x.IsActive == isActive
                );
            }

            int totalUsers = await query.CountAsync();

            int totalPages = (int)Math.Ceiling(
                totalUsers / (double)pageSize
            );

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var users = await query
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.IsAdmin = IsAdmin();
            ViewBag.IsSuperAdmin = IsSuperAdmin();

            ViewBag.Search = search;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.DepartmentFilter = departmentFilter;
            ViewBag.StatusFilter = statusFilter;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        
        // Add User Page
        // Admin aur SuperAdmin dono page open kar sakte hain
        
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction("Index");
            }

            // View me role dropdown show/hide karne ke liye
            ViewBag.IsSuperAdmin = IsSuperAdmin();

            return View();
        }

        
        // Save New User
        // Role sirf SuperAdmin decide karega
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            User user,
            string Password)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction("Index");
            }

            // Validation fail hone par bhi role section sahi dikhe
            ViewBag.IsSuperAdmin = IsSuperAdmin();

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                ViewBag.Error = "Name is required.";
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ViewBag.Error = "Email is required.";
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(user.Mobile))
            {
                ViewBag.Error = "Mobile number is required.";
                return View(user);
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ViewBag.Error = "Password is required.";
                return View(user);
            }

            user.Name = user.Name.Trim();
            user.Email = user.Email.Trim().ToLower();
            user.Mobile = user.Mobile.Trim();

            // Sirf SuperAdmin role decide kar sakta hai
            if (IsSuperAdmin())
            {
                if (string.IsNullOrWhiteSpace(user.Role))
                {
                    user.Role = "User";
                }

                string[] allowedRoles =
                {
                    "User",
                    "Admin",
                    "SuperAdmin"
                };

                if (!allowedRoles.Contains(user.Role))
                {
                    ViewBag.Error = "Selected role is not valid.";
                    return View(user);
                }
            }
            else
            {
                // Admin ke create kiye account ka role hamesha User hoga
                user.Role = "User";
            }

            user.MobileCountryCode =
                string.IsNullOrWhiteSpace(
                    user.MobileCountryCode
                )
                ? "+91"
                : user.MobileCountryCode.Trim();

            int requiredLength =
                GetMobileLength(user.MobileCountryCode);

            if (user.Mobile.Length != requiredLength)
            {
                ViewBag.Error =
                    $"Mobile number must be {requiredLength} digits.";

                return View(user);
            }

            if (!IsValidMobileNumber(user.MobileCountryCode, user.Mobile))
            {
                ViewBag.Error = "Invalid mobile number for selected country.";

                return View(user);
            }


            bool emailExists =
                await _context.Users.AnyAsync(x =>
                    x.Email == user.Email
                );

            if (emailExists)
            {
                ViewBag.Error = "Email already exists.";
                return View(user);
            }

            bool mobileExists =
                await _context.Users.AnyAsync(x =>
                    x.MobileCountryCode ==
                    user.MobileCountryCode &&

                    x.Mobile == user.Mobile
                );

            if (mobileExists)
            {
                ViewBag.Error =
                    "Mobile number already exists.";

                return View(user);
            }

            // Username blank hai to name se unique username banega
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
                        x.UserName == user.UserName
                    );

                if (userNameExists)
                {
                    ViewBag.Error =
                        "Username already exists.";

                    return View(user);
                }
            }

            // Plain password database me save nahi karna
            var hasher = new PasswordHasher<User>();

            user.PasswordHash =
                hasher.HashPassword(user, Password);

            user.Department =
                string.IsNullOrWhiteSpace(user.Department)
                ? "General"
                : user.Department.Trim();

            user.IsDeleted = false;
            user.IsActive = true;
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = null;

            // Naya account starting me blocked nahi hoga
            user.FailedLoginAttempts = 0;
            user.IsBlocked = false;
            user.BlockedAt = null;
            user.UnblockedAt = null;
            user.UnblockedBy = null;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"User added successfully. Username: {user.UserName}";

            return RedirectToAction("Index");
        }

        
        // Edit Profile Page
        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            int? loggedInUserId = CurrentUserId();

            // Normal user sirf apni profile edit karega
            if (!IsAdmin() && loggedInUserId != id)
            {
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Admin SuperAdmin ko edit nahi kar sakta
            if (IsAdmin() &&
                !CanManageTargetUser(user) &&
                user.Id != loggedInUserId)
            {
                TempData["Error"] =
                    "Admin cannot edit SuperAdmin.";

                return RedirectToAction("Index");
            }

            ViewBag.IsSuperAdmin = IsSuperAdmin();

            await LoadSecurityQuestionData(
                user.Id,
                user
            );

            return View(user);
        }

        
        // Update Profile
        // Role update sirf SuperAdmin karega
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            User user,
            string? SecurityAnswer,
            List<RegisterSecurityQuestionViewModel>?
                SecurityQuestions)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            int? loggedInUserId = CurrentUserId();

            if (!IsAdmin() && loggedInUserId != user.Id)
            {
                return RedirectToAction("Index");
            }

            var existingUser =
                await _context.Users.FindAsync(user.Id);

            if (existingUser == null)
            {
                return NotFound();
            }

            if (IsAdmin() &&
                !CanManageTargetUser(existingUser) &&
                existingUser.Id != loggedInUserId)
            {
                TempData["Error"] =
                    "Admin cannot update SuperAdmin.";

                return RedirectToAction("Index");
            }

            ViewBag.IsSuperAdmin = IsSuperAdmin();

            if (string.IsNullOrWhiteSpace(user.Name))
            {
                ViewBag.Error = "Name is required.";

                await LoadSecurityQuestionData(
                    existingUser.Id,
                    existingUser
                );

                return View(existingUser);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ViewBag.Error = "Email is required.";

                await LoadSecurityQuestionData(
                    existingUser.Id,
                    existingUser
                );

                return View(existingUser);
            }

            if (string.IsNullOrWhiteSpace(user.Mobile))
            {
                ViewBag.Error =
                    "Mobile number is required.";

                await LoadSecurityQuestionData(
                    existingUser.Id,
                    existingUser
                );

                return View(existingUser);
            }

            string cleanEmail =
                user.Email.Trim().ToLower();

            string countryCode =
                string.IsNullOrWhiteSpace(
                    user.MobileCountryCode
                )
                ? "+91"
                : user.MobileCountryCode.Trim();

            string cleanMobile =
                user.Mobile.Trim();
            int requiredLength =
GetMobileLength(countryCode);

            if (cleanMobile.Length != requiredLength)
            {
                ViewBag.Error =
                $"Mobile number must be {requiredLength} digits.";

                await LoadSecurityQuestionData(existingUser.Id, existingUser);

                return View(existingUser);
            }
            if (!IsValidMobileNumber(countryCode, cleanMobile))
            {
                ViewBag.Error =
                "Invalid mobile number for selected country.";

                await LoadSecurityQuestionData(existingUser.Id, existingUser);

                return View(existingUser);
            }


            bool emailExists =
                await _context.Users.AnyAsync(x =>
                    x.Email == cleanEmail &&
                    x.Id != existingUser.Id
                );

            if (emailExists)
            {
                ViewBag.Error = "Email already exists.";

                await LoadSecurityQuestionData(
                    existingUser.Id,
                    existingUser
                );

                return View(existingUser);
            }

            bool mobileExists =
                await _context.Users.AnyAsync(x =>
                    x.MobileCountryCode == countryCode &&
                    x.Mobile == cleanMobile &&
                    x.Id != existingUser.Id
                );

            if (mobileExists)
            {
                ViewBag.Error =
                    "Mobile number already exists.";

                await LoadSecurityQuestionData(
                    existingUser.Id,
                    existingUser
                );

                return View(existingUser);
            }

            string finalUserName;

            if (string.IsNullOrWhiteSpace(user.UserName))
            {
                finalUserName =
                    await GenerateUniqueUserName(user.Name);
            }
            else
            {
                finalUserName =
                    CleanUserName(user.UserName);

                bool usernameExists =
                    await _context.Users.AnyAsync(x =>
                        x.UserName == finalUserName &&
                        x.Id != existingUser.Id
                    );

                if (usernameExists)
                {
                    ViewBag.Error =
                        "Username already exists.";

                    await LoadSecurityQuestionData(
                        existingUser.Id,
                        existingUser
                    );

                    return View(existingUser);
                }
            }

            existingUser.Name = user.Name.Trim();
            existingUser.UserName = finalUserName;
            existingUser.Email = cleanEmail;
            existingUser.MobileCountryCode = countryCode;
            existingUser.Mobile = cleanMobile;

            // Old single-answer field use ho raha ho to update hoga
            if (!string.IsNullOrWhiteSpace(SecurityAnswer))
            {
                var hasher =
                    new PasswordHasher<User>();

                existingUser.SecurityAnswerHash =
                    hasher.HashPassword(
                        existingUser,
                        SecurityAnswer
                            .Trim()
                            .ToLower()
                    );
            }

            if (IsSuperAdmin())
            {
                // Role sirf SuperAdmin change karega
                existingUser.Role =
                    string.IsNullOrWhiteSpace(user.Role)
                    ? existingUser.Role
                    : user.Role;

                existingUser.Department =
                    string.IsNullOrWhiteSpace(user.Department)
                    ? "General"
                    : user.Department.Trim();

                existingUser.IsActive = user.IsActive;
            }
            else if (IsAdmin())
            {
                // Admin role change nahi karega
                existingUser.Department =
                    string.IsNullOrWhiteSpace(user.Department)
                    ? existingUser.Department
                    : user.Department.Trim();

                existingUser.IsActive = user.IsActive;
            }

            existingUser.UpdatedAt = DateTime.Now;

            // Form me security question ka data aaya tabhi update hoga
            if (HasSecurityQuestionInput(SecurityQuestions))
            {
                string? securityQuestionError =
                    await ValidateSecurityQuestionUpdate(
                        SecurityQuestions!
                    );

                if (securityQuestionError != null)
                {
                    ViewBag.Error =
                        securityQuestionError;

                    await LoadSecurityQuestionData(
                        existingUser.Id,
                        existingUser
                    );

                    return View(existingUser);
                }

                await ReplaceUserSecurityQuestions(
                    existingUser,
                    SecurityQuestions!
                );
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "User details updated successfully.";

            return RedirectToAction("Index");
        }

        
        // Soft Delete
        
        public async Task<IActionResult> SoftDelete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction("Index");
            }

            int? loggedInUserId = CurrentUserId();

            if (loggedInUserId == id)
            {
                TempData["Error"] =
                    "You cannot delete your own account from here.";

                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return RedirectToAction("Index");
            }

            if (!CanManageTargetUser(user))
            {
                TempData["Error"] =
                    "Admin cannot delete SuperAdmin.";

                return RedirectToAction("Index");
            }

            user.IsDeleted = true;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "User moved to Trash.";

            return RedirectToAction("Index");
        }

        
        // Trash Page
        
        public async Task<IActionResult> Trash()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction("Index");
            }

            var users = await _context.Users
                .Where(x => x.IsDeleted == true)
                .OrderBy(x => x.Id)
                .ToListAsync();

            ViewBag.IsSuperAdmin = IsSuperAdmin();

            return View(users);
        }

        
        // Restore User
        
        public async Task<IActionResult> Restore(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return RedirectToAction("Trash");
            }

            if (!CanManageTargetUser(user))
            {
                TempData["Error"] =
                    "Admin cannot restore SuperAdmin.";

                return RedirectToAction("Trash");
            }

            user.IsDeleted = false;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "User restored successfully.";

            return RedirectToAction("Trash");
        }

        
        // Permanent Delete
        
        public async Task<IActionResult> PermanentDelete(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction("Index");
            }

            int? loggedInUserId = CurrentUserId();

            if (loggedInUserId == id)
            {
                TempData["Error"] =
                    "You cannot permanently delete your own account.";

                return RedirectToAction("Trash");
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return RedirectToAction("Trash");
            }

            if (!CanManageTargetUser(user))
            {
                TempData["Error"] =
                    "Admin cannot permanently delete SuperAdmin.";

                return RedirectToAction("Trash");
            }

            // User delete se pehle uske security questions remove honge
            var userSecurityQuestions =
                await _context.UserSecurityQuestions
                    .Where(x => x.UserId == user.Id)
                    .ToListAsync();

            _context.UserSecurityQuestions
                .RemoveRange(userSecurityQuestions);

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "User permanently deleted.";

            return RedirectToAction("Trash");
        }

        
        // Normal User Delete Own Account
        
        public async Task<IActionResult> DeleteMyAccount()
        {
            int? userId = CurrentUserId();

            if (userId == null)
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            string? role =
                HttpContext.Session.GetString("UserRole");

            if (role == "Admin" ||
                role == "SuperAdmin")
            {
                TempData["Error"] =
                    "Admin and SuperAdmin cannot delete their account here.";

                return RedirectToAction("Index");
            }

            var user =
                await _context.Users.FindAsync(userId);

            if (user != null)
            {
                user.IsDeleted = true;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            HttpContext.Session.Clear();

            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        
        // Blocked Accounts List
        // Sirf SuperAdmin ke liye
        
        [HttpGet]
        public async Task<IActionResult> BlockedAccounts()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsSuperAdmin())
            {
                TempData["Error"] =
                    "Only the Super Admin can manage blocked accounts.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            var blockedUsers = await _context.Users
                .Where(x =>
                    x.IsBlocked == true &&
                    x.Role != "SuperAdmin"
                )
                .OrderByDescending(x => x.BlockedAt)
                .ThenBy(x => x.Name)
                .ToListAsync();

            return View(blockedUsers);
        }

        
        // Unblock Account
        // Failed attempts bhi reset honge
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockAccount(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth"
                );
            }

            if (!IsSuperAdmin())
            {
                TempData["Error"] =
                    "Only the Super Admin can unblock an account.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                TempData["Error"] =
                    "User account was not found.";

                return RedirectToAction(
                    "BlockedAccounts"
                );
            }

            if (string.Equals(
                user.Role,
                "SuperAdmin",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] =
                    "Super Admin account cannot be managed here.";

                return RedirectToAction(
                    "BlockedAccounts"
                );
            }

            if (!user.IsBlocked)
            {
                TempData["Error"] =
                    "This account is not blocked.";

                return RedirectToAction(
                    "BlockedAccounts"
                );
            }

            int? currentSuperAdminId =
                HttpContext.Session.GetInt32("UserId");

            user.IsBlocked = false;
            user.FailedLoginAttempts = 0;
            user.BlockedAt = null;
            user.UnblockedAt = DateTime.Now;
            user.UnblockedBy = currentSuperAdminId;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{user.Name}'s account has been unblocked.";

            return RedirectToAction(
                "BlockedAccounts"
            );
        }

        
        // Edit page ke liye current security questions load
        
        private async Task LoadSecurityQuestionData(
            int userId,
            User user)
        {
            ViewBag.SecurityQuestionMasters =
                await _context.SecurityQuestionMasters
                    .Where(x => x.IsActive == true)
                    .OrderBy(x => x.QuestionText)
                    .ToListAsync();

            var currentQuestions =
                await _context.UserSecurityQuestions
                    .Where(x => x.UserId == userId)
                    .OrderBy(x => x.Id)
                    .ToListAsync();

            var questionTexts = new List<string>();

            foreach (var item in currentQuestions)
            {
                if (!string.IsNullOrWhiteSpace(
                    item.QuestionText))
                {
                    questionTexts.Add(
                        item.QuestionText
                    );
                }
                else if (item.SecurityQuestionMasterId != null)
                {
                    string? questionText =
                        await _context
                            .SecurityQuestionMasters
                            .Where(x =>
                                x.Id ==
                                item.SecurityQuestionMasterId
                            )
                            .Select(x => x.QuestionText)
                            .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(
                        questionText))
                    {
                        questionTexts.Add(questionText);
                    }
                }
            }

            // Purane accounts ke old single question ke liye
            if (questionTexts.Count == 0 &&
                !string.IsNullOrWhiteSpace(
                    user.SecurityQuestion))
            {
                questionTexts.Add(
                    user.SecurityQuestion
                );
            }

            ViewBag.CurrentSecurityQuestions =
                questionTexts;
        }

        // Security question form me kuch fill hai ya nahi
        private bool HasSecurityQuestionInput(
            List<RegisterSecurityQuestionViewModel>?
                questions)
        {
            if (questions == null)
            {
                return false;
            }

            return questions.Any(x =>
                x.SecurityQuestionMasterId != null ||

                !string.IsNullOrWhiteSpace(
                    x.QuestionText
                ) ||

                !string.IsNullOrWhiteSpace(
                    x.Answer
                )
            );
        }

        // Security questions ko save karne se pehle validation
        private async Task<string?>
            ValidateSecurityQuestionUpdate(
                List<RegisterSecurityQuestionViewModel>
                    questions)
        {
            if (questions == null ||
                questions.Count < 3)
            {
                return "Please select 3 security questions.";
            }

            foreach (var item in questions)
            {
                if (item.SecurityQuestionMasterId == null)
                {
                    return "Please select all 3 security questions.";
                }

                if (string.IsNullOrWhiteSpace(item.Answer))
                {
                    return "Please enter answer for all security questions.";
                }

                if (item.SecurityQuestionMasterId == 0 &&
                    string.IsNullOrWhiteSpace(
                        item.QuestionText))
                {
                    return "Please enter your custom security question.";
                }

                if (item.SecurityQuestionMasterId > 0)
                {
                    bool exists =
                        await _context
                            .SecurityQuestionMasters
                            .AnyAsync(x =>
                                x.Id ==
                                item.SecurityQuestionMasterId &&

                                x.IsActive == true
                            );

                    if (!exists)
                    {
                        return "Selected security question is not valid.";
                    }
                }
            }

            var masterIds = questions
                .Where(x =>
                    x.SecurityQuestionMasterId > 0
                )
                .Select(x =>
                    x.SecurityQuestionMasterId!.Value
                )
                .ToList();

            if (masterIds.Count !=
                masterIds.Distinct().Count())
            {
                return "Please select different security questions.";
            }

            var customQuestions = questions
                .Where(x =>
                    x.SecurityQuestionMasterId == 0
                )
                .Select(x =>
                    x.QuestionText!
                        .Trim()
                        .ToLower()
                )
                .ToList();

            if (customQuestions.Count !=
                customQuestions.Distinct().Count())
            {
                return "Please enter different custom security questions.";
            }

            return null;
        }

        // Purane questions remove karke naye questions save
        private async Task ReplaceUserSecurityQuestions(
            User user,
            List<RegisterSecurityQuestionViewModel>
                questions)
        {
            var oldQuestions =
                await _context.UserSecurityQuestions
                    .Where(x => x.UserId == user.Id)
                    .ToListAsync();

            _context.UserSecurityQuestions
                .RemoveRange(oldQuestions);

            var hasher =
                new PasswordHasher<User>();

            var selectedMasterIds = questions
                .Where(x =>
                    x.SecurityQuestionMasterId > 0
                )
                .Select(x =>
                    x.SecurityQuestionMasterId!.Value
                )
                .ToList();

            var masterQuestions =
                await _context.SecurityQuestionMasters
                    .Where(x =>
                        selectedMasterIds.Contains(x.Id)
                    )
                    .ToDictionaryAsync(
                        x => x.Id,
                        x => x.QuestionText
                    );

            foreach (var item in questions)
            {
                var newQuestion =
                    new Usersecurityquestion
                    {
                        UserId = user.Id,

                        SecurityAnswerHash =
                            hasher.HashPassword(
                                user,
                                item.Answer
                                    .Trim()
                                    .ToLower()
                            ),

                        CreatedAt = DateTime.Now
                    };

                if (item.SecurityQuestionMasterId == 0)
                {
                    newQuestion
                        .SecurityQuestionMasterId = null;

                    newQuestion.QuestionText =
                        item.QuestionText!.Trim();
                }
                else
                {
                    newQuestion
                        .SecurityQuestionMasterId =
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

                _context.UserSecurityQuestions
                    .Add(newQuestion);
            }

            // Old single-question columns clear kar rahe hain
            user.SecurityQuestion = null;
            user.SecurityAnswerHash = null;
        }
    }
}