using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class AIController : Controller
{
    private readonly IGeminiService _geminiService;
    private readonly IQuestionService _questionService;
    private readonly ITestService _testService;
    private readonly ILessonService _lessonService;
    private readonly ISubjectService _subjectService;
    private readonly IChapterService _chapterService;
    private readonly IStudentPackageService _studentPackageService;

    public AIController(IGeminiService geminiService, IQuestionService questionService,
        ITestService testService, ILessonService lessonService, ISubjectService subjectService, 
        IChapterService chapterService, IStudentPackageService studentPackageService)
    {
        _geminiService = geminiService;
        _questionService = questionService;
        _testService = testService;
        _lessonService = lessonService;
        _subjectService = subjectService;
        _chapterService = chapterService;
        _studentPackageService = studentPackageService;
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GeneratePractice()
    {
        var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var activePackages = await _studentPackageService.GetByStudentIdAsync(studentId);
        
        var active = activePackages.Where(sp => sp.Status == "Active" && (sp.EndDate == null || sp.EndDate >= DateOnly.FromDateTime(DateTime.Now))).ToList();
        
        ViewBag.StudentPackages = active;
        return View();
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> GeneratePractice([FromBody] GeneratePracticeRequest req)
    {
        Response.ContentType = "application/json";
        try
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var subject = await _subjectService.GetByIdAsync(req.SubjectId);
            if (subject == null) throw new Exception("Subject does not exist.");

            var chapters = await _chapterService.GetBySubjectIdAsync(req.SubjectId);
            var lessons = new List<Lesson>();
            foreach (var ch in chapters)
                lessons.AddRange(await _lessonService.GetByChapterIdAsync(ch.ChapterId));

            if (!lessons.Any()) throw new Exception("No lessons found for this subject.");

            var existingQuestions = new List<Question>();
            foreach (var l in lessons)
                existingQuestions.AddRange(await _questionService.GetByLessonIdAsync(l.LessonId));

            var practice = new Test
            {
                TestName = req.PracticeName ?? $"AI Practice {DateTime.Now:yyyyMMddHHmm}",
                SubjectId = req.SubjectId,
                Duration = req.Duration,
                Status = "Active",
                Types = "Practice",
                StudentId = studentId
            };
            var practiceId = await _testService.InsertAsync(practice);

            var questions = new List<Question>();
            var levelCounts = new[] { (Level: "Remember", Count: req.RememberCount), (Level: "Understand", Count: req.UnderstandCount), (Level: "Apply", Count: req.ApplyCount) };

            foreach (var (level, count) in levelCounts)
            {
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var prompt = BuildPrompt(level, lessons, existingQuestions, req.AdditionalRequirements, subject);
                        var aiResponse = await _geminiService.GenerateContentAsync(prompt);
                        var question = ParseAIResponse(aiResponse, lessons.First().LessonId, level);

                        if (question != null && !await _questionService.ContentExistsAsync(question.QuestionContent))
                        {
                            var qId = await _questionService.InsertAsync(question);
                            questions.Add(question);
                            await _testService.AddQuestionToTestAsync(practiceId, qId);
                        }
                    }
                    catch (Exception ex) when (ex.Message.Contains("429"))
                    {
                        await Task.Delay(32000);
                        i--;
                    }
                }
            }

            return Json(new { message = $"Successfully created {questions.Count} questions.", practiceId });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { message = ex.Message });
        }
    }

    public IActionResult Chat() => View();

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest req)
    {
        try
        {
            var response = await _geminiService.GenerateContentAsync(req.Message);
            return Json(new { response });
        }
        catch (Exception ex)
        {
            return Json(new { error = ex.Message });
        }
    }

    private string BuildPrompt(string level, List<Lesson> lessons, List<Question> existingQuestions, string? additionalReqs, Data.Entities.Subject subject)
    {
        var sb = new StringBuilder();
        sb.Append($"Create a new multiple-choice question for the subject '{subject.SubjectName}' with difficulty level '{level}' (Bloom's Taxonomy: Remember, Understand, Apply).\n");
        sb.Append("The question should cover content from the following lessons:\n");
        foreach (var l in lessons)
            sb.Append($"- Lesson: {l.LessonName}  Content: {l.ContentText ?? "No content"}\n");
        sb.Append("\nExisting questions (avoid duplication):\n");
        if (!existingQuestions.Any()) sb.Append("None.\n");
        else foreach (var q in existingQuestions) sb.Append($"- {q.QuestionContent}\n");
        if (!string.IsNullOrWhiteSpace(additionalReqs)) sb.Append($"Additional: {additionalReqs}\n");
        sb.Append("\nFORMAT:\nQuestion: [content]\nOptions: A) [opt1], B) [opt2], C) [opt3], D) [opt4]\nCorrect: [exact option text]\n");
        return sb.ToString();
    }

    private Question? ParseAIResponse(string result, int lessonId, string level)
    {
        string questionContent = "", answer = "", correctAnswer = "";
        foreach (var line in result.Split('\n').Select(l => l.Trim()))
        {
            if (line.StartsWith("Question: ")) questionContent = line["Question: ".Length..].Trim();
            else if (line.StartsWith("Options: ")) answer = line["Options: ".Length..].Trim();
            else if (line.StartsWith("Correct: ")) correctAnswer = line["Correct: ".Length..].Trim();
        }
        if (string.IsNullOrEmpty(questionContent) || string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(correctAnswer)) return null;
        var options = answer.Split(',').Select(o => o.Trim()).Where(o => !string.IsNullOrEmpty(o)).ToArray();
        if (options.Length < 4) return null;
        if (!options.Any(o => o.Equals(correctAnswer, StringComparison.OrdinalIgnoreCase))) return null;
        return new Question { QuestionContent = questionContent, LessonId = lessonId, Status = "Active", IsMultipleChoice = true, Answer = answer, CorrectAnswer = correctAnswer, Level = level };
    }
}

public class GeneratePracticeRequest
{
    public string? PracticeName { get; set; }
    public int Duration { get; set; }
    public int SubjectId { get; set; }
    public int GradeId { get; set; }
    public int RememberCount { get; set; }
    public int UnderstandCount { get; set; }
    public int ApplyCount { get; set; }
    public string? AdditionalRequirements { get; set; }
}

public class ChatRequest { public string Message { get; set; } = ""; }
