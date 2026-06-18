using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Student,Parent")]
public class TestController : Controller
{
    private readonly ITestService _testService;
    private readonly ITestAttemptService _testAttemptService;
    private readonly ISubjectService _subjectService;
    private readonly IQuestionReportService _questionReportService;
    private readonly AppDbContext _db;

    public TestController(ITestService testService, ITestAttemptService testAttemptService, 
        ISubjectService subjectService, IQuestionReportService questionReportService, AppDbContext db)
    {
        _testService = testService;
        _testAttemptService = testAttemptService;
        _subjectService = subjectService;
        _questionReportService = questionReportService;
        _db = db;
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Report(int questionId, int attemptId, string reason)
    {
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _questionReportService.AddAsync(questionId, studentId, reason);
        TempData["Success"] = "Thank you! The question report has been submitted.";
        return RedirectToAction(nameof(Result), new { attemptId });
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ReportAjax([FromBody] ReportRequest req)
    {
        if (req == null || req.QuestionId <= 0 || string.IsNullOrWhiteSpace(req.Reason))
        {
            return Json(new { success = false, message = "Thông tin gửi không hợp lệ." });
        }
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _questionReportService.AddAsync(req.QuestionId, studentId, req.Reason);
        return Json(new { success = true, message = "Cảm ơn con! Báo cáo lỗi của câu hỏi đã được gửi đến thầy cô biên tập." });
    }

    public class ReportRequest
    {
        public int QuestionId { get; set; }
        public string Reason { get; set; } = null!;
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> List(int? subjectId, string? type, int page = 1)
    {
        const int pageSize = 10;
        ViewBag.Subjects = await _subjectService.GetAllAsync();
        var tests = await _testService.SearchAsync(null, subjectId, type, "Active", page, pageSize);
        var total = await _testService.CountAsync(null, subjectId, type, "Active");
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.CurrentPage = page;
        return View(tests);
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Take(int id)
    {
        var test = await _testService.GetByIdAsync(id);
        if (test == null) return NotFound();
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var attemptId = await _testAttemptService.StartAttemptAsync(id, studentId);
        return View(new TakeTestViewModel { Test = test, AttemptId = attemptId });
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Submit(int attemptId, IFormCollection form)
    {
        var answers = new Dictionary<int, string>();
        foreach (var key in form.Keys)
        {
            if (key.StartsWith("q_") && int.TryParse(key[2..], out int qId))
                answers[qId] = form[key].ToString();
        }
        await _testAttemptService.SaveResultAsync(attemptId, answers);
        return RedirectToAction(nameof(Result), new { attemptId });
    }

    [Authorize(Roles = "Student,Parent")]
    public async Task<IActionResult> Result(int attemptId)
    {
        var attempt = await _testAttemptService.GetWithResultsAsync(attemptId);
        if (attempt == null) return NotFound();

        // Security ownership verification
        if (User.IsInRole("Parent"))
        {
            var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var isLinked = await _db.Users.AnyAsync(u => u.UserId == attempt.StudentId && u.ParentId == parentId && !u.Deleted);
            if (!isLinked)
            {
                return Forbid();
            }
        }
        else if (User.IsInRole("Student"))
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (attempt.StudentId != studentId)
            {
                return Forbid();
            }
        }

        return View(attempt);
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> History()
    {
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var attempts = await _testAttemptService.GetByStudentIdAsync(studentId);
        return View(attempts);
    }

    [HttpGet]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Practice()
    {
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var grades = await _db.Grades.Where(g => g.Status == "Active").ToListAsync();
        var subjects = await _db.Subjects.Where(s => s.Status == "Active").ToListAsync();
        var chapters = await _db.Chapters.Where(c => c.Status == "Active").ToListAsync();
        var lessons = await _db.Lessons.Where(l => l.Status == "Active").ToListAsync();

        var completedLessonIds = await _db.StudentLessonProgresses
            .Where(p => p.StudentId == studentId)
            .Select(p => p.LessonId)
            .ToListAsync();

        ViewBag.Grades = grades;
        ViewBag.Subjects = subjects;
        ViewBag.Chapters = chapters;
        ViewBag.Lessons = lessons;
        ViewBag.CompletedLessonIds = completedLessonIds;

        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StartPractice(int lessonId)
    {
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Verify that lesson is completed
        var isCompleted = await _db.StudentLessonProgresses.AnyAsync(p => p.StudentId == studentId && p.LessonId == lessonId);
        if (!isCompleted)
        {
            TempData["Error"] = "Con phải hoàn thành bài học này trước để mở khóa luyện tập nhé!";
            return RedirectToAction(nameof(Practice));
        }

        // Get lesson details
        var lesson = await _db.Lessons.Include(l => l.Chapter).FirstOrDefaultAsync(l => l.LessonId == lessonId);
        if (lesson == null) return NotFound();

        // Get pre-created active questions for this lesson
        var questions = await _db.Questions.Where(q => q.LessonId == lessonId && q.Status == "Active").ToListAsync();
        if (questions.Count == 0)
        {
            TempData["Error"] = "Bài học này chưa có câu hỏi luyện tập nào được thầy cô tạo sẵn. Con hãy quay lại sau nhé!";
            return RedirectToAction(nameof(Practice));
        }

        // Create a practice test
        var test = new Test
        {
            TestName = $"Luyện tập: {lesson.LessonName}",
            SubjectId = lesson.Chapter.SubjectId,
            LessonId = lessonId,
            Duration = 15,
            Status = "Active",
            Types = "Practice",
            StudentId = studentId
        };
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        foreach (var q in questions)
        {
            _db.TestQuestions.Add(new TestQuestion { TestId = test.TestId, QuestionId = q.QuestionId });
        }
        await _db.SaveChangesAsync();

        // Start attempt and redirect to Take
        return RedirectToAction(nameof(Take), new { id = test.TestId });
    }
}

public class TakeTestViewModel
{
    public Test Test { get; set; } = null!;
    public int AttemptId { get; set; }
}
