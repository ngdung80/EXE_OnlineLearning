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

            List<Lesson> lessons = new List<Lesson>();
            if (req.LessonId.HasValue && req.LessonId.Value > 0)
            {
                var targetLesson = await _lessonService.GetByIdAsync(req.LessonId.Value);
                if (targetLesson != null) lessons.Add(targetLesson);
            }
            else
            {
                var chapters = await _chapterService.GetBySubjectIdAsync(req.SubjectId);
                foreach (var ch in chapters)
                    lessons.AddRange(await _lessonService.GetByChapterIdAsync(ch.ChapterId));
            }

            if (!lessons.Any()) throw new Exception("Không tìm thấy bài học nào.");

            var practice = new Test
            {
                TestName = req.PracticeName ?? $"Luyện tập AI {DateTime.Now:yyyyMMddHHmm}",
                SubjectId = req.SubjectId,
                Duration = req.Duration,
                Status = "Active",
                Types = "Practice",
                StudentId = studentId
            };
            var practiceId = await _testService.InsertAsync(practice);

            var questions = new List<Question>();
            
            // Build the single batch prompt to retrieve all questions in one go
            var prompt = BuildBatchPrompt(lessons, req.RememberCount, req.UnderstandCount, req.ApplyCount, req.AdditionalRequirements, subject);
            
            var aiResponse = await _geminiService.GenerateContentAsync(prompt);
            var cleanJsonString = CleanJson(aiResponse);
            
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var aiQuestions = JsonSerializer.Deserialize<List<AIQuestionDto>>(cleanJsonString, jsonOptions);

            if (aiQuestions != null)
            {
                foreach (var aiQ in aiQuestions)
                {
                    if (string.IsNullOrEmpty(aiQ.QuestionContent) || aiQ.Options == null || aiQ.Options.Count < 4 || string.IsNullOrEmpty(aiQ.CorrectAnswer))
                        continue;

                    if (await _questionService.ContentExistsAsync(aiQ.QuestionContent))
                        continue;

                    var question = new Question
                    {
                        QuestionContent = aiQ.QuestionContent,
                        LessonId = lessons.First().LessonId,
                        Status = "Active",
                        IsMultipleChoice = true,
                        Answer = string.Join(", ", aiQ.Options),
                        CorrectAnswer = aiQ.CorrectAnswer,
                        Level = aiQ.Level ?? "Remember"
                    };

                    var qId = await _questionService.InsertAsync(question);
                    questions.Add(question);
                    await _testService.AddQuestionToTestAsync(practiceId, qId);
                }
            }

            return Json(new { message = $"Successfully created {questions.Count} questions.", practiceId });
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { message = GetFriendlyAIErrorMessage(ex) });
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
            Console.WriteLine("\n=== GEMINI API ERROR ===");
            Console.WriteLine(ex.Message);
            Console.WriteLine("========================\n");
            return Json(new { error = GetFriendlyAIErrorMessage(ex) });
        }
    }

    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> GenerateSingleQuestion([FromBody] SingleQuestionRequest req)
    {
        Response.ContentType = "application/json";
        try
        {
            var lesson = await _lessonService.GetByIdAsync(req.LessonId);
            if (lesson == null) throw new Exception("Bài học không tồn tại.");

            var subject = lesson.Chapter?.Subject;
            if (subject == null) throw new Exception("Môn học không tồn tại.");

            var sb = new StringBuilder();
            sb.Append($"Create a single fun multiple-choice question in Vietnamese for the subject '{subject.SubjectName}' based on the video source and description of this lesson:\n");
            sb.Append($"- Lesson Name: {lesson.LessonName}\n");
            sb.Append($"- Video Source: {lesson.FileUrl ?? "No video link"}\n");
            sb.Append($"- Lesson Description/Content: {lesson.ContentText ?? "No description available"}\n");
            sb.Append("\nThe question should test the child's understanding of the lesson and video materials, be friendly, and suitable for children.\n");
            sb.Append("You MUST return the response strictly as a JSON object (no markdown code blocks, no explanations outside the JSON object).\n");
            sb.Append("The JSON object must have the following exact keys:\n");
            sb.Append("- \"questionContent\": (string) the text of the question\n");
            sb.Append("- \"options\": (array of 4 strings) containing option prefix (A, B, C, D). Example: [\"A) Táo\", \"B) Cam\", \"C) Chuối\", \"D) Dâu\"]\n");
            sb.Append("- \"correctAnswer\": (string) the exact string representing the correct option. Example: \"A) Táo\"\n");
            sb.Append("- \"explanation\": (string) a short, encouraging explanation in Vietnamese telling the child why that option is correct.\n");

            var prompt = sb.ToString();
            var aiResponse = await _geminiService.GenerateContentAsync(prompt);
            var cleanJsonString = CleanJson(aiResponse);

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var questionDto = JsonSerializer.Deserialize<AISingleQuestionDto>(cleanJsonString, jsonOptions);

            if (questionDto == null || string.IsNullOrEmpty(questionDto.QuestionContent) || questionDto.Options == null || questionDto.Options.Count < 4 || string.IsNullOrEmpty(questionDto.CorrectAnswer))
            {
                throw new Exception("Không thể phân tích câu hỏi do AI tạo ra. Vui lòng thử lại!");
            }

            return Json(questionDto);
        }
        catch (Exception ex)
        {
            Response.StatusCode = 400;
            return Json(new { error = GetFriendlyAIErrorMessage(ex) });
        }
    }

    private string BuildBatchPrompt(List<Lesson> lessons, int rememberCount, int understandCount, int applyCount, string? additionalReqs, Data.Entities.Subject subject)
    {
        var sb = new StringBuilder();
        sb.Append($"Create multiple-choice questions for the subject '{subject.SubjectName}'.\n");
        sb.Append("The questions should cover content from the following lessons:\n");
        foreach (var l in lessons)
            sb.Append($"- Lesson: {l.LessonName}  Content: {l.ContentText ?? "No content"}\n");
            
        sb.Append($"\nGenerate a total of {rememberCount + understandCount + applyCount} questions:\n");
        sb.Append($"- {rememberCount} question(s) with level 'Remember'\n");
        sb.Append($"- {understandCount} question(s) with level 'Understand'\n");
        sb.Append($"- {applyCount} question(s) with level 'Apply'\n");
        
        if (!string.IsNullOrWhiteSpace(additionalReqs)) 
            sb.Append($"Additional guidelines: {additionalReqs}\n");
            
        sb.Append("\nYou MUST return the response strictly as a JSON array of objects. Do not include any explanations or extra text outside the JSON array.\n");
        sb.Append("Each object in the array must have the following exact keys:\n");
        sb.Append("- \"questionContent\": (string) the text of the question\n");
        sb.Append("- \"options\": (array of 4 strings) containing option prefix (A, B, C, D). Example: [\"A) Option text\", \"B) Option text\", \"C) Option text\", \"D) Option text\"]\n");
        sb.Append("- \"correctAnswer\": (string) the exact string representing the correct option. Example: \"B) Option text\"\n");
        sb.Append("- \"level\": (string) either \"Remember\", \"Understand\", or \"Apply\"\n");
        
        return sb.ToString();
    }

    private string CleanJson(string text)
    {
        text = text.Trim();
        if (text.StartsWith("```json"))
        {
            text = text["```json".Length..].Trim();
        }
        else if (text.StartsWith("```"))
        {
            text = text["```".Length..].Trim();
        }
        if (text.EndsWith("```"))
        {
            text = text[..^"```".Length].Trim();
        }
        return text;
    }

    private string GetFriendlyAIErrorMessage(Exception ex)
    {
        var msg = ex.Message;
        if (msg.Contains("429") || msg.Contains("RESOURCE_EXHAUSTED") || msg.Contains("quota"))
        {
            return "⚠️ Hệ thống AI đang tạm thời quá tải hoặc hết lượt dùng (quota) miễn phí trong ngày. Con vui lòng đợi một chút rồi thử lại nhé!";
        }
        if (msg.Contains("503") || msg.Contains("Unavailable"))
        {
            return "☁️ Máy chủ AI của Google đang bận hoặc bảo trì tạm thời. Con vui lòng thử lại sau vài giây nhé!";
        }
        if (msg.Contains("401") || msg.Contains("403") || msg.Contains("API_KEY"))
        {
            return "🔒 Lỗi xác thực API Key của AI. Quản trị viên vui lòng kiểm tra lại cấu hình.";
        }
        if (msg.Contains("timeout") || msg.Contains("canceled") || msg.Contains("HttpClient"))
        {
            return "⏳ Kết nối tới máy chủ AI bị quá thời gian phản hồi (Timeout). Vui lòng kiểm tra lại mạng hoặc thử lại sau.";
        }
        return "🤖 Trợ lý AI đang gặp sự cố nhỏ khi tạo câu hỏi. Con thử lại nhé!";
    }
}

public class AIQuestionDto
{
    public string QuestionContent { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = "";
    public string Level { get; set; } = "";
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
    public int? LessonId { get; set; }
}

public class ChatRequest { public string Message { get; set; } = ""; }

public class SingleQuestionRequest
{
    public int LessonId { get; set; }
}

public class AISingleQuestionDto
{
    public string QuestionContent { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = "";
    public string Explanation { get; set; } = "";
}
