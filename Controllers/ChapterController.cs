using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class ChapterController : Controller
{
    private readonly IChapterService _chapterService;
    private readonly ISubjectService _subjectService;
    private readonly IGradeService _gradeService;
    private readonly IStudentPackageService _studentPackageService;

    public ChapterController(IChapterService chapterService, ISubjectService subjectService, IGradeService gradeService, IStudentPackageService studentPackageService)
    {
        _chapterService = chapterService;
        _subjectService = subjectService;
        _gradeService = gradeService;
        _studentPackageService = studentPackageService;
    }

    public async Task<IActionResult> Index(int? subjectId)
    {
        var isAdminOrManager = User.IsInRole("Admin") || User.IsInRole("Content Manager");
        var allSubjects = await _subjectService.GetAllAsync();
        
        List<Subject> allowedSubjects = allSubjects;
        
        if (User.IsInRole("Student"))
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var today = DateOnly.FromDateTime(DateTime.Now);
            var activePackages = (await _studentPackageService.GetByStudentIdAsync(studentId))
                .Where(sp => sp.Status == "Active" && sp.EndDate >= today)
                .ToList();
            
            var activeGradeIds = activePackages
                .Where(sp => sp.GradeId.HasValue)
                .Select(sp => sp.GradeId!.Value)
                .Distinct()
                .ToList();

            if (!subjectId.HasValue)
            {
                if (activeGradeIds.Count == 0 || activeGradeIds.Count > 1)
                {
                    return RedirectToAction("Index", "Grade");
                }
            }

            var activeSubjectIds = activePackages
                .Where(sp => sp.SubjectId.HasValue)
                .Select(sp => sp.SubjectId!.Value)
                .Distinct()
                .ToList();
                
            allowedSubjects = allSubjects.Where(s => activeSubjectIds.Contains(s.SubjectId)).ToList();
        }

        ViewBag.Subjects = isAdminOrManager ? allSubjects : allowedSubjects.Where(s => s.Status == "Active").ToList();
        ViewBag.SelectedSubjectId = subjectId;
        
        List<Chapter> chapters = new List<Chapter>();

        if (subjectId.HasValue)
        {
            if (!isAdminOrManager && !allowedSubjects.Any(s => s.SubjectId == subjectId.Value))
            {
                return Forbid();
            }
            chapters = await _chapterService.GetBySubjectIdAsync(subjectId.Value);
        }
        else
        {
            if (isAdminOrManager)
            {
                chapters = await _chapterService.GetAllAsync();
            }
            else
            {
                var allowedSubjectIds = allowedSubjects.Select(s => s.SubjectId).ToList();
                var allActiveChapters = new List<Chapter>();
                foreach (var subId in allowedSubjectIds)
                {
                    var subChapters = await _chapterService.GetBySubjectIdAsync(subId);
                    allActiveChapters.AddRange(subChapters);
                }
                chapters = allActiveChapters;
            }
        }

        if (!isAdminOrManager)
        {
            chapters = chapters.Where(c => c.Status == "Active").ToList();
        }

        if (User.IsInRole("Student"))
        {
            var studentId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            if (subjectId.HasValue)
            {
                var hasAccess = await _studentPackageService.StudentHasAccessToSubjectAsync(studentId, subjectId.Value);
                if (!hasAccess)
                {
                    return RedirectToAction("AccessDenied", "Account");
                }
            }
            else
            {
                var studentPackages = await _studentPackageService.GetByStudentIdAsync(studentId);
                var activeSubjectIds = studentPackages
                    .Where(sp => sp.Status == "Active" && sp.EndDate >= DateOnly.FromDateTime(DateTime.Now))
                    .Select(sp => sp.SubjectId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();
                chapters = chapters.Where(c => activeSubjectIds.Contains(c.SubjectId)).ToList();
            }
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
