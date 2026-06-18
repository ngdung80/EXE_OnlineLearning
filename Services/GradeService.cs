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
    /// <summary>Lấy danh sách Grade mà các học sinh con của Parent đã có gói học active.</summary>
    Task<List<Grade>> GetPurchasedByParentAsync(int parentId);
    /// <summary>Lấy danh sách Grade mà Student đã được mua gói học active.</summary>
    Task<List<Grade>> GetPurchasedByStudentAsync(int studentId);
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

    public async Task<List<Grade>> GetPurchasedByParentAsync(int parentId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Lấy tất cả StudentId là con của parent này
        var childIds = await _db.Users
            .Where(u => u.ParentId == parentId && !u.Deleted)
            .Select(u => u.UserId)
            .ToListAsync();

        if (!childIds.Any()) return new List<Grade>();

        // Lấy distinct GradeId từ StudentPackage active, chưa hết hạn, của các con
        var gradeIds = await _db.StudentPackages
            .Where(sp => childIds.Contains(sp.StudentId)
                      && sp.Status == "Active"
                      && sp.GradeId != null
                      && sp.EndDate >= today)
            .Select(sp => sp.GradeId!.Value)
            .Distinct()
            .ToListAsync();

        if (!gradeIds.Any()) return new List<Grade>();

        return await _db.Grades
            .Where(g => gradeIds.Contains(g.GradeId) && g.Status == "Active")
            .OrderBy(g => g.GradeName)
            .ToListAsync();
    }

    public async Task<List<Grade>> GetPurchasedByStudentAsync(int studentId)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        var gradeIds = await _db.StudentPackages
            .Where(sp => sp.StudentId == studentId
                      && sp.Status == "Active"
                      && sp.GradeId != null
                      && sp.EndDate >= today)
            .Select(sp => sp.GradeId!.Value)
            .Distinct()
            .ToListAsync();

        if (!gradeIds.Any()) return new List<Grade>();

        return await _db.Grades
            .Where(g => gradeIds.Contains(g.GradeId) && g.Status == "Active")
            .OrderBy(g => g.GradeName)
            .ToListAsync();
    }
}
