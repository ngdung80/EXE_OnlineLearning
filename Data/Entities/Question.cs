using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Question
{
    public int QuestionId { get; set; }
    public string QuestionContent { get; set; } = null!;
    public int LessonId { get; set; }
    public string? Status { get; set; }
    public bool IsMultipleChoice { get; set; } = true;
    public string? Answer { get; set; }       // "A) opt1, B) opt2, C) opt3, D) opt4"
    public string? CorrectAnswer { get; set; }
    public string? Level { get; set; }         // Remember / Understand / Apply

    public Lesson Lesson { get; set; } = null!;
    public ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();
    public ICollection<QuestionReport> QuestionReports { get; set; } = new List<QuestionReport>();
}
