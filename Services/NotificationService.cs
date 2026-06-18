using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface INotificationService
{
    Task<List<Notification>> GetByUserIdAsync(int userId);
    Task<int> CountUnreadAsync(int userId);
    Task MarkReadAsync(int notificationId);
    Task AddAsync(int userId, string message);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    public NotificationService(AppDbContext db) => _db = db;

    public async Task<List<Notification>> GetByUserIdAsync(int userId)
        => await _db.Notifications.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedAt).ToListAsync();

    public async Task<int> CountUnreadAsync(int userId)
        => await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task MarkReadAsync(int notificationId)
    {
        var n = await _db.Notifications.FindAsync(notificationId);
        if (n != null) { n.IsRead = true; await _db.SaveChangesAsync(); }
    }

    public async Task AddAsync(int userId, string message)
    {
        _db.Notifications.Add(new Notification { UserId = userId, Message = message, IsRead = false, CreatedAt = DateTime.Now });
        await _db.SaveChangesAsync();
    }
}

public interface IQuestionReportService
{
    Task<List<QuestionReport>> GetAllAsync(string? status, int page, int pageSize);
    Task<int> CountAsync(string? status);
    Task<QuestionReport?> GetByIdAsync(int id);
    Task AddAsync(int questionId, int reportedBy, string reason);
    Task ReviewAsync(int reportId, string status, string? note, int reviewedBy);
}

public class QuestionReportService : IQuestionReportService
{
    private readonly AppDbContext _db;
    public QuestionReportService(AppDbContext db) => _db = db;

    public async Task<List<QuestionReport>> GetAllAsync(string? status, int page, int pageSize)
    {
        var query = _db.QuestionReports.Include(r => r.Question).Include(r => r.Reporter).AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
        return await query.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> CountAsync(string? status)
    {
        var query = _db.QuestionReports.AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
        return await query.CountAsync();
    }

    public async Task<QuestionReport?> GetByIdAsync(int id)
        => await _db.QuestionReports.Include(r => r.Question).Include(r => r.Reporter).FirstOrDefaultAsync(r => r.ReportId == id);

    public async Task AddAsync(int questionId, int reportedBy, string reason)
    {
        _db.QuestionReports.Add(new QuestionReport { QuestionId = questionId, ReportedBy = reportedBy, Reason = reason, Status = "Pending", CreatedAt = DateTime.Now });
        await _db.SaveChangesAsync();
    }

    public async Task ReviewAsync(int reportId, string status, string? note, int reviewedBy)
    {
        var r = await _db.QuestionReports.FindAsync(reportId);
        if (r != null) { r.Status = status; r.ReviewNote = note; r.ReviewedBy = reviewedBy; await _db.SaveChangesAsync(); }
    }
}

public interface IMentorService
{
    Task<List<MentorAssignment>> GetByStudentIdAsync(int studentId);
    Task AssignAsync(int mentorId, int studentId);
}

public class MentorService : IMentorService
{
    private readonly AppDbContext _db;
    public MentorService(AppDbContext db) => _db = db;

    public async Task<List<MentorAssignment>> GetByStudentIdAsync(int studentId)
        => await _db.MentorAssignments.Include(ma => ma.Mentor).Where(ma => ma.StudentId == studentId).ToListAsync();

    public async Task AssignAsync(int mentorId, int studentId)
    {
        _db.MentorAssignments.Add(new MentorAssignment { MentorId = mentorId, StudentId = studentId, AssignedDate = DateTime.Now, Status = "Active" });
        await _db.SaveChangesAsync();
    }
}
