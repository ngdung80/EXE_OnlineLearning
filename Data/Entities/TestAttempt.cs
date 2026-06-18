using System;
using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class TestAttempt
{
    public int AttemptId { get; set; }
    public int TestId { get; set; }
    public int StudentId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public double? Score { get; set; }
    public int? TotalQuestions { get; set; }
    public int? CorrectAnswers { get; set; }
    public string? Status { get; set; }

    public Test Test { get; set; } = null!;
    public User Student { get; set; } = null!;
    public ICollection<TestQuestionResult> TestQuestionResults { get; set; } = new List<TestQuestionResult>();
}
