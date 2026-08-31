using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Controllers
{
    public class SiteContentsController : Controller
    {
        private readonly AppDbContext _context;

        public SiteContentsController(AppDbContext context)
        {
            _context = context;
        }

        // Sirf SuperAdmin site content manage kar sakta hai
        private bool IsSuperAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "SuperAdmin";
        }

        // Ab Site Content me sirf Home page fixed content allowed hai
        private bool IsAllowedContentKey(string contentKey)
        {
            return contentKey == "HomeTitle"
                || contentKey == "HomeSubtitle"
                || contentKey == "HomeFooterNote";
        }

       
        // Manage Site Content List
       
        public async Task<IActionResult> Index()
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            var contents = await _context.Sitecontents
                .Where(x =>
                    x.ContentKey == "HomeTitle" ||
                    x.ContentKey == "HomeSubtitle" ||
                    x.ContentKey == "HomeFooterNote")
                .OrderBy(x => x.Id)
                .ToListAsync();

            return View(contents);
        }

       
        // Edit Page
       
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            var content = await _context.Sitecontents.FindAsync(id);

            if (content == null)
                return NotFound();

            if (!IsAllowedContentKey(content.ContentKey))
                return RedirectToAction("Index");

            return View(content);
        }

       
        // Update Site Content
       
        [HttpPost]
        public async Task<IActionResult> Edit(Sitecontent model)
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            var content = await _context.Sitecontents.FindAsync(model.Id);

            if (content == null)
                return NotFound();

            if (!IsAllowedContentKey(content.ContentKey))
                return RedirectToAction("Index");

            // HomeTitle ke liye sirf Title field use hoti hai
            if (content.ContentKey == "HomeTitle")
            {
                if (string.IsNullOrWhiteSpace(model.Title))
                {
                    ViewBag.Error = "Home title is required.";
                    return View(content);
                }

                content.Title = model.Title.Trim();
                content.Content = null;
            }

            // HomeSubtitle ke liye sirf Content field use hoti hai
            else if (content.ContentKey == "HomeSubtitle")
            {
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    ViewBag.Error = "Home subtitle is required.";
                    return View(content);
                }

                content.Title = null;
                content.Content = model.Content.Trim();
            }

            // HomeFooterNote ke liye sirf Content field use hoti hai
            else if (content.ContentKey == "HomeFooterNote")
            {
                if (string.IsNullOrWhiteSpace(model.Content))
                {
                    ViewBag.Error = "Home footer note is required.";
                    return View(content);
                }

                content.Title = null;
                content.Content = model.Content.Trim();
            }

            content.IsActive = model.IsActive ?? true;
            content.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}