namespace POT_System_ASPNET.Data.Entities;

public class TestQuestionResult
{
    public int ResultId { get; set; }
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public string? SelectedAnswer { get; set; }
    public bool IsCorrect { get; set; }

    public TestAttempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
