using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class ChapterController : Controller
{
    private readonly IChapterService _chapterService;
    private readonly ISubjectService _subjectService;
    private readonly IGradeService _gradeService;

    public ChapterController(IChapterService chapterService, ISubjectService subjectService, IGradeService gradeService)
    {
        _chapterService = chapterService;
        _subjectService = subjectService;
        _gradeService = gradeService;
    }

    public async Task<IActionResult> Index(int? subjectId)
    {
        var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Content Manager");
        var allSubjects = await _subjectService.GetAllAsync();
        ViewBag.Subjects = isAdminOrManager ? allSubjects : allSubjects.Where(s => s.Status == "Active").ToList();
        ViewBag.SelectedSubjectId = subjectId;
        
        List<Chapter> chapters = subjectId.HasValue 
            ? await _chapterService.GetBySubjectIdAsync(subjectId.Value) 
            : await _chapterService.GetAllAsync();

        if (!isAdminOrManager)
        {
            chapters = chapters.Where(c => c.Status == "Active").ToList();
        }

        if (subjectId.HasValue)
        {
            var subject = await _subjectService.GetByIdAsync(subjectId.Value);
            ViewBag.Subject = subject;
        }

        return View(chapters);
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpGet]
    public async Task<IActionResult> Create(int? subjectId)
    {
        ViewBag.Subjects = await _subjectService.GetAllAsync();
        ViewBag.Grades = await _gradeService.GetAllAsync();
        ViewBag.SelectedSubjectId = subjectId;
        return View();
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Create(Chapter chapter)
    {
        await _chapterService.AddAsync(chapter);
        TempData["Success"] = "Chapter added successfully.";
        return RedirectToAction(nameof(Index), new { subjectId = chapter.SubjectId });
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var chapter = await _chapterService.GetByIdAsync(id);
        if (chapter == null) return NotFound();
        ViewBag.Subjects = await _subjectService.GetAllAsync();
        return View(chapter);
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Edit(Chapter chapter)
    {
        await _chapterService.UpdateAsync(chapter);
        TempData["Success"] = "Chapter updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _chapterService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public async Task<IActionResult> GetBySubject(int subjectId)
    {
        var chapters = await _chapterService.GetBySubjectIdAsync(subjectId);
        return Json(chapters.Select(c => new { c.ChapterId, c.ChapterName }));
    }
}
