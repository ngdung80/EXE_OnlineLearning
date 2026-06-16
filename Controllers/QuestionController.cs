using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin,Content Manager,Content Approver")]
public class QuestionController : Controller
{
    private readonly IQuestionService _questionService;
    private readonly ILessonService _lessonService;
    private readonly AppDbContext _db;

    public QuestionController(IQuestionService questionService, ILessonService lessonService, AppDbContext db)
    {
        _questionService = questionService;
        _lessonService = lessonService;
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, int? lessonId, string? level, string? status, int page = 1)
    {
        const int pageSize = 10;
        var questions = await _questionService.SearchAsync(search, lessonId, level, status, page, pageSize);
        var total = await _questionService.CountAsync(search, lessonId, level, status);

        ViewBag.Lessons = await _lessonService.GetAllAsync();
        ViewBag.Subjects = await _db.Subjects.Where(s => s.Status == "Active").ToListAsync();
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Filter = new { search, lessonId, level, status };

        return View(questions);
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpGet]
    public async Task<IActionResult> Create(int? lessonId)
    {
        ViewBag.Lessons = await _lessonService.GetAllAsync();
        ViewBag.PreselectedLessonId = lessonId;
        return View();
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Create(Question question)
    {
        question.Status = "Active";
        
        await _questionService.InsertAsync(question);
        TempData["Success"] = "Question added successfully.";
        return RedirectToAction(nameof(Index), new { lessonId = question.LessonId });
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var question = await _questionService.GetByIdAsync(id);
        if (question == null) return NotFound();
        ViewBag.Lessons = await _lessonService.GetAllAsync();
        return View(question);
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Edit(Question question)
    {
        var existing = await _questionService.GetByIdAsync(question.QuestionId);
        if (existing == null) return NotFound();

        existing.QuestionContent = question.QuestionContent;
        existing.Answer = question.Answer;
        existing.CorrectAnswer = question.CorrectAnswer;
        existing.Level = question.Level;
        existing.LessonId = question.LessonId;
        existing.Status = question.Status;
        existing.IsMultipleChoice = question.IsMultipleChoice;

        await _questionService.UpdateAsync(existing);
        TempData["Success"] = "Question updated successfully.";
        return RedirectToAction(nameof(Index), new { lessonId = existing.LessonId });
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _questionService.DeleteAsync(id);
        TempData["Success"] = "Question deleted.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin,Content Manager")]
    [HttpPost]
    public async Task<IActionResult> CreateTestFromQuestions(string testName, int duration, string types, int subjectId, int? lessonId, List<int> questionIds)
    {
        if (string.IsNullOrEmpty(testName) || duration <= 0 || questionIds == null || !questionIds.Any())
        {
            TempData["Error"] = "Vui lòng điền đầy đủ thông tin và chọn ít nhất một câu hỏi!";
            return RedirectToAction(nameof(Index));
        }

        var test = new Test
        {
            TestName = testName,
            Duration = duration,
            Types = types,
            SubjectId = subjectId,
            LessonId = lessonId > 0 ? lessonId : null,
            Status = "Active",
            CreatedAt = DateTime.Now
        };

        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        foreach (var qId in questionIds)
        {
            _db.TestQuestions.Add(new TestQuestion { TestId = test.TestId, QuestionId = qId });
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Tạo bài kiểm tra '{testName}' thành công với {questionIds.Count} câu hỏi!";
        return RedirectToAction(nameof(Index));
    }
}
