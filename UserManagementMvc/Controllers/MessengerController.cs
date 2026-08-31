using Microsoft.AspNetCore.Mvc;
using UserManagementMvc.Services;

namespace UserManagementMvc.Controllers
{
    public class MessengerController : Controller
    {
        private readonly MessengerService _messengerService;
        private readonly IWebHostEnvironment _environment;

        private const long MaxAttachmentSize =
            25 * 1024 * 1024;

        private static readonly HashSet<string>
            AllowedExtensions =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif",
                    ".webp",

                    ".mp4",
                    ".webm",
                    ".mov",

                    ".pdf",
                    ".doc",
                    ".docx",
                    ".xls",
                    ".xlsx",
                    ".txt",
                    ".zip"
                };

        public MessengerController(
            MessengerService messengerService,
            IWebHostEnvironment environment)
        {
            _messengerService =
                messengerService;

            _environment =
                environment;
        }

        private int? CurrentUserId
        {
            get
            {
                return HttpContext.Session
                    .GetInt32("UserId");
            }
        }

         
        // MESSENGER HOME
         

        [HttpGet]
        public async Task<IActionResult> Index(
            string? search)
        {
            if (!CurrentUserId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            var users =
                await _messengerService
                    .SearchUsersAsync(
                        CurrentUserId.Value,
                        search);

            var model =
                new UserManagementMvc.ViewModels
                    .MessengerViewModel
                {
                    Users = users
                };

            return View(model);
        }

         
        // LIVE SEARCH API
         

        [HttpGet]
        public async Task<IActionResult> SearchUsers(
            string? search)
        {
            if (!CurrentUserId.HasValue)
            {
                return Unauthorized();
            }

            var users =
                await _messengerService
                    .SearchUsersAsync(
                        CurrentUserId.Value,
                        search);

            return Json(users);
        }

         
        // CHAT
         

        [HttpGet]
        public async Task<IActionResult> Chat(int id)
        {
            if (!CurrentUserId.HasValue)
            {
                return RedirectToAction(
                    "Login",
                    "UserAuth");
            }

            if (id == CurrentUserId.Value)
            {
                return RedirectToAction("Index");
            }

            var selectedUser =
                await _messengerService
                    .GetUserByIdAsync(
                        CurrentUserId.Value,
                        id);

            if (selectedUser == null)
            {
                return RedirectToAction("Index");
            }

            var messages =
                await _messengerService
                    .GetConversationAsync(
                        CurrentUserId.Value,
                        id);

            var emojis =
    await _messengerService
        .GetActiveEmojisAsync();

            ViewBag.Emojis = emojis;

            ViewBag.OtherUserId = id;

            ViewBag.OtherUser =
                selectedUser;

            return View(messages);
        }

         
        // UPLOAD ATTACHMENT
         

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAttachment(
            IFormFile? file)
        {
            if (!CurrentUserId.HasValue)
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Login required."
                });
            }

            if (file == null ||
                file.Length <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Please select a file."
                });
            }

            if (file.Length > MaxAttachmentSize)
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Maximum file size is 25 MB."
                });
            }

            var extension =
                Path.GetExtension(
                    file.FileName);

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "This file type is not allowed."
                });
            }

            var uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "chat");

            Directory.CreateDirectory(
                uploadFolder);

            var safeOriginalName =
                Path.GetFileName(
                    file.FileName);

            var storedFileName =
                $"{Guid.NewGuid():N}{extension}";

            var fullPath =
                Path.Combine(
                    uploadFolder,
                    storedFileName);

            await using (
                var stream =
                    new FileStream(
                        fullPath,
                        FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url =
                $"/uploads/chat/{storedFileName}";

            return Json(new
            {
                success = true,

                url = url,

                name = safeOriginalName,

                contentType =
                    file.ContentType,

                size =
                    file.Length
            });
        }

         
        // DELETE MESSAGE FOR ME
         

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteForMe(
            int id)
        {
            if (!CurrentUserId.HasValue)
            {
                return Json(new
                {
                    success = false,
                    message = "Login required."
                });
            }

            bool result =
                await _messengerService
                    .DeleteForMeAsync(
                        CurrentUserId.Value,
                        id);

            return Json(new
            {
                success = result
            });
        }
    }
}