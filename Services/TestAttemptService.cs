using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface ITestAttemptService
{
    Task<int> StartAttemptAsync(int testId, int studentId);
    Task<TestAttempt?> GetByIdAsync(int attemptId);
    Task<List<TestAttempt>> GetByStudentIdAsync(int studentId);
    Task SaveResultAsync(int attemptId, Dictionary<int, string> answers);
    Task<TestAttempt?> GetWithResultsAsync(int attemptId);
}

public class TestAttemptService : ITestAttemptService
{
    private readonly AppDbContext _db;
    public TestAttemptService(AppDbContext db) => _db = db;

    public async Task<int> StartAttemptAsync(int testId, int studentId)
    {
        var attempt = new TestAttempt
        {
            TestId = testId,
            StudentId = studentId,
            StartTime = DateTime.Now,
            Status = "InProgress"
        };
        _db.TestAttempts.Add(attempt);
        await _db.SaveChangesAsync();
        return attempt.AttemptId;
    }

    public async Task<TestAttempt?> GetByIdAsync(int attemptId)
        => await _db.TestAttempts.Include(a => a.Test).ThenInclude(t => t.TestQuestions).ThenInclude(tq => tq.Question)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId);

    public async Task<List<TestAttempt>> GetByStudentIdAsync(int studentId)
        => await _db.TestAttempts.Include(a => a.Test).Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.StartTime).ToListAsync();

    public async Task SaveResultAsync(int attemptId, Dictionary<int, string> answers)
    {
        var attempt = await _db.TestAttempts
            .Include(a => a.Test).ThenInclude(t => t.TestQuestions).ThenInclude(tq => tq.Question)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
        if (attempt == null) return;

        var questions = attempt.Test.TestQuestions.Select(tq => tq.Question).ToList();
        int correct = 0;

        foreach (var q in questions)
        {
            var selected = answers.ContainsKey(q.QuestionId) ? answers[q.QuestionId] : null;
            bool isCorrect = selected != null && selected == q.CorrectAnswer;
            if (isCorrect) correct++;
            _db.TestQuestionResults.Add(new TestQuestionResult
            {
                AttemptId = attemptId,
                QuestionId = q.QuestionId,
                SelectedAnswer = selected,
                IsCorrect = isCorrect
            });
        }

        attempt.EndTime = DateTime.Now;
        attempt.TotalQuestions = questions.Count;
        attempt.CorrectAnswers = correct;
        attempt.Score = questions.Count > 0 ? Math.Round((double)correct / questions.Count * 10, 2) : 0;
        attempt.Status = "Completed";
        await _db.SaveChangesAsync();
    }

    public async Task<TestAttempt?> GetWithResultsAsync(int attemptId)
        => await _db.TestAttempts
            .Include(a => a.Test)
            .Include(a => a.TestQuestionResults).ThenInclude(r => r.Question)
            .FirstOrDefaultAsync(a => a.AttemptId == attemptId);
}
