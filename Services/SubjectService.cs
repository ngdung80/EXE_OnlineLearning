using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface ISubjectService
{
    Task<List<Subject>> GetAllAsync();
    Task<List<Subject>> GetByGradeIdAsync(int gradeId);
    Task<Subject?> GetByIdAsync(int id);
    Task<List<int>> GetSubjectIdsByGradeIdAsync(int gradeId);
    Task AddAsync(Subject subject);
    Task UpdateAsync(Subject subject);
    Task DeleteAsync(int id);
}

public class SubjectService : ISubjectService
{
    private readonly AppDbContext _db;
    public SubjectService(AppDbContext db) => _db = db;

    public async Task<List<Subject>> GetAllAsync() => await _db.Subjects.Include(s => s.Grade).ToListAsync();
    public async Task<List<Subject>> GetByGradeIdAsync(int gradeId) => await _db.Subjects.Where(s => s.GradeId == gradeId).ToListAsync();
    public async Task<Subject?> GetByIdAsync(int id) => await _db.Subjects.Include(s => s.Grade).FirstOrDefaultAsync(s => s.SubjectId == id);
    public async Task<List<int>> GetSubjectIdsByGradeIdAsync(int gradeId)
        => await _db.Subjects.Where(s => s.GradeId == gradeId && s.Status == "Active").Select(s => s.SubjectId).ToListAsync();

    public async Task AddAsync(Subject subject) { _db.Subjects.Add(subject); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Subject subject) { _db.Subjects.Update(subject); await _db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { var s = await _db.Subjects.FindAsync(id); if (s != null) { _db.Subjects.Remove(s); await _db.SaveChangesAsync(); } }
}
