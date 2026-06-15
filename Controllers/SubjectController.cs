using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class SubjectController : Controller
{
    private readonly ISubjectService _subjectService;
    private readonly IGradeService _gradeService;

    public SubjectController(ISubjectService subjectService, IGradeService gradeService)
    {
        _subjectService = subjectService;
        _gradeService = gradeService;
    }

    public async Task<IActionResult> Index(int? gradeId)
    {
        var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Content Manager");
        ViewBag.Grades = isAdminOrManager ? await _gradeService.GetAllAsync() : await _gradeService.GetActiveAsync();
        ViewBag.SelectedGradeId = gradeId;
        
        List<Subject> subjects = gradeId.HasValue
            ? await _subjectService.GetByGradeIdAsync(gradeId.Value)
            : await _subjectService.GetAllAsync();

        if (!isAdminOrManager)
        {
            subjects = subjects.Where(s => s.Status == "Active").ToList();
        }

        if (gradeId.HasValue)
        {
            var grade = await _gradeService.GetByIdAsync(gradeId.Value);
            ViewBag.Grade = grade;
        }

        return View(subjects);
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Grades = await _gradeService.GetAllAsync();
        return View();
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Create(Subject subject, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "subjects");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await imageFile.CopyToAsync(stream);
            subject.Image = $"/uploads/subjects/{fileName}";
        }
        await _subjectService.AddAsync(subject);
        TempData["Success"] = "Subject added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var subject = await _subjectService.GetByIdAsync(id);
        if (subject == null) return NotFound();
        ViewBag.Grades = await _gradeService.GetAllAsync();
        return View(subject);
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Edit(Subject subject, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "subjects");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await imageFile.CopyToAsync(stream);
            subject.Image = $"/uploads/subjects/{fileName}";
        }
        await _subjectService.UpdateAsync(subject);
        TempData["Success"] = "Subject updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _subjectService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // API endpoint for cascading dropdowns
    [AllowAnonymous]
    public async Task<IActionResult> GetByGrade(int gradeId)
    {
        var subjects = await _subjectService.GetByGradeIdAsync(gradeId);
        return Json(subjects.Select(s => new { s.SubjectId, s.SubjectName }));
    }
}
