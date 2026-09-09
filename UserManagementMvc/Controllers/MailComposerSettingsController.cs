using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;
using UserManagementMvc.Services;

namespace UserManagementMvc.Controllers;

public class MailComposerSettingsController : Controller
{
    private readonly AppDbContext _context;
    private readonly MailHtmlSanitizer _htmlSanitizer;

    public MailComposerSettingsController(
        AppDbContext context,
        MailHtmlSanitizer htmlSanitizer)
    {
        _context = context;
        _htmlSanitizer = htmlSanitizer;
    }

    private bool IsSuperAdmin()
    {
        return string.Equals(
            HttpContext.Session
                .GetString("UserRole"),
            "SuperAdmin",
            StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!IsSuperAdmin())
        {
            return RedirectToAction(
                "Index",
                "Dashboard");
        }

        var tools =
            await _context.MailComposerTools
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ToListAsync();

        var signatures =
            await _context.MailSignatures
                .OrderByDescending(x => x.IsDefault)
                .ThenBy(x => x.Name)
                .ToListAsync();

        ViewBag.Signatures =
            signatures;

        return View(tools);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTools(
        List<MailComposerTool> tools)
    {
        if (!IsSuperAdmin())
        {
            return Forbid();
        }

        foreach (var item in tools)
        {
            var existing =
                await _context.MailComposerTools
                    .FirstOrDefaultAsync(
                        x => x.Id == item.Id);

            if (existing == null)
            {
                continue;
            }

            existing.IsEnabled =
                item.IsEnabled;

            existing.DisplayOrder =
                item.DisplayOrder;

            existing.UpdatedAt =
                DateTime.Now;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Mail composer tools updated successfully.";

        return RedirectToAction(
            nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSignature(
        string name,
        string signatureHtml)
    {
        if (!IsSuperAdmin())
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Error"] =
                "Signature name is required.";

            return RedirectToAction(
                nameof(Index));
        }

        string safeHtml =
            _htmlSanitizer.Sanitize(
                signatureHtml);

        if (string.IsNullOrWhiteSpace(
            safeHtml))
        {
            TempData["Error"] =
                "Signature content is required.";

            return RedirectToAction(
                nameof(Index));
        }

        bool hasDefault =
            await _context.MailSignatures
                .AnyAsync(x =>
                    x.IsDefault &&
                    x.IsActive);

        var signature =
            new MailSignature
            {
                Name =
                    name.Trim(),
                SignatureHtml =
                    safeHtml,
                IsActive =
                    true,
                IsDefault =
                    !hasDefault,
                CreatedBy =
                    HttpContext.Session
                        .GetInt32("UserId"),
                CreatedAt =
                    DateTime.Now
            };

        _context.MailSignatures
            .Add(signature);

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Signature added successfully.";

        return RedirectToAction(
            nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        SetDefaultSignature(int id)
    {
        if (!IsSuperAdmin())
        {
            return Forbid();
        }

        var signatures =
            await _context.MailSignatures
                .Where(x => x.IsActive)
                .ToListAsync();

        foreach (var signature in signatures)
        {
            signature.IsDefault =
                signature.Id == id;

            signature.UpdatedAt =
                DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        ToggleSignature(int id)
    {
        if (!IsSuperAdmin())
        {
            return Forbid();
        }

        var signature =
            await _context.MailSignatures
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (signature == null)
        {
            return NotFound();
        }

        signature.IsActive =
            !signature.IsActive;

        if (!signature.IsActive)
        {
            signature.IsDefault = false;
        }

        signature.UpdatedAt =
            DateTime.Now;

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
        DeleteSignature(int id)
    {
        if (!IsSuperAdmin())
        {
            return Forbid();
        }

        var signature =
            await _context.MailSignatures
                .FirstOrDefaultAsync(
                    x => x.Id == id);

        if (signature == null)
        {
            return NotFound();
        }

        _context.MailSignatures
            .Remove(signature);

        await _context.SaveChangesAsync();

        return RedirectToAction(
            nameof(Index));
    }
}