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

            if (!needsUpgrade)
            {
                var studentPackages = await _db.StudentPackages
                    .Include(sp => sp.Grade)
                    .Where(sp => sp.StudentId == currentUserId && sp.Package.Duration >= 180 && sp.GradeId.HasValue)
                    .ToListAsync();

                var studentGrades = studentPackages.Select(sp => sp.Grade).Where(g => g != null).Distinct().ToList();
                ViewBag.StudentGrades = studentGrades;

                var gradeIds = studentGrades.Select(g => g!.GradeId).ToList();
                var allChapters = await _db.Chapters
                    .Include(c => c.Subject)
                    .Where(c => gradeIds.Contains(c.Subject.GradeId) && c.Status == "Active")
                    .ToListAsync();

                var studentChaptersData = new List<object>();
                foreach (var ch in allChapters)
                {
                    var eligibilityResult = await CheckCertificateEligibilityAsync(currentUserId, ch.ChapterId);

                    var lastProgress = await _db.StudentLessonProgresses
                        .Where(p => p.StudentId == currentUserId && p.Lesson.ChapterId == ch.ChapterId && p.Lesson.Status == "Active")
                        .OrderByDescending(p => p.CompletedAt)
                        .FirstOrDefaultAsync();

                    studentChaptersData.Add(new
                    {
                        ChapterId = ch.ChapterId,
                        ChapterName = ch.ChapterName,
                        GradeId = ch.Subject.GradeId,
                        TotalLessons = eligibilityResult.TotalLessons,
                        CompletedLessons = eligibilityResult.CompletedLessons,
                        PracticeCount = eligibilityResult.PracticeCount,
                        IsCompleted = eligibilityResult.Eligible,
                        Classification = eligibilityResult.Classification,
                        CompletedDate = lastProgress?.CompletedAt.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy")
                    });
                }
                ViewBag.StudentChaptersJson = System.Text.Json.JsonSerializer.Serialize(studentChaptersData);
                ViewBag.StudentId = currentUserId;
            }
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
                    var eligibilityResult = await CheckCertificateEligibilityAsync(sp.StudentId, ch.ChapterId);

                    if (eligibilityResult.Eligible)
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
                                CompletedDate = lastProgress?.CompletedAt ?? DateTime.Now,
                                Classification = eligibilityResult.Classification
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

        var result = await CheckCertificateEligibilityAsync(studentId, chapterId);
        if (!result.Eligible)
        {
            TempData["Error"] = "Học sinh chưa đáp ứng đủ điều kiện nhận chứng chỉ (Cần hoàn thành bài học, làm ít nhất 2 bài luyện tập và tất cả bài kiểm tra đạt từ 6.0 điểm trở lên).";
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
        ViewBag.Classification = result.Classification;

        return View();
    }

    private async Task<(bool Eligible, string Classification, int PracticeCount, int TotalLessons, int CompletedLessons, double? AvgScore)> CheckCertificateEligibilityAsync(int studentId, int chapterId)
    {
        var lessonIds = await _db.Lessons
            .Where(l => l.ChapterId == chapterId && l.Status == "Active")
            .Select(l => l.LessonId)
            .ToListAsync();

        if (lessonIds.Count == 0)
        {
            return (false, "Khá", 0, 0, 0, null);
        }

        var completedLessonsCount = await _db.StudentLessonProgresses.CountAsync(p => 
            p.StudentId == studentId && 
            lessonIds.Contains(p.LessonId) && 
            p.Lesson.Status == "Active");

        bool lessonsCompleted = completedLessonsCount >= lessonIds.Count;

        // Get tests for these lessons
        var tests = await _db.Tests
            .Where(t => t.Status == "Active" && t.LessonId.HasValue && lessonIds.Contains(t.LessonId.Value))
            .ToListAsync();

        var practiceTestIds = tests.Where(t => t.Types == "Practice").Select(t => t.TestId).ToList();
        var standardTests = tests.Where(t => t.Types == "Test").ToList();

        // Check practice tests: must do at least 2 attempts (if any practice tests exist)
        int practiceAttemptsCount = 0;
        bool practiceCompleted = true;
        if (practiceTestIds.Count > 0)
        {
            practiceAttemptsCount = await _db.TestAttempts
                .Where(ta => ta.StudentId == studentId && practiceTestIds.Contains(ta.TestId) && ta.Score.HasValue)
                .CountAsync();
            practiceCompleted = practiceAttemptsCount >= 2;
        }

        // Check standard tests: must score >= 6.0 for all (if any standard tests exist)
        bool standardPassed = true;
        double totalBestScores = 0;
        int standardTestCount = standardTests.Count;

        if (standardTestCount > 0)
        {
            foreach (var st in standardTests)
            {
                var bestAttempt = await _db.TestAttempts
                    .Where(ta => ta.StudentId == studentId && ta.TestId == st.TestId && ta.Score.HasValue)
                    .OrderByDescending(ta => ta.Score)
                    .FirstOrDefaultAsync();

                if (bestAttempt == null || bestAttempt.Score < 6.0)
                {
                    standardPassed = false;
                    break;
                }
                totalBestScores += bestAttempt.Score ?? 0.0;
            }
        }

        bool eligible = lessonsCompleted && practiceCompleted && standardPassed;

        // Classification
        double avgScore = 6.0;
        if (standardTestCount > 0)
        {
            avgScore = totalBestScores / standardTestCount;
        }
        else if (practiceTestIds.Count > 0)
        {
            var bestPracticeScores = new List<double>();
            foreach (var pid in practiceTestIds)
            {
                var bestP = await _db.TestAttempts
                    .Where(ta => ta.StudentId == studentId && ta.TestId == pid && ta.Score.HasValue)
                    .OrderByDescending(ta => ta.Score)
                    .Select(ta => ta.Score!.Value)
                    .FirstOrDefaultAsync();
                if (bestP > 0) bestPracticeScores.Add(bestP);
            }
            if (bestPracticeScores.Count > 0)
            {
                avgScore = bestPracticeScores.Average();
            }
        }

        string classification = "Khá";
        if (avgScore >= 9.5) classification = "Xuất sắc";
        else if (avgScore >= 8.0) classification = "Giỏi";

        return (eligible, classification, practiceAttemptsCount, lessonIds.Count, completedLessonsCount, avgScore);
    }
}
