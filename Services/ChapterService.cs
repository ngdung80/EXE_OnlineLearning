using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface IChapterService
{
    Task<List<Chapter>> GetAllAsync();
    Task<List<Chapter>> GetBySubjectIdAsync(int subjectId);
    Task<Chapter?> GetByIdAsync(int id);
    Task AddAsync(Chapter chapter);
    Task UpdateAsync(Chapter chapter);
    Task DeleteAsync(int id);
}

public class ChapterService : IChapterService
{
    private readonly AppDbContext _db;
    public ChapterService(AppDbContext db) => _db = db;

    public async Task<List<Chapter>> GetAllAsync() => await _db.Chapters.Include(c => c.Subject).ToListAsync();
    public async Task<List<Chapter>> GetBySubjectIdAsync(int subjectId)
        => await _db.Chapters.Where(c => c.SubjectId == subjectId).ToListAsync();
    public async Task<Chapter?> GetByIdAsync(int id) => await _db.Chapters.Include(c => c.Subject).FirstOrDefaultAsync(c => c.ChapterId == id);

    public async Task AddAsync(Chapter chapter) { _db.Chapters.Add(chapter); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Chapter chapter) { _db.Chapters.Update(chapter); await _db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { var c = await _db.Chapters.FindAsync(id); if (c != null) { _db.Chapters.Remove(c); await _db.SaveChangesAsync(); } }
}
