using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface IPackageService
{
    Task<List<Package>> GetAllAsync();
    Task<List<Package>> GetActiveAsync();
    Task<Package?> GetByIdAsync(int id);
    Task AddAsync(Package pkg);
    Task UpdateAsync(Package pkg);
    Task DeleteAsync(int id);
}

public class PackageService : IPackageService
{
    private readonly AppDbContext _db;
    public PackageService(AppDbContext db) => _db = db;

    public async Task<List<Package>> GetAllAsync() => await _db.Packages.ToListAsync();
    public async Task<List<Package>> GetActiveAsync() => await _db.Packages.Where(p => p.Status == "Active").ToListAsync();
    public async Task<Package?> GetByIdAsync(int id) => await _db.Packages.FindAsync(id);

    public async Task AddAsync(Package pkg) { _db.Packages.Add(pkg); await _db.SaveChangesAsync(); }
    public async Task UpdateAsync(Package pkg) { _db.Packages.Update(pkg); await _db.SaveChangesAsync(); }
    public async Task DeleteAsync(int id) { var p = await _db.Packages.FindAsync(id); if (p != null) { _db.Packages.Remove(p); await _db.SaveChangesAsync(); } }
}

public interface IStudentPackageService
{
    Task<bool> HasActivePackageForGradeAsync(int studentId, int packageId, int gradeId);
    Task<int> InsertForGradeAsync(int studentId, int packageId, int gradeId, DateOnly startDate, DateOnly endDate, List<int> subjectIds);
    Task<List<StudentPackage>> GetByStudentIdAsync(int studentId);
    Task<bool> StudentHasAccessToSubjectAsync(int studentId, int subjectId);
}

public class StudentPackageService : IStudentPackageService
{
    private readonly AppDbContext _db;
    public StudentPackageService(AppDbContext db) => _db = db;

    public async Task<bool> HasActivePackageForGradeAsync(int studentId, int packageId, int gradeId)
        => await _db.StudentPackages.AnyAsync(sp =>
            sp.StudentId == studentId && sp.PackageId == packageId && sp.GradeId == gradeId &&
            sp.Status == "Active" && sp.EndDate >= DateOnly.FromDateTime(DateTime.Now));

    public async Task<int> InsertForGradeAsync(int studentId, int packageId, int gradeId, DateOnly startDate, DateOnly endDate, List<int> subjectIds)
    {
        int lastId = 0;
        foreach (var subjectId in subjectIds)
        {
            var sp = new StudentPackage
            {
                StudentId = studentId,
                PackageId = packageId,
                GradeId = gradeId,
                SubjectId = subjectId,
                StartDate = startDate,
                EndDate = endDate,
                Status = "Active"
            };
            _db.StudentPackages.Add(sp);
            await _db.SaveChangesAsync();
            lastId = sp.StudentPackageId;
        }
        return lastId;
    }

    public async Task<List<StudentPackage>> GetByStudentIdAsync(int studentId)
        => await _db.StudentPackages.Include(sp => sp.Package).Include(sp => sp.Subject).Include(sp => sp.Grade)
            .Where(sp => sp.StudentId == studentId).ToListAsync();

    public async Task<bool> StudentHasAccessToSubjectAsync(int studentId, int subjectId)
        => await _db.StudentPackages.AnyAsync(sp =>
            sp.StudentId == studentId && sp.SubjectId == subjectId &&
            sp.Status == "Active" && sp.EndDate >= DateOnly.FromDateTime(DateTime.Now));
}
