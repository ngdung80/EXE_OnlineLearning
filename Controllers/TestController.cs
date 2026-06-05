using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Student")]
public class TestController : Controller
{
    private readonly ITestService _testService;
    private readonly ITestAttemptService _testAttemptService;
    private readonly ISubjectService _subjectService;
    private readonly IQuestionReportService _questionReportService;

    public TestController(ITestService testService, ITestAttemptService testAttemptService, 
        ISubjectService subjectService, IQuestionReportService questionReportService)
    {
        _testService = testService;
        _testAttemptService = testAttemptService;
        _subjectService = subjectService;
        _questionReportService = questionReportService;
    }

    [HttpPost]
    public async Task<IActionResult> Report(int questionId, int attemptId, string reason)
    {
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _questionReportService.AddAsync(questionId, studentId, reason);
        TempData["Success"] = "Thank you! The question report has been submitted.";
        return RedirectToAction(nameof(Result), new { attemptId });
    }

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

    public async Task<IActionResult> Take(int id)
    {
        var test = await _testService.GetByIdAsync(id);
        if (test == null) return NotFound();
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var attemptId = await _testAttemptService.StartAttemptAsync(id, studentId);
        return View(new TakeTestViewModel { Test = test, AttemptId = attemptId });
    }

    [HttpPost]
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

    public async Task<IActionResult> Result(int attemptId)
    {
        var attempt = await _testAttemptService.GetWithResultsAsync(attemptId);
        if (attempt == null) return NotFound();
        return View(attempt);
    }

    public async Task<IActionResult> History()
    {
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var attempts = await _testAttemptService.GetByStudentIdAsync(studentId);
        return View(attempts);
    }
}

public class TakeTestViewModel
{
    public Test Test { get; set; } = null!;
    public int AttemptId { get; set; }
}
