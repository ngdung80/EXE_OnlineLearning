using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface IGradeService
{
    Task<List<Grade>> GetAllAsync();
    Task<List<Grade>> GetActiveAsync();
    Task<Grade?> GetByIdAsync(int id);
    Task AddAsync(Grade grade);
    Task UpdateAsync(Grade grade);
    Task DeleteAsync(int id);
}

public class GradeService : IGradeService
{
    private readonly AppDbContext _db;
    public GradeService(AppDbContext db) => _db = db;

    public async Task<List<Grade>> GetAllAsync() => await _db.Grades.ToListAsync();
    public async Task<List<Grade>> GetActiveAsync() => await _db.Grades.Where(g => g.Status == "Active").ToListAsync();
    public async Task<Grade?> GetByIdAsync(int id) => await _db.Grades.FindAsync(id);

    public async Task AddAsync(Grade grade) { _db.Grades.Add(grade); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Grade grade) { _db.Grades.Update(grade); await _db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id)
    {
        var g = await _db.Grades.FindAsync(id);
        if (g != null) { _db.Grades.Remove(g); await _db.SaveChangesAsync(); }
    }
}
