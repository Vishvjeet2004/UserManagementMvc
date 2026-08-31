using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Controllers
{
    public class EmojiManagementController : Controller
    {
        private readonly AppDbContext _context;

        public EmojiManagementController(AppDbContext context)
        {
            _context = context;
        }

        // Displays the emoji management page.
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!IsSuperAdmin())
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var emojis = await _context.ChatEmojis
                .AsNoTracking()
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return View(emojis);
        }

        // Returns all active emojis for the messenger.
        [HttpGet]
        public async Task<IActionResult> GetActiveEmojis()
        {
            if (!IsLoggedIn())
            {
                return Unauthorized();
            }

            var emojis = await _context.ChatEmojis
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Emoji,
                    x.SortOrder
                })
                .ToListAsync();

            return Json(emojis);
        }

        // Adds a new emoji to the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEmoji(
            string? emoji,
            int sortOrder = 0)
        {
            if (!IsSuperAdmin())
            {
                return Json(new
                {
                    success = false,
                    message = "You are not authorized to manage emojis."
                });
            }

            if (string.IsNullOrWhiteSpace(emoji))
            {
                return Json(new
                {
                    success = false,
                    message = "Please enter an emoji."
                });
            }

            emoji = emoji.Trim();

            if (emoji.Length > 20)
            {
                return Json(new
                {
                    success = false,
                    message = "Emoji value is too long."
                });
            }

            var exists = await _context.ChatEmojis
                .AnyAsync(x => x.Emoji == emoji);

            if (exists)
            {
                return Json(new
                {
                    success = false,
                    message = "This emoji already exists."
                });
            }

            var item = new ChatEmoji
            {
                Emoji = emoji,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.ChatEmojis.Add(item);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Emoji added successfully.",
                id = item.Id,
                emoji = item.Emoji,
                sortOrder = item.SortOrder
            });
        }

        // Toggles the active status of an emoji.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEmoji(int id)
        {
            if (!IsSuperAdmin())
            {
                return Json(new
                {
                    success = false,
                    message = "You are not authorized to manage emojis."
                });
            }

            var emoji = await _context.ChatEmojis
                .FirstOrDefaultAsync(x => x.Id == id);

            if (emoji == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Emoji not found."
                });
            }

            emoji.IsActive = !emoji.IsActive;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isActive = emoji.IsActive
            });
        }

        // Deletes an emoji from the database.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmoji(int id)
        {
            if (!IsSuperAdmin())
            {
                return Json(new
                {
                    success = false,
                    message = "You are not authorized to manage emojis."
                });
            }

            var emoji = await _context.ChatEmojis
                .FirstOrDefaultAsync(x => x.Id == id);

            if (emoji == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Emoji not found."
                });
            }

            _context.ChatEmojis.Remove(emoji);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Emoji deleted successfully."
            });
        }

        // Checks whether the current user is logged in.
        private bool IsLoggedIn()
        {
            return HttpContext.Session
                .GetInt32("UserId")
                .HasValue;
        }

        // Checks whether the current user is a SuperAdmin.
        private bool IsSuperAdmin()
        {
            var userId = HttpContext.Session
                .GetInt32("UserId");

            var role = HttpContext.Session
                .GetString("UserRole");

            if (string.IsNullOrWhiteSpace(role))
            {
                role = HttpContext.Session
                    .GetString("Role");
            }

            return userId.HasValue &&
                   string.Equals(
                       role?.Trim(),
                       "SuperAdmin",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}