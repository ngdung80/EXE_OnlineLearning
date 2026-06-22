using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace POT_System_ASPNET.Controllers
{
    [Authorize(Roles = "Student")]
    public class GameController : Controller
    {
        private readonly AppDbContext _db;

        public GameController(AppDbContext db)
        {
            _db = db;
        }

        // GET: Game
        public async Task<IActionResult> Index()
        {
            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Fetch Grades, Chapters, and Lessons to filter on client-side
            var grades = await _db.Grades.Where(g => g.Status == "Active").ToListAsync();
            var chapters = await _db.Chapters.Where(c => c.Status == "Active").Include(c => c.Subject).ToListAsync();
            var lessons = await _db.Lessons.Where(l => l.Status == "Active" && !string.IsNullOrEmpty(l.VocabularyJson)).ToListAsync();

            ViewBag.Grades = grades;
            ViewBag.Chapters = chapters;
            ViewBag.Lessons = lessons;

            return View();
        }

        // GET: Game/Play
        public async Task<IActionResult> Play(int lessonId, string gameType)
        {
            var lesson = await _db.Lessons.Include(l => l.Chapter).FirstOrDefaultAsync(l => l.LessonId == lessonId);
            if (lesson == null) return NotFound();

            if (string.IsNullOrEmpty(lesson.VocabularyJson) || lesson.VocabularyJson == "[]")
            {
                TempData["Error"] = "Bài học này chưa có từ vựng để chơi game. Con hãy chọn bài khác nhé!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.GameType = gameType;
            ViewBag.LessonId = lessonId;
            return View(lesson);
        }

        // POST: Game/SubmitReward
        [HttpPost]
        public async Task<IActionResult> SubmitReward([FromBody] RewardRequest req)
        {
            if (req == null || req.LessonId <= 0)
            {
                return Json(new { success = false, message = "Thông tin không hợp lệ." });
            }

            var studentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            var lesson = await _db.Lessons.Include(l => l.Chapter).FirstOrDefaultAsync(l => l.LessonId == req.LessonId);
            if (lesson == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài học." });
            }

            // Find or create Game Test object
            var test = await _db.Tests.FirstOrDefaultAsync(t => t.LessonId == req.LessonId && t.Types == "Game");
            if (test == null)
            {
                test = new Test
                {
                    TestName = $"Trò chơi: {lesson.LessonName}",
                    SubjectId = lesson.Chapter.SubjectId,
                    LessonId = req.LessonId,
                    Duration = 10,
                    Status = "Active",
                    Types = "Game"
                };
                _db.Tests.Add(test);
                await _db.SaveChangesAsync();
            }

            // Create TestAttempt (CorrectAnswers = 1 means 10 stars)
            var attempt = new TestAttempt
            {
                TestId = test.TestId,
                StudentId = studentId,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                Score = 100,
                TotalQuestions = 1,
                CorrectAnswers = 1, // 1 correct answer = 10 stars reward!
                Status = "Completed"
            };

            _db.TestAttempts.Add(attempt);
            await _db.SaveChangesAsync();

            // Re-calculate Student Stars for session
            var student = await _db.Users.FindAsync(studentId);
            int availableStars = 0;
            if (student != null)
            {
                var totalCorrect = await _db.TestAttempts
                    .Where(ta => ta.StudentId == studentId && ta.Score.HasValue)
                    .SumAsync(ta => ta.CorrectAnswers ?? 0);
                int totalEarnedStars = totalCorrect * 10;

                int spentStars = 0;
                if (!string.IsNullOrEmpty(student.Specialization))
                {
                    var unlockedList = student.Specialization.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var avId in unlockedList)
                    {
                        spentStars += avId switch
                        {
                            "VIP_1" => 400,
                            "VIP_2" => 800,
                            "VIP_3" => 1200,
                            "VIP_4" => 1600,
                            "VIP_5" => 2000,
                            _ => 0
                        };
                    }
                }
                availableStars = Math.Max(0, totalEarnedStars - spentStars);
                HttpContext.Session.SetString("StudentStars", availableStars.ToString());
            }

            return Json(new { success = true, starsEarned = 10, totalStars = availableStars });
        }

        public class RewardRequest
        {
            public int LessonId { get; set; }
            public string GameType { get; set; } = null!;
        }
    }
}
