using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class CertificateController : Controller
{
    private readonly AppDbContext _db;

    public CertificateController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? gradeId, string? studentName)
    {
        var grades = await _db.Grades.Where(g => g.Status == "Active").ToListAsync();
        ViewBag.Grades = grades;
        ViewBag.SelectedGradeId = gradeId;
        ViewBag.SearchName = studentName;

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Challenge();
        }
        int currentUserId = int.Parse(userIdClaim.Value);
        bool isStudent = User.IsInRole("Student");
        bool isParent = User.IsInRole("Parent");

        bool needsUpgrade = false;
        var linkedStudentIds = new List<int>();

        if (isStudent)
        {
            bool hasPremium = await _db.StudentPackages
                .AnyAsync(sp => sp.StudentId == currentUserId && sp.Package.Duration >= 180);
            if (!hasPremium)
            {
                needsUpgrade = true;
            }
            linkedStudentIds.Add(currentUserId);
        }
        else if (isParent)
        {
            var children = await _db.Users.Where(u => u.ParentId == currentUserId).ToListAsync();
            linkedStudentIds = children.Select(c => c.UserId).ToList();

            bool hasPremium = await _db.StudentPackages
                .AnyAsync(sp => linkedStudentIds.Contains(sp.StudentId) && sp.Package.Duration >= 180);
            if (!hasPremium)
            {
                needsUpgrade = true;
            }
        }

        ViewBag.NeedsUpgrade = needsUpgrade;
        var certificates = new List<CertificateViewModel>();

        if (!needsUpgrade)
        {
            var query = _db.StudentPackages
                .Include(sp => sp.Student)
                .Include(sp => sp.Grade)
                .Include(sp => sp.Package)
                .Where(sp => sp.Package.Duration >= 180 && sp.GradeId.HasValue)
                .AsQueryable();

            if (isStudent)
            {
                query = query.Where(sp => sp.StudentId == currentUserId);
            }
            else if (isParent)
            {
                query = query.Where(sp => linkedStudentIds.Contains(sp.StudentId));
            }

            if (gradeId.HasValue)
            {
                query = query.Where(sp => sp.GradeId == gradeId.Value);
            }
            if (!string.IsNullOrEmpty(studentName))
            {
                query = query.Where(sp => 
                    (sp.Student.FullName != null && sp.Student.FullName.Contains(studentName)) || 
                    sp.Student.Username.Contains(studentName));
            }

            var studentPackagesList = await query.ToListAsync();

            foreach (var sp in studentPackagesList)
            {
                var chapters = await _db.Chapters
                    .Where(c => c.Subject.GradeId == sp.GradeId && c.Status == "Active")
                    .ToListAsync();

                foreach (var ch in chapters)
                {
                    var totalLessons = await _db.Lessons.CountAsync(l => l.ChapterId == ch.ChapterId && l.Status == "Active");
                    if (totalLessons == 0) continue;

                    var completedLessons = await _db.StudentLessonProgresses.CountAsync(p => 
                        p.StudentId == sp.StudentId && 
                        p.Lesson.ChapterId == ch.ChapterId && 
                        p.Lesson.Status == "Active");

                    if (completedLessons >= totalLessons)
                    {
                        if (!certificates.Any(c => c.StudentId == sp.StudentId && c.GradeId == sp.GradeId && c.ChapterId == ch.ChapterId))
                        {
                            var lastProgress = await _db.StudentLessonProgresses
                                .Where(p => p.StudentId == sp.StudentId && p.Lesson.ChapterId == ch.ChapterId && p.Lesson.Status == "Active")
                                .OrderByDescending(p => p.CompletedAt)
                                .FirstOrDefaultAsync();

                            certificates.Add(new CertificateViewModel
                            {
                                StudentId = sp.StudentId,
                                StudentName = sp.Student.FullName ?? sp.Student.Username,
                                GradeId = sp.GradeId!.Value,
                                GradeName = sp.Grade!.GradeName,
                                ChapterId = ch.ChapterId,
                                ChapterName = ch.ChapterName,
                                CompletedDate = lastProgress?.CompletedAt ?? DateTime.Now
                            });
                        }
                    }
                }
            }
        }

        return View(certificates);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int studentId, int gradeId, int chapterId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            return Challenge();
        }
        int currentUserId = int.Parse(userIdClaim.Value);
        bool isStudent = User.IsInRole("Student");
        bool isParent = User.IsInRole("Parent");

        if (isStudent && studentId != currentUserId)
        {
            return Forbid();
        }
        if (isParent)
        {
            var isChild = await _db.Users.AnyAsync(u => u.UserId == studentId && u.ParentId == currentUserId);
            if (!isChild)
            {
                return Forbid();
            }
        }

        var hasSixMonthPackage = await _db.StudentPackages
            .AnyAsync(sp => sp.StudentId == studentId && sp.GradeId == gradeId && sp.Package.Duration >= 180);

        if (!hasSixMonthPackage)
        {
            TempData["Error"] = "Học sinh không sở hữu gói học 6 tháng cho khối lớp này hoặc chưa đăng ký khóa học.";
            return RedirectToAction(nameof(Index));
        }

        var totalLessons = await _db.Lessons.CountAsync(l => l.ChapterId == chapterId && l.Status == "Active");
        var completedLessons = await _db.StudentLessonProgresses.CountAsync(p => 
            p.StudentId == studentId && 
            p.Lesson.ChapterId == chapterId && 
            p.Lesson.Status == "Active");

        if (totalLessons == 0 || completedLessons < totalLessons)
        {
            TempData["Error"] = "Học sinh chưa hoàn thành đầy đủ bài học của chương này để nhận chứng chỉ.";
            return RedirectToAction(nameof(Index));
        }

        var student = await _db.Users.FindAsync(studentId);
        var grade = await _db.Grades.FindAsync(gradeId);
        var chapter = await _db.Chapters.FindAsync(chapterId);
        
        var lastProgress = await _db.StudentLessonProgresses
            .Where(p => p.StudentId == studentId && p.Lesson.ChapterId == chapterId && p.Lesson.Status == "Active")
            .OrderByDescending(p => p.CompletedAt)
            .FirstOrDefaultAsync();

        ViewBag.Student = student;
        ViewBag.Grade = grade;
        ViewBag.Chapter = chapter;
        ViewBag.CompletedDate = lastProgress?.CompletedAt ?? DateTime.Now;

        return View();
    }
}
