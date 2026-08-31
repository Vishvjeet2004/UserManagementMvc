using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using UserManagementMvc.Models;

namespace UserManagementMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // Home Page
        // Home title sitecontents se
        // Home features sitecontentsections se

        public async Task<IActionResult> Index()
        {
            var contents = await GetSiteContents();

            ViewBag.HomeTitle = GetTitle(contents, "HomeTitle", "User Management System");

            ViewBag.HomeSubtitle = GetContent(
                contents,
                "HomeSubtitle",
                "Manage users efficiently using ASP.NET Core MVC and MySQL Database."
            );

            ViewBag.HomeFooterNote = GetContent(
                contents,
                "HomeFooterNote",
                "Built with ASP.NET Core MVC (.NET 10), Entity Framework Core and MySQL using Database-First Approach."
            );

            var homeFeatures = await _context.Sitecontentsections
                .Where(x => x.PageName == "HomeFeature" && x.IsActive == true)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return View(homeFeatures);
        }

        
        // Privacy Page
        
        public async Task<IActionResult> Privacy()
        {
            var contents = await GetSiteContents();

            ViewBag.PrivacyTitle = GetTitle(contents, "PrivacyContent", "Privacy Policy");

            ViewBag.PrivacyIntro = GetContent(
                contents,
                "PrivacyContent",
                "This application stores user information securely."
            );

            var privacySections = await _context.Sitecontentsections
                .Where(x => x.PageName == "Privacy" && x.IsActive == true)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return View(privacySections);
        }
        
        // Help Page
        // Help title sitecontents se
        // Help sections sitecontentsections se
        
        public async Task<IActionResult> Help()
        {
            var contents = await GetSiteContents();

            ViewBag.HelpTitle = GetTitle(contents, "HelpContent", "Help & Support");

            ViewBag.HelpIntro = GetContent(
                contents,
                "HelpContent",
                "Find help, support information and frequently asked questions here."
            );

            var helpSections = await _context.Sitecontentsections
                .Where(x => x.PageName == "Help" && x.IsActive == true)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return View(helpSections);
        }

        
        // Load active site contents
        
        private async Task<Dictionary<string, Sitecontent>> GetSiteContents()
        {
            return await _context.Sitecontents
                .Where(x => x.IsActive == true)
                .ToDictionaryAsync(x => x.ContentKey, x => x);
        }

        
        // Get title by key
        
        private string GetTitle(
            Dictionary<string, Sitecontent> contents,
            string key,
            string fallback)
        {
            if (contents.ContainsKey(key) &&
                !string.IsNullOrWhiteSpace(contents[key].Title))
            {
                return contents[key].Title!;
            }

            return fallback;
        }

        
        // Get content by key
        
        private string GetContent(
            Dictionary<string, Sitecontent> contents,
            string key,
            string fallback)
        {
            if (contents.ContainsKey(key) &&
                !string.IsNullOrWhiteSpace(contents[key].Content))
            {
                return contents[key].Content!;
            }

            return fallback;
        }

        
        // Error Page
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}