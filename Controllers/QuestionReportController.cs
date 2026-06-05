using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin,Content Manager,Content Approver")]
public class QuestionReportController : Controller
{
    private readonly IQuestionReportService _reportService;
    private readonly IQuestionService _questionService;

    public QuestionReportController(IQuestionReportService reportService, IQuestionService questionService)
    {
        _reportService = reportService;
        _questionService = questionService;
    }

    public async Task<IActionResult> Index(string? status, int page = 1)
    {
        const int pageSize = 10;
        var reports = await _reportService.GetAllAsync(status, page, pageSize);
        var total = await _reportService.CountAsync(status);
        
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.SelectedStatus = status;

        return View(reports);
    }

    [HttpPost]
    public async Task<IActionResult> Review(int id, string status, string? reviewNote)
    {
        var reviewerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _reportService.ReviewAsync(id, status, reviewNote, reviewerId);
        
        TempData["Success"] = "Report reviewed successfully.";
        return RedirectToAction(nameof(Index), new { status });
    }

    [HttpPost]
    public async Task<IActionResult> EditQuestion(int reportId, int questionId, string questionContent, string answer, string correctAnswer)
    {
        var question = await _questionService.GetByIdAsync(questionId);
        if (question == null) return NotFound();

        question.QuestionContent = questionContent;
        question.Answer = answer;
        question.CorrectAnswer = correctAnswer;
        await _questionService.UpdateAsync(question);

        var reviewerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        await _reportService.ReviewAsync(reportId, "Resolved", $"Question updated: {questionContent}", reviewerId);

        TempData["Success"] = "Question fixed and report resolved!";
        return RedirectToAction(nameof(Index));
    }
}
