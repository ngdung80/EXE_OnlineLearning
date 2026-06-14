using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin,Content Manager,Content Approver")]
public class QuestionController : Controller
{
    private readonly IQuestionService _questionService;
    private readonly ILessonService _lessonService;

    public QuestionController(IQuestionService questionService, ILessonService lessonService)
    {
        _questionService = questionService;
        _lessonService = lessonService;
    }

    public async Task<IActionResult> Index(string? search, int? lessonId, string? level, string? status, int page = 1)
    {
        const int pageSize = 10;
        var questions = await _questionService.SearchAsync(search, lessonId, level, status, page, pageSize);
        var total = await _questionService.CountAsync(search, lessonId, level, status);

        ViewBag.Lessons = await _lessonService.GetAllAsync();
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
        question.IsMultipleChoice = true; // Default
        
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
}
