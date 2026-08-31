using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementMvc.Models;

namespace UserManagementMvc.Controllers
{
    public class SecurityQuestionsController : Controller
    {
        private readonly AppDbContext _context;

        public SecurityQuestionsController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsSuperAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "SuperAdmin";
        }

        
        // List
        
        public async Task<IActionResult> Index()
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            var questions = await _context.SecurityQuestionMasters
                .OrderBy(x => x.Id)
                .ToListAsync();

            return View(questions);
        }

        
        // Create Page
        
        [HttpGet]
        public IActionResult Create()
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            return View();
        }

        
        // Create Submit
        
        [HttpPost]
        public async Task<IActionResult> Create(Securityquestionmaster model)
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            if (string.IsNullOrWhiteSpace(model.QuestionText))
            {
                ViewBag.Error = "Question text is required.";
                return View(model);
            }

            string questionText = model.QuestionText.Trim();

            var exists = await _context.SecurityQuestionMasters
                .AnyAsync(x => x.QuestionText == questionText);

            if (exists)
            {
                ViewBag.Error = "This security question already exists.";
                return View(model);
            }

            model.QuestionText = questionText;
            model.IsActive = true;
            model.CreatedByRole = "SuperAdmin";
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = null;

            _context.SecurityQuestionMasters.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        
        // Edit Page
        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            var question = await _context.SecurityQuestionMasters.FindAsync(id);

            if (question == null)
                return NotFound();

            return View(question);
        }

        
        // Edit Submit
        
        [HttpPost]
        public async Task<IActionResult> Edit(Securityquestionmaster model)
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            var question = await _context.SecurityQuestionMasters.FindAsync(model.Id);

            if (question == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(model.QuestionText))
            {
                ViewBag.Error = "Question text is required.";
                return View(model);
            }

            question.QuestionText = model.QuestionText.Trim();
            question.IsActive = model.IsActive;
            question.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        
        // Active / Inactive
        
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (!IsSuperAdmin())
                return RedirectToAction("Login", "SuperAdminAuth");

            var question = await _context.SecurityQuestionMasters.FindAsync(id);

            if (question != null)
            {
                question.IsActive = !(question.IsActive ?? true);
                question.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}