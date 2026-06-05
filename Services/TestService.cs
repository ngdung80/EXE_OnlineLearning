using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface ITestService
{
    Task<List<Test>> GetByStudentIdAsync(int studentId);
    Task<List<Test>> GetPublishedTestsBySubjectIdAsync(int subjectId);
    Task<Test?> GetByIdAsync(int id);
    Task<int> InsertAsync(Test test);
    Task UpdateAsync(Test test);
    Task DeleteAsync(int id);
    Task<List<Test>> SearchAsync(string? name, int? subjectId, string? type, string? status, int page, int pageSize);
    Task<int> CountAsync(string? name, int? subjectId, string? type, string? status);
    Task AddQuestionToTestAsync(int testId, int questionId);
    Task RemoveQuestionFromTestAsync(int testId, int questionId);
    Task<List<TestQuestion>> GetTestQuestionsAsync(int testId);
}

public class TestService : ITestService
{
    private readonly AppDbContext _db;
    public TestService(AppDbContext db) => _db = db;

    public async Task<List<Test>> GetByStudentIdAsync(int studentId)
        => await _db.Tests.Where(t => t.StudentId == studentId).Include(t => t.Subject).ToListAsync();

    public async Task<List<Test>> GetPublishedTestsBySubjectIdAsync(int subjectId)
        => await _db.Tests.Where(t => t.SubjectId == subjectId && t.Status == "Active" && t.Types == "Test").ToListAsync();

    public async Task<Test?> GetByIdAsync(int id)
        => await _db.Tests.Include(t => t.TestQuestions).ThenInclude(tq => tq.Question).FirstOrDefaultAsync(t => t.TestId == id);

    public async Task<int> InsertAsync(Test test)
    {
        test.CreatedAt = DateTime.Now;
        _db.Tests.Add(test);
        await _db.SaveChangesAsync();
        return test.TestId;
    }

    public async Task UpdateAsync(Test test) { _db.Tests.Update(test); await _db.SaveChangesAsync(); }

    public async Task DeleteAsync(int id)
    {
        var t = await _db.Tests.Include(t => t.TestQuestions).FirstOrDefaultAsync(t => t.TestId == id);
        if (t != null) { _db.TestQuestions.RemoveRange(t.TestQuestions); _db.Tests.Remove(t); await _db.SaveChangesAsync(); }
    }

    public async Task<List<Test>> SearchAsync(string? name, int? subjectId, string? type, string? status, int page, int pageSize)
    {
        var query = _db.Tests.Include(t => t.Subject).AsQueryable();
        if (!string.IsNullOrEmpty(name)) query = query.Where(t => t.TestName.Contains(name));
        if (subjectId.HasValue) query = query.Where(t => t.SubjectId == subjectId);
        if (!string.IsNullOrEmpty(type)) query = query.Where(t => t.Types == type);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        return await query.OrderByDescending(t => t.TestId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> CountAsync(string? name, int? subjectId, string? type, string? status)
    {
        var query = _db.Tests.AsQueryable();
        if (!string.IsNullOrEmpty(name)) query = query.Where(t => t.TestName.Contains(name));
        if (subjectId.HasValue) query = query.Where(t => t.SubjectId == subjectId);
        if (!string.IsNullOrEmpty(type)) query = query.Where(t => t.Types == type);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        return await query.CountAsync();
    }

    public async Task AddQuestionToTestAsync(int testId, int questionId)
    {
        _db.TestQuestions.Add(new TestQuestion { TestId = testId, QuestionId = questionId });
        await _db.SaveChangesAsync();
    }

    public async Task RemoveQuestionFromTestAsync(int testId, int questionId)
    {
        var tq = await _db.TestQuestions.FirstOrDefaultAsync(tq => tq.TestId == testId && tq.QuestionId == questionId);
        if (tq != null) { _db.TestQuestions.Remove(tq); await _db.SaveChangesAsync(); }
    }

    public async Task<List<TestQuestion>> GetTestQuestionsAsync(int testId)
        => await _db.TestQuestions.Where(tq => tq.TestId == testId).Include(tq => tq.Question).ToListAsync();
}
