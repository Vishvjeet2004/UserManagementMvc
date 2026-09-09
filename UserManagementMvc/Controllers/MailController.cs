using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;
using UserManagementMvc.Services;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers;

public class MailController : Controller
{
    private readonly AppDbContext _context;
    private readonly IMailService _mailService;
    private readonly IAuditLogService _auditLogService;

    public MailController(
        AppDbContext context,
        IMailService mailService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _mailService = mailService;
        _auditLogService = auditLogService;
    }

    private int? CurrentUserId()
    {
        return HttpContext.Session.GetInt32("UserId");
    }

    private IActionResult LoginRedirect()
    {
        return RedirectToAction(
            "Login",
            "UserAuth");
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return await Inbox();
    }

    [HttpGet]
    public async Task<IActionResult> Inbox()
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var inbox =
            await _mailService
                .GetInboxAsync(userId.Value);

        return View(inbox);
    }

    [HttpGet]
    public async Task<IActionResult> Sent()
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var sent =
            await _mailService
                .GetSentAsync(userId.Value);

        return View(sent);
    }

    [HttpGet]
    public async Task<IActionResult> Starred()
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var starred =
            await _mailService
                .GetStarredAsync(userId.Value);

        return View(starred);
    }

    [HttpGet]
    public async Task<IActionResult> Drafts()
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var drafts =
            await _mailService
                .GetDraftsAsync(userId.Value);

        return View(drafts);
    }

    [HttpGet]
    public async Task<IActionResult> Trash()
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var trash =
            await _mailService
                .GetTrashAsync(userId.Value);

        return View(trash);
    }

    [HttpGet]
    public async Task<IActionResult> Compose(
        string? to = null,
        int? replyTo = null,
        int? draftId = null)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (draftId.HasValue)
        {
            return RedirectToAction(
                nameof(EditDraft),
                new
                {
                    id = draftId.Value
                });
        }

        var model =
            new MailComposeViewModel
            {
                RecipientEmail =
                    to ?? "",
                ReplyToMessageId =
                    replyTo
            };

        await LoadComposeDataAsync(
            model,
            userId.Value);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Compose(
        MailComposeViewModel model)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (!ModelState.IsValid)
        {
            await LoadComposeDataAsync(
                model,
                userId.Value);

            return View(model);
        }

        bool result =
            await _mailService.SendMailAsync(
                userId.Value,
                model.RecipientEmail,
                model.CcEmails,
                model.BccEmails,
                model.Subject,
                model.Body,
                model.ReplyToMessageId,
                model.Attachments);

        if (!result)
        {
            ModelState.AddModelError(
                "",
                "Mail could not be sent. Please check recipient, attachments and SMTP settings.");

            await LoadComposeDataAsync(
                model,
                userId.Value);

            return View(model);
        }

        await _auditLogService.LogAsync(
            userId.Value,
            "Send Mail",
            $"Mail sent to {model.RecipientEmail}");

        TempData["Success"] =
            "Mail sent successfully.";

        return RedirectToAction(
            nameof(Sent));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveDraft(
        MailComposeViewModel model)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        int? draftId =
            await _mailService.SaveDraftAsync(
                userId.Value,
                model.RecipientEmail,
                model.CcEmails,
                model.BccEmails,
                model.Subject,
                model.Body,
                model.DraftId,
                model.Attachments);

        if (draftId == null)
        {
            ModelState.AddModelError(
                "",
                "Draft could not be saved.");

            await LoadComposeDataAsync(
                model,
                userId.Value);

            return View(
                "Compose",
                model);
        }

        TempData["Success"] =
            "Draft saved successfully.";

        return RedirectToAction(
            nameof(Drafts));
    }

    [HttpGet]
    public async Task<IActionResult> EditDraft(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var draft =
            await _mailService
                .GetDraftAsync(
                    id,
                    userId.Value);

        if (draft == null)
        {
            return NotFound();
        }

        var model =
            new MailComposeViewModel
            {
                DraftId =
                    draft.Id,

                RecipientEmail =
                    draft.RecipientEmail,

                CcEmails =
                    draft.CcEmails,

                BccEmails =
                    draft.BccEmails,

                Subject =
                    draft.Subject,

                Body =
                    draft.Body,

                ExistingAttachments =
                    draft.Attachments
                        .ToList()
            };

        await LoadComposeDataAsync(
            model,
            userId.Value);

        return View(
            "Compose",
            model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendDraft(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        bool result =
            await _mailService
                .SendDraftAsync(
                    id,
                    userId.Value);

        if (!result)
        {
            TempData["Error"] =
                "Draft could not be sent. Please check recipient, subject, message and SMTP settings.";

            return RedirectToAction(
                nameof(EditDraft),
                new
                {
                    id
                });
        }

        await _auditLogService.LogAsync(
            userId.Value,
            "Send Draft",
            $"Draft {id} sent.");

        TempData["Success"] =
            "Draft sent successfully.";

        return RedirectToAction(
            nameof(Sent));
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var mail =
            await _mailService
                .GetMessageAsync(
                    id,
                    userId.Value);

        if (mail == null)
        {
            return NotFound();
        }

        if (mail.RecipientUserId.HasValue &&
            mail.RecipientUserId.Value == userId.Value)
        {
            await _mailService.MarkAsReadAsync(
                id,
                userId.Value);
        }

        return View(mail);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        var attachment =
            await _context
                .Set<MailAttachment>()
                .Include(x => x.MailMessage)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    (
                        x.MailMessage.SenderUserId ==
                            userId.Value
                        ||
                        x.MailMessage.RecipientUserId ==
                            userId.Value
                    ));

        if (attachment == null)
        {
            return NotFound();
        }

        string relativePath =
            attachment.StoragePath
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);

        string physicalPath =
            Path.Combine(
                Directory.GetCurrentDirectory(),
                relativePath);

        if (!System.IO.File.Exists(
                physicalPath))
        {
            return NotFound();
        }

        string contentType =
            string.IsNullOrWhiteSpace(
                attachment.ContentType)
                ? "application/octet-stream"
                : attachment.ContentType;

        return PhysicalFile(
            physicalPath,
            contentType,
            attachment.OriginalFileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStar(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        await _mailService.ToggleStarAsync(
            id,
            userId.Value);

        return RedirectToAction(
            nameof(Details),
            new
            {
                id
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStarFromList(
        int id,
        string returnAction = "Inbox")
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        await _mailService.ToggleStarAsync(
            id,
            userId.Value);

        return RedirectToAction(
            returnAction);
    }

    [HttpGet]
    public async Task<IActionResult> DeleteInbox(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        await _mailService.DeleteFromInboxAsync(
            id,
            userId.Value);

        return RedirectToAction(
            nameof(Inbox));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteSent(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        await _mailService.DeleteFromSentAsync(
            id,
            userId.Value);

        return RedirectToAction(
            nameof(Sent));
    }

    [HttpGet]
    public async Task<IActionResult> DeleteDraft(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        await _mailService.DeleteDraftAsync(
            id,
            userId.Value);

        return RedirectToAction(
            nameof(Drafts));
    }

    [HttpGet]
    public async Task<IActionResult> Restore(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        await _mailService.RestoreAsync(
            id,
            userId.Value);

        return RedirectToAction(
            nameof(Trash));
    }

    [HttpGet]
    public async Task<IActionResult> PermanentDelete(
        int id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        await _mailService.PermanentDeleteAsync(
            id,
            userId.Value);

        return RedirectToAction(
            nameof(Trash));
    }

    private async Task LoadComposeDataAsync(
        MailComposeViewModel model,
        int userId)
    {
        model.Recipients =
            await _context
                .Users
                .AsNoTracking()
                .Where(x =>
                    x.Id != userId &&
                    x.IsDeleted != true &&
                    x.IsActive == true &&
                    !string.IsNullOrWhiteSpace(
                        x.Email))
                .OrderBy(x => x.Name)
                .ToListAsync();

        model.Tools =
            await _context
                .Set<MailComposerTool>()
                .AsNoTracking()
                .Where(x =>
                    x.IsEnabled)
                .OrderBy(x =>
                    x.DisplayOrder)
                .ThenBy(x =>
                    x.Id)
                .ToListAsync();

        model.Signatures =
            await _context
                .Set<MailSignature>()
                .AsNoTracking()
                .Where(x =>
                    x.IsActive)
                .OrderByDescending(x =>
                    x.IsDefault)
                .ThenBy(x =>
                    x.Name)
                .ToListAsync();
    }
}