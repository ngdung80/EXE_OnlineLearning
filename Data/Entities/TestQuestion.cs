namespace POT_System_ASPNET.Data.Entities;

public class TestQuestion
{
    public int TestQuestionId { get; set; }
    public int TestId { get; set; }
    public int QuestionId { get; set; }

    public Test Test { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
