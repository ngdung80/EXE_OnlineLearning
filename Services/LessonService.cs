using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface ILessonService
{
    Task<List<Lesson>> GetAllAsync();
    Task<List<Lesson>> GetByChapterIdAsync(int chapterId);
    Task<List<Lesson>> GetActiveBySubjectIdAsync(int subjectId);
    Task<Lesson?> GetByIdAsync(int id);
    Task<List<Lesson>> SearchAsync(string? name, int? gradeId, int? subjectId, int? chapterId, string? status, int page, int pageSize);
    Task<int> CountAsync(string? name, int? gradeId, int? subjectId, int? chapterId, string? status);
    Task AddAsync(Lesson lesson);
    Task UpdateAsync(Lesson lesson);
    Task SetInactiveAsync(int id);
    Task RecoverAsync(int id);
    Task DeleteAsync(int id);
    Task<bool> IsNameExistsAsync(string name, int chapterId, int excludeId);
}

public class LessonService : ILessonService
{
    private readonly AppDbContext _db;
    public LessonService(AppDbContext db) => _db = db;

    public async Task<List<Lesson>> GetAllAsync() => await _db.Lessons.ToListAsync();

    public async Task<List<Lesson>> GetByChapterIdAsync(int chapterId)
        => await _db.Lessons.Where(l => l.ChapterId == chapterId && l.Status == "Active").OrderBy(l => l.LessonId).ToListAsync();

    public async Task<List<Lesson>> GetActiveBySubjectIdAsync(int subjectId)
        => await _db.Lessons
            .Include(l => l.Chapter)
            .Where(l => l.Chapter.SubjectId == subjectId && l.Status == "Active")
            .ToListAsync();

    public async Task<Lesson?> GetByIdAsync(int id) => await _db.Lessons
        .Include(l => l.Chapter).ThenInclude(c => c.Subject).ThenInclude(s => s.Grade)
        .Include(l => l.Tests)
        .FirstOrDefaultAsync(l => l.LessonId == id);

    public async Task<List<Lesson>> SearchAsync(string? name, int? gradeId, int? subjectId, int? chapterId, string? status, int page, int pageSize)
    {
        var query = _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Subject).ThenInclude(s => s.Grade)
            .Where(l => l.DeletedDate == null).AsQueryable();
        if (!string.IsNullOrEmpty(name)) query = query.Where(l => l.LessonName.Contains(name));
        if (gradeId.HasValue) query = query.Where(l => l.Chapter.Subject.GradeId == gradeId);
        if (subjectId.HasValue) query = query.Where(l => l.Chapter.SubjectId == subjectId);
        if (chapterId.HasValue) query = query.Where(l => l.ChapterId == chapterId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);
        return await query.OrderBy(l => l.LessonId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> CountAsync(string? name, int? gradeId, int? subjectId, int? chapterId, string? status)
    {
        var query = _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Subject).ThenInclude(s => s.Grade)
            .Where(l => l.DeletedDate == null).AsQueryable();
        if (!string.IsNullOrEmpty(name)) query = query.Where(l => l.LessonName.Contains(name));
        if (gradeId.HasValue) query = query.Where(l => l.Chapter.Subject.GradeId == gradeId);
        if (subjectId.HasValue) query = query.Where(l => l.Chapter.SubjectId == subjectId);
        if (chapterId.HasValue) query = query.Where(l => l.ChapterId == chapterId);
        if (!string.IsNullOrEmpty(status)) query = query.Where(l => l.Status == status);
        return await query.CountAsync();
    }

    public async Task AddAsync(Lesson lesson) { _db.Lessons.Add(lesson); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Lesson lesson) { _db.Lessons.Update(lesson); await _db.SaveChangesAsync(); }

    public async Task SetInactiveAsync(int id)
    {
        var l = await _db.Lessons.FindAsync(id);
        if (l != null) { l.Status = "Inactive"; l.InactiveDate = DateTime.Now; await _db.SaveChangesAsync(); }
    }

    public async Task RecoverAsync(int id)
    {
        var l = await _db.Lessons.FindAsync(id);
        if (l != null && l.Status == "Inactive") { l.Status = "Active"; l.InactiveDate = null; await _db.SaveChangesAsync(); }
    }

    public async Task DeleteAsync(int id)
    {
        var l = await _db.Lessons.FindAsync(id);
        if (l != null && l.Status == "Inactive") { _db.Lessons.Remove(l); await _db.SaveChangesAsync(); }
    }

    public async Task<bool> IsNameExistsAsync(string name, int chapterId, int excludeId)
        => await _db.Lessons.AnyAsync(l => l.LessonName.ToLower() == name.ToLower() && l.ChapterId == chapterId && l.LessonId != excludeId && l.DeletedDate == null);
}
