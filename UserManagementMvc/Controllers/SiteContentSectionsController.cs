using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Controllers
{
    public class SiteContentSectionsController : Controller
    {
        private readonly AppDbContext _context;

        public SiteContentSectionsController(AppDbContext context)
        {
            _context = context;
        }

        

        // Current user role
        

        private string GetCurrentRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "";
        }

        
        // Current user department
        
        private string GetCurrentDepartment()
        {
            return HttpContext.Session.GetString("UserDepartment") ?? "";
        }

        
        // Allowed page names
        // Privacy, HomeFeature, Help
        
        private string NormalizePageName(string? pageName)
        {
            if (string.Equals(
                pageName,
                "HomeFeature",
                StringComparison.OrdinalIgnoreCase))
            {
                return "HomeFeature";
            }

            if (string.Equals(
                pageName,
                "Help",
                StringComparison.OrdinalIgnoreCase))
            {
                return "Help";
            }

            return "Privacy";
        }

        
        // Page management permission
        
        private bool CanManagePage(string pageName)
        {
            string role =
                HttpContext.Session.GetString("UserRole")?.Trim() ?? "";

            string department =
                HttpContext.Session.GetString("UserDepartment")?.Trim() ?? "";

            bool isSuperAdmin = role.Equals(
                "SuperAdmin",
                StringComparison.OrdinalIgnoreCase
            );

            bool isAdmin = role.Equals(
                "Admin",
                StringComparison.OrdinalIgnoreCase
            );

            bool isSupportDepartment =
                department.Equals(
                    "Support",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                department.Equals(
                    "Support Team",
                    StringComparison.OrdinalIgnoreCase
                )
                ||
                department.Equals(
                    "Support Department",
                    StringComparison.OrdinalIgnoreCase
                );

            // SuperAdmin sab manage karega
            if (isSuperAdmin)
            {
                return true;
            }

            // Admin aur Support Team Help + Privacy manage karenge
            if (pageName == "Help" || pageName == "Privacy")
            {
                return isAdmin || isSupportDepartment;
            }

            // HomeFeature sirf SuperAdmin
            return false;
        }

        
        // Login redirect
        
        private IActionResult RedirectToLogin()
        {
            return RedirectToAction("Login", "UserAuth");
        }

        
        // Page display name
        
        private string GetPageDisplayName(string pageName)
        {
            return pageName switch
            {
                "HomeFeature" => "Home Features",
                "Help" => "Help Sections",
                _ => "Privacy Sections"
            };
        }

        
        // Item name for buttons/text
        
        private string GetItemName(string pageName)
        {
            return pageName switch
            {
                "HomeFeature" => "Feature Box",
                "Help" => "Help Section",
                _ => "Privacy Section"
            };
        }

        
        // Labels for Create/Edit/List
                private void LoadPageViewData(string pageName)
        {
            ViewBag.PageName = pageName;
            ViewBag.PageDisplayName = GetPageDisplayName(pageName);
            ViewBag.ItemName = GetItemName(pageName);

            if (pageName == "HomeFeature")
            {
                ViewBag.TitleLabel = "Feature Title";
                ViewBag.ContentLabel = "Feature Description";

                ViewBag.HelpText =
                    "This feature box will appear on the Home page.";
            }
            else if (pageName == "Help")
            {
                ViewBag.TitleLabel = "Help Title";
                ViewBag.ContentLabel = "Help Description";

                ViewBag.HelpText =
                    "This section will appear on the Help page.";
            }
            else
            {
                ViewBag.TitleLabel = "Section Title";
                ViewBag.ContentLabel = "Section Description";

                ViewBag.HelpText =
                    "This section will appear on the Privacy page.";
            }
        }

        
        // Sections List
        
        public async Task<IActionResult> Index(string? pageName)
        {
            string finalPageName = NormalizePageName(pageName);

            if (!CanManagePage(finalPageName))
            {
                if (string.IsNullOrWhiteSpace(GetCurrentRole()))
                {
                    return RedirectToLogin();
                }

                TempData["Error"] =
                    "You do not have permission to manage this section.";

                return RedirectToAction("Index", "Dashboard");
            }

            LoadPageViewData(finalPageName);

            var sections = await _context.Sitecontentsections
                .Where(x => x.PageName == finalPageName)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return View(sections);
        }

        
        // Add New Section Page
        
        [HttpGet]
        public async Task<IActionResult> Create(string? pageName)
        {
            string finalPageName = NormalizePageName(pageName);

            if (!CanManagePage(finalPageName))
            {
                if (string.IsNullOrWhiteSpace(GetCurrentRole()))
                {
                    return RedirectToLogin();
                }

                TempData["Error"] =
                    "You do not have permission to add this section.";

                return RedirectToAction("Index", "Dashboard");
            }

            LoadPageViewData(finalPageName);

            int maxOrder = await _context.Sitecontentsections
                .Where(x => x.PageName == finalPageName)
                .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

            var model = new Sitecontentsection
            {
                PageName = finalPageName,
                DisplayOrder = maxOrder + 1,
                IsActive = true
            };

            return View(model);
        }

        
        // Save New Section
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Sitecontentsection model)
        {
            string finalPageName =
                NormalizePageName(model.PageName);

            if (!CanManagePage(finalPageName))
            {
                if (string.IsNullOrWhiteSpace(GetCurrentRole()))
                {
                    return RedirectToLogin();
                }

                TempData["Error"] =
                    "You do not have permission to add this section.";

                return RedirectToAction("Index", "Dashboard");
            }

            LoadPageViewData(finalPageName);

            if (string.IsNullOrWhiteSpace(model.SectionTitle))
            {
                ViewBag.Error = "Title is required.";
                model.PageName = finalPageName;

                return View(model);
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                int maxOrder = await _context.Sitecontentsections
                    .Where(x => x.PageName == finalPageName)
                    .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

                int requestedOrder = model.DisplayOrder ?? 1;

                if (requestedOrder < 1)
                {
                    requestedOrder = 1;
                }

                if (requestedOrder > maxOrder + 1)
                {
                    requestedOrder = maxOrder + 1;
                }

                await _context.Database
                    .ExecuteSqlInterpolatedAsync($@"
                        UPDATE sitecontentsections
                        SET DisplayOrder = DisplayOrder + 1
                        WHERE PageName = {finalPageName}
                        AND DisplayOrder >= {requestedOrder}
                        ORDER BY DisplayOrder DESC
                    ");

                model.PageName = finalPageName;
                model.SectionTitle = model.SectionTitle.Trim();
                model.SectionContent =
                    model.SectionContent?.Trim();

                model.DisplayOrder = requestedOrder;
                model.IsActive = model.IsActive ?? true;
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = null;

                _context.Sitecontentsections.Add(model);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    $"{GetItemName(finalPageName)} added successfully.";

                return RedirectToAction(
                    "Index",
                    new { pageName = finalPageName });
            }
            catch
            {
                await transaction.RollbackAsync();

                ViewBag.Error =
                    "Something went wrong while saving.";

                model.PageName = finalPageName;

                return View(model);
            }
        }

        
        // Edit Section Page
        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var section =
                await _context.Sitecontentsections.FindAsync(id);

            if (section == null)
            {
                return NotFound();
            }

            string finalPageName =
                NormalizePageName(section.PageName);

            if (!CanManagePage(finalPageName))
            {
                if (string.IsNullOrWhiteSpace(GetCurrentRole()))
                {
                    return RedirectToLogin();
                }

                TempData["Error"] =
                    "You do not have permission to edit this section.";

                return RedirectToAction("Index", "Dashboard");
            }

            section.PageName = finalPageName;

            LoadPageViewData(finalPageName);

            return View(section);
        }

        
        // Update Section
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            Sitecontentsection model)
        {
            var section =
                await _context.Sitecontentsections.FindAsync(model.Id);

            if (section == null)
            {
                return NotFound();
            }

            string finalPageName =
                NormalizePageName(section.PageName);

            if (!CanManagePage(finalPageName))
            {
                if (string.IsNullOrWhiteSpace(GetCurrentRole()))
                {
                    return RedirectToLogin();
                }

                TempData["Error"] =
                    "You do not have permission to edit this section.";

                return RedirectToAction("Index", "Dashboard");
            }

            LoadPageViewData(finalPageName);

            if (string.IsNullOrWhiteSpace(model.SectionTitle))
            {
                ViewBag.Error = "Title is required.";

                model.PageName = finalPageName;

                return View(model);
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                int oldOrder = section.DisplayOrder ?? 1;

                int maxOrder = await _context.Sitecontentsections
                    .Where(x => x.PageName == finalPageName)
                    .MaxAsync(x => (int?)x.DisplayOrder) ?? 1;

                int newOrder =
                    model.DisplayOrder ?? oldOrder;

                if (newOrder < 1)
                {
                    newOrder = 1;
                }

                if (newOrder > maxOrder)
                {
                    newOrder = maxOrder;
                }

                section.SectionTitle =
                    model.SectionTitle.Trim();

                section.SectionContent =
                    model.SectionContent?.Trim();

                section.IsActive =
                    model.IsActive ?? true;

                section.UpdatedAt = DateTime.Now;

                if (newOrder != oldOrder)
                {
                    section.DisplayOrder = -section.Id;

                    await _context.SaveChangesAsync();

                    if (newOrder < oldOrder)
                    {
                        await _context.Database
                            .ExecuteSqlInterpolatedAsync($@"
                                UPDATE sitecontentsections
                                SET DisplayOrder = DisplayOrder + 1
                                WHERE PageName = {finalPageName}
                                AND Id <> {section.Id}
                                AND DisplayOrder >= {newOrder}
                                AND DisplayOrder < {oldOrder}
                                ORDER BY DisplayOrder DESC
                            ");
                    }
                    else
                    {
                        await _context.Database
                            .ExecuteSqlInterpolatedAsync($@"
                                UPDATE sitecontentsections
                                SET DisplayOrder = DisplayOrder - 1
                                WHERE PageName = {finalPageName}
                                AND Id <> {section.Id}
                                AND DisplayOrder > {oldOrder}
                                AND DisplayOrder <= {newOrder}
                                ORDER BY DisplayOrder ASC
                            ");
                    }

                    section.DisplayOrder = newOrder;
                }
                else
                {
                    section.DisplayOrder = oldOrder;
                }

                // PageName edit form se change nahi hoga
                section.PageName = finalPageName;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] =
                    $"{GetItemName(finalPageName)} updated successfully.";

                return RedirectToAction(
                    "Index",
                    new { pageName = finalPageName });
            }
            catch
            {
                await transaction.RollbackAsync();

                ViewBag.Error =
                    "Something went wrong while updating.";

                model.PageName = finalPageName;

                return View(model);
            }
        }

        
        // Active / Inactive
        
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var section =
                await _context.Sitecontentsections.FindAsync(id);

            if (section == null)
            {
                return RedirectToAction(
                    "Index",
                    new { pageName = "Privacy" });
            }

            string finalPageName =
                NormalizePageName(section.PageName);

            if (!CanManagePage(finalPageName))
            {
                if (string.IsNullOrWhiteSpace(GetCurrentRole()))
                {
                    return RedirectToLogin();
                }

                TempData["Error"] =
                    "You do not have permission to change this section.";

                return RedirectToAction("Index", "Dashboard");
            }

            section.IsActive =
                !(section.IsActive ?? true);

            section.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Index",
                new { pageName = finalPageName });
        }

        
        // Delete Section
        
        public async Task<IActionResult> Delete(int id)
        {
            var section =
                await _context.Sitecontentsections.FindAsync(id);

            if (section == null)
            {
                return RedirectToAction(
                    "Index",
                    new { pageName = "Privacy" });
            }

            string finalPageName =
                NormalizePageName(section.PageName);

            if (!CanManagePage(finalPageName))
            {
                if (string.IsNullOrWhiteSpace(GetCurrentRole()))
                {
                    return RedirectToLogin();
                }

                TempData["Error"] =
                    "You do not have permission to delete this section.";

                return RedirectToAction("Index", "Dashboard");
            }

            using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                int deletedOrder =
                    section.DisplayOrder ?? 0;

                _context.Sitecontentsections.Remove(section);

                await _context.SaveChangesAsync();

                if (deletedOrder > 0)
                {
                    await _context.Database
                        .ExecuteSqlInterpolatedAsync($@"
                            UPDATE sitecontentsections
                            SET DisplayOrder = DisplayOrder - 1
                            WHERE PageName = {finalPageName}
                            AND DisplayOrder > {deletedOrder}
                            ORDER BY DisplayOrder ASC
                        ");
                }

                await transaction.CommitAsync();

                TempData["Success"] =
                    $"{GetItemName(finalPageName)} deleted successfully.";

                return RedirectToAction(
                    "Index",
                    new { pageName = finalPageName });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Something went wrong while deleting.";

                return RedirectToAction(
                    "Index",
                    new { pageName = finalPageName });
            }
        }
    }
}