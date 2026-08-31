using Microsoft.AspNetCore.Mvc;
using UserManagementMvc.Services;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers;

public class MailController : Controller
{
    private readonly IMailService _mailService;
    private readonly IAuditLogService _auditLogService;

    public MailController(
        IMailService mailService,
        IAuditLogService auditLogService)
    {
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

        var inbox = await _mailService
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

        var sent = await _mailService
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

        var starred = await _mailService
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

        var drafts = await _mailService
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

        var trash = await _mailService
            .GetTrashAsync(userId.Value);

        return View(trash);
    }

    [HttpGet]
    public IActionResult Compose(
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

        var model = new MailComposeViewModel
        {
            RecipientEmail = to ?? "",
            ReplyToMessageId = replyTo
        };

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
            return View(model);
        }

        bool result = await _mailService.SendMailAsync(
            userId.Value,
            model.RecipientEmail,
            model.Subject,
            model.Body,
            model.ReplyToMessageId);

        if (!result)
        {
            ModelState.AddModelError(
                "",
                "Mail could not be sent.");

            return View(model);
        }

        await _auditLogService.LogAsync(
            userId.Value,
            "Send Mail",
            $"Mail sent to {model.RecipientEmail}");

        TempData["Success"] =
            "Mail sent successfully.";

        return RedirectToAction(nameof(Sent));
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

        var draftId = await _mailService.SaveDraftAsync(
            userId.Value,
            model.RecipientEmail,
            model.Subject,
            model.Body,
            model.DraftId);

        if (draftId == null)
        {
            ModelState.AddModelError(
                "",
                "Draft could not be saved.");

            return View("Compose", model);
        }

        TempData["Success"] =
            "Draft saved successfully.";

        return RedirectToAction(nameof(Drafts));
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

        var draft = await _mailService
            .GetDraftAsync(id, userId.Value);

        if (draft == null)
        {
            return NotFound();
        }

        var model = new MailComposeViewModel
        {
            DraftId = draft.Id,
            RecipientEmail = draft.RecipientEmail,
            Subject = draft.Subject,
            Body = draft.Body
        };

        return View("Compose", model);
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

        bool result = await _mailService.SendDraftAsync(
            id,
            userId.Value);

        if (!result)
        {
            TempData["Error"] =
                "Draft could not be sent. Please check recipient, subject and message.";

            return RedirectToAction(nameof(EditDraft), new
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

        return RedirectToAction(nameof(Sent));
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

        var mail = await _mailService
            .GetMessageAsync(
                id,
                userId.Value);

        if (mail == null)
        {
            return NotFound();
        }

        if (mail.RecipientUserId == userId.Value)
        {
            await _mailService.MarkAsReadAsync(
                id,
                userId.Value);
        }

        return View(mail);
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

        return RedirectToAction(nameof(Inbox));
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

        return RedirectToAction(nameof(Sent));
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

        return RedirectToAction(nameof(Drafts));
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

        return RedirectToAction(nameof(Trash));
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

        return RedirectToAction(nameof(Trash));
    }
}