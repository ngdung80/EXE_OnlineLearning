using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin,Content Manager,Content Approver")]
public class LessonController : Controller
{
    private readonly ILessonService _lessonService;
    private readonly IChapterService _chapterService;
    private readonly ISubjectService _subjectService;
    private readonly IGradeService _gradeService;

    public LessonController(ILessonService lessonService, IChapterService chapterService,
        ISubjectService subjectService, IGradeService gradeService)
    {
        _lessonService = lessonService;
        _chapterService = chapterService;
        _subjectService = subjectService;
        _gradeService = gradeService;
    }

    public async Task<IActionResult> Index(string? name, int? gradeId, int? subjectId, int? chapterId, string? status, int page = 1)
    {
        const int pageSize = 10;
        ViewBag.Grades = await _gradeService.GetAllAsync();
        ViewBag.Subjects = subjectId.HasValue ? await _subjectService.GetByGradeIdAsync(gradeId ?? 0) : new List<Subject>();
        ViewBag.Chapters = chapterId.HasValue ? await _chapterService.GetBySubjectIdAsync(subjectId ?? 0) : new List<Chapter>();
        ViewBag.Filter = new { name, gradeId, subjectId, chapterId, status, page };

        var lessons = await _lessonService.SearchAsync(name, gradeId, subjectId, chapterId, status, page, pageSize);
        var total = await _lessonService.CountAsync(name, gradeId, subjectId, chapterId, status);
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.CurrentPage = page;
        return View(lessons);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Grades = await _gradeService.GetAllAsync();
        ViewBag.Chapters = new List<Chapter>();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Lesson lesson, IFormFile? lessonFile, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lessons");
            Directory.CreateDirectory(dir);
            var fn = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            using var s = new FileStream(Path.Combine(dir, fn), FileMode.Create);
            await imageFile.CopyToAsync(s);
            lesson.ImageUrl = $"/uploads/lessons/{fn}";
        }
        if (lessonFile != null && lessonFile.Length > 0)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files");
            Directory.CreateDirectory(dir);
            var fn = $"{Guid.NewGuid()}{Path.GetExtension(lessonFile.FileName)}";
            using var s = new FileStream(Path.Combine(dir, fn), FileMode.Create);
            await lessonFile.CopyToAsync(s);
            lesson.FileUrl = $"/uploads/files/{fn}";
        }
        lesson.Status = "Active";
        await _lessonService.AddAsync(lesson);
        TempData["Success"] = "Lesson added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _lessonService.GetByIdAsync(id);
        if (lesson == null) return NotFound();
        ViewBag.Grades = await _gradeService.GetAllAsync();
        ViewBag.Chapters = await _chapterService.GetAllAsync();
        return View(lesson);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Lesson lesson, IFormFile? imageFile, IFormFile? lessonFile)
    {
        var existing = await _lessonService.GetByIdAsync(lesson.LessonId);
        if (existing == null) return NotFound();

        existing.LessonName = lesson.LessonName;
        existing.ChapterId = lesson.ChapterId;
        existing.ContentText = lesson.ContentText;

        if (imageFile != null && imageFile.Length > 0)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lessons");
            Directory.CreateDirectory(dir);
            var fn = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            using var s = new FileStream(Path.Combine(dir, fn), FileMode.Create);
            await imageFile.CopyToAsync(s);
            existing.ImageUrl = $"/uploads/lessons/{fn}";
        }
        if (lessonFile != null && lessonFile.Length > 0)
        {
            var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "files");
            Directory.CreateDirectory(dir);
            var fn = $"{Guid.NewGuid()}{Path.GetExtension(lessonFile.FileName)}";
            using var s = new FileStream(Path.Combine(dir, fn), FileMode.Create);
            await lessonFile.CopyToAsync(s);
            existing.FileUrl = $"/uploads/files/{fn}";
        }

        await _lessonService.UpdateAsync(existing);
        TempData["Success"] = "Lesson updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetInactive(int id) { await _lessonService.SetInactiveAsync(id); return RedirectToAction(nameof(Index)); }

    [HttpPost]
    public async Task<IActionResult> Recover(int id) { await _lessonService.RecoverAsync(id); return RedirectToAction(nameof(Index)); }

    [HttpPost]
    public async Task<IActionResult> Delete(int id) { await _lessonService.DeleteAsync(id); return RedirectToAction(nameof(Index)); }

    // Student view
    [AllowAnonymous]
    public async Task<IActionResult> Detail(int id)
    {
        var lesson = await _lessonService.GetByIdAsync(id);
        if (lesson == null) return NotFound();
        return View(lesson);
    }

    // Approver list
    [Authorize(Roles = "Admin,Content Approver")]
    public async Task<IActionResult> PendingApproval(string? name, int? gradeId, int? subjectId, int? chapterId, int page = 1)
    {
        const int pageSize = 10;
        var lessons = await _lessonService.SearchAsync(name, gradeId, subjectId, chapterId, "Pending", page, pageSize);
        var total = await _lessonService.CountAsync(name, gradeId, subjectId, chapterId, "Pending");
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.CurrentPage = page;
        return View(lessons);
    }

    [Authorize(Roles = "Admin,Content Approver")]
    [HttpPost]
    public async Task<IActionResult> Approve(int id)
    {
        var lesson = await _lessonService.GetByIdAsync(id);
        if (lesson != null) { lesson.Status = "Active"; await _lessonService.UpdateAsync(lesson); }
        return RedirectToAction(nameof(PendingApproval));
    }

    [Authorize(Roles = "Admin,Content Approver")]
    [HttpPost]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var lesson = await _lessonService.GetByIdAsync(id);
        if (lesson != null) { lesson.Status = "Rejected"; await _lessonService.UpdateAsync(lesson); }
        return RedirectToAction(nameof(PendingApproval));
    }
}
