using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface IQuestionService
{
    Task<List<Question>> GetByLessonIdAsync(int lessonId);
    Task<Question?> GetByIdAsync(int id);
    Task<bool> ContentExistsAsync(string content);
    Task<int> InsertAsync(Question question);
    Task UpdateAsync(Question question);
    Task DeleteAsync(int id);
    Task<List<Question>> SearchAsync(string? content, int? lessonId, string? level, string? status, int page, int pageSize);
    Task<int> CountAsync(string? content, int? lessonId, string? level, string? status);
}

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _db;
    public QuestionService(AppDbContext db) => _db = db;

    public async Task<List<Question>> GetByLessonIdAsync(int lessonId)
        => await _db.Questions.Where(q => q.LessonId == lessonId).ToListAsync();

    public async Task<Question?> GetByIdAsync(int id) => await _db.Questions.FindAsync(id);

    public async Task<bool> ContentExistsAsync(string content)
        => await _db.Questions.AnyAsync(q => q.QuestionContent == content);

    public async Task<int> InsertAsync(Question question)
    {
        _db.Questions.Add(question);
        await _db.SaveChangesAsync();
        return question.QuestionId;
    }

    public async Task UpdateAsync(Question question) { _db.Questions.Update(question); await _db.SaveChangesAsync(); }

    public async Task DeleteAsync(int id)
    {
        var q = await _db.Questions.FindAsync(id);
        if (q != null) { _db.Questions.Remove(q); await _db.SaveChangesAsync(); }
    }

    public async Task<List<Question>> SearchAsync(string? content, int? lessonId, string? level, string? status, int page, int pageSize)
    {
        var query = _db.Questions.Include(q => q.Lesson).AsQueryable();
        if (!string.IsNullOrEmpty(content)) query = query.Where(q => q.QuestionContent.Contains(content));
        if (lessonId.HasValue) query = query.Where(q => q.LessonId == lessonId);
        if (!string.IsNullOrEmpty(level)) query = query.Where(q => q.Level == level);
        if (!string.IsNullOrEmpty(status)) query = query.Where(q => q.Status == status);
        return await query.OrderByDescending(q => q.QuestionId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> CountAsync(string? content, int? lessonId, string? level, string? status)
    {
        var query = _db.Questions.AsQueryable();
        if (!string.IsNullOrEmpty(content)) query = query.Where(q => q.QuestionContent.Contains(content));
        if (lessonId.HasValue) query = query.Where(q => q.LessonId == lessonId);
        if (!string.IsNullOrEmpty(level)) query = query.Where(q => q.Level == level);
        if (!string.IsNullOrEmpty(status)) query = query.Where(q => q.Status == status);
        return await query.CountAsync();
    }
}
