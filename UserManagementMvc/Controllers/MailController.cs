using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;
using UserManagementMvc.Services;
using UserManagementMvc.ViewModels;

namespace UserManagementMvc.Controllers;

public class MailController : Controller
{
    private const int MaxInboxMessages = 50;

    private readonly AppDbContext _context;
    private readonly IMailService _mailService;
    private readonly IMailServerService _mailServerService;
    private readonly IAuditLogService _auditLogService;

    public MailController(
        AppDbContext context,
        IMailService mailService,
        IMailServerService mailServerService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _mailService = mailService;
        _mailServerService = mailServerService;
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

        var databaseMessages =
            await _mailService.GetInboxAsync(
                userId.Value);

        var serverMessages =
            await _mailServerService.GetInboxAsync(
                userId.Value);

        var inbox =
            new List<MailDetailsViewModel>();

        foreach (var message in databaseMessages)
        {
            inbox.Add(
                new MailDetailsViewModel
                {
                    Id = message.Id,
                    DatabaseMessage = message
                });
        }

        foreach (var message in serverMessages)
        {
            inbox.Add(
                new MailDetailsViewModel
                {
                    ServerMessage = message
                });
        }

        var latest50 =
            inbox
                .OrderByDescending(GetMessageDate)
                .Take(MaxInboxMessages)
                .ToList();

        return View(latest50);
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
            await _mailService.GetSentAsync(
                userId.Value);

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
            await _mailService.GetStarredAsync(
                userId.Value);

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
            await _mailService.GetDraftsAsync(
                userId.Value);

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
            await _mailService.GetTrashAsync(
                userId.Value);

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
                RecipientEmail = to ?? "",
                ReplyToMessageId = replyTo
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
            await _mailService.GetDraftAsync(
                id,
                userId.Value);

        if (draft == null)
        {
            return NotFound();
        }

        var model =
            new MailComposeViewModel
            {
                DraftId = draft.Id,

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
                    draft.Attachments.ToList()
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
            await _mailService.SendDraftAsync(
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
        string id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (id.StartsWith(
                "imap:",
                StringComparison.OrdinalIgnoreCase))
        {
            var serverMessage =
                await _mailServerService.GetMessageAsync(
                    id,
                    userId.Value);

            if (serverMessage == null)
            {
                return NotFound();
            }

            serverMessage.IsRead = true;

            return View(
                new MailDetailsViewModel
                {
                    ServerMessage = serverMessage
                });
        }

        if (!int.TryParse(
                id,
                out int databaseMessageId))
        {
            return NotFound();
        }

        var mail =
            await _mailService.GetMessageAsync(
                databaseMessageId,
                userId.Value);

        if (mail == null)
        {
            return NotFound();
        }

        if (mail.RecipientUserId.HasValue &&
            mail.RecipientUserId.Value == userId.Value)
        {
            await _mailService.MarkAsReadAsync(
                databaseMessageId,
                userId.Value);
        }

        return View(
            new MailDetailsViewModel
            {
                DatabaseMessage = mail
            });
    }

    [HttpGet]
    public async Task<IActionResult> PreviewAttachment(
        string id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (id.StartsWith(
                "imap:",
                StringComparison.OrdinalIgnoreCase))
        {
            var serverAttachment =
                await _mailServerService.GetAttachmentAsync(
                    id,
                    userId.Value);

            if (serverAttachment == null)
            {
                return NotFound();
            }

            string previewContentType =
                string.IsNullOrWhiteSpace(
                    serverAttachment.ContentType)
                    ? "application/octet-stream"
                    : serverAttachment.ContentType;

            return File(
                serverAttachment.Content,
                previewContentType,
                enableRangeProcessing: true);
        }

        if (!int.TryParse(
                id,
                out int attachmentId))
        {
            return NotFound();
        }

        var databaseAttachment =
            await _mailService.GetAttachmentAsync(
                attachmentId,
                userId.Value);

        if (databaseAttachment == null)
        {
            return NotFound();
        }

        string physicalPath =
            databaseAttachment.StoragePath;

        if (!Path.IsPathRooted(
                physicalPath))
        {
            physicalPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    physicalPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
        }

        if (!System.IO.File.Exists(
                physicalPath))
        {
            return NotFound();
        }

        string databasePreviewContentType =
            string.IsNullOrWhiteSpace(
                databaseAttachment.ContentType)
                    ? "application/octet-stream"
                    : databaseAttachment.ContentType;

        return PhysicalFile(
            physicalPath,
            databasePreviewContentType,
            enableRangeProcessing: true);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(
        string id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (id.StartsWith(
                "imap:",
                StringComparison.OrdinalIgnoreCase))
        {
            var serverAttachment =
                await _mailServerService.GetAttachmentAsync(
                    id,
                    userId.Value);

            if (serverAttachment == null)
            {
                return NotFound();
            }

            string downloadContentType =
                string.IsNullOrWhiteSpace(
                    serverAttachment.ContentType)
                    ? "application/octet-stream"
                    : serverAttachment.ContentType;

            string downloadFileName =
                string.IsNullOrWhiteSpace(
                    serverAttachment.FileName)
                    ? "attachment"
                    : serverAttachment.FileName;

            return File(
                serverAttachment.Content,
                downloadContentType,
                downloadFileName,
                enableRangeProcessing: true);
        }

        if (!int.TryParse(
                id,
                out int attachmentId))
        {
            return NotFound();
        }

        var databaseAttachment =
            await _mailService.GetAttachmentAsync(
                attachmentId,
                userId.Value);

        if (databaseAttachment == null)
        {
            return NotFound();
        }

        string databasePhysicalPath =
            databaseAttachment.StoragePath;

        if (!Path.IsPathRooted(
                databasePhysicalPath))
        {
            databasePhysicalPath =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    databasePhysicalPath.Replace(
                        '/',
                        Path.DirectorySeparatorChar));
        }

        if (!System.IO.File.Exists(
                databasePhysicalPath))
        {
            return NotFound();
        }

        string databaseDownloadContentType =
            string.IsNullOrWhiteSpace(
                databaseAttachment.ContentType)
                    ? "application/octet-stream"
                    : databaseAttachment.ContentType;

        string databaseDownloadFileName =
            string.IsNullOrWhiteSpace(
                databaseAttachment.OriginalFileName)
                    ? "attachment"
                    : databaseAttachment.OriginalFileName;

        return PhysicalFile(
            databasePhysicalPath,
            databaseDownloadContentType,
            databaseDownloadFileName,
            enableRangeProcessing: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStar(
        string id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (id.StartsWith(
                "imap:",
                StringComparison.OrdinalIgnoreCase))
        {
            await _mailServerService.ToggleStarAsync(
                id,
                userId.Value);

            return RedirectToAction(
                nameof(Details),
                new
                {
                    id
                });
        }

        if (!int.TryParse(
                id,
                out int databaseMessageId))
        {
            return NotFound();
        }

        await _mailService.ToggleStarAsync(
            databaseMessageId,
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
        string id,
        string returnAction = "Inbox")
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (id.StartsWith(
                "imap:",
                StringComparison.OrdinalIgnoreCase))
        {
            await _mailServerService.ToggleStarAsync(
                id,
                userId.Value);

            return RedirectToAction(
                returnAction);
        }

        if (!int.TryParse(
                id,
                out int databaseMessageId))
        {
            return NotFound();
        }

        await _mailService.ToggleStarAsync(
            databaseMessageId,
            userId.Value);

        return RedirectToAction(
            returnAction);
    }

    [HttpGet]
    public async Task<IActionResult> DeleteInbox(
        string id)
    {
        int? userId = CurrentUserId();

        if (userId == null)
        {
            return LoginRedirect();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        if (id.StartsWith(
                "imap:",
                StringComparison.OrdinalIgnoreCase))
        {
            await _mailServerService.DeleteFromInboxAsync(
                id,
                userId.Value);

            return RedirectToAction(
                nameof(Inbox));
        }

        if (!int.TryParse(
                id,
                out int databaseMessageId))
        {
            return NotFound();
        }

        await _mailService.DeleteFromInboxAsync(
            databaseMessageId,
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

    private static DateTime GetMessageDate(
        MailDetailsViewModel message)
    {
        if (message.IsServerMessage)
        {
            return message.ServerMessage!.SentAt;
        }

        return message.DatabaseMessage!.SentAt;
    }

    private async Task LoadComposeDataAsync(
        MailComposeViewModel model,
        int userId)
    {
        model.Recipients =
            await _context.Users
                .AsNoTracking()
                .Where(x =>
                    x.Id != userId &&
                    x.IsDeleted != true &&
                    x.IsActive == true &&
                    !string.IsNullOrWhiteSpace(x.Email))
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