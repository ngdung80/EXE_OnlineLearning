using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace POT_System_ASPNET.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // Seed Admin account
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser == null)
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Email = "admin@cyborg.edu.vn",
                FullName = "System Administrator",
                Role = "Admin",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1990, 1, 1)
            });
            await db.SaveChangesAsync();
        }
        else
        {
            adminUser.Password = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            adminUser.Status = "active";
            adminUser.Deleted = false;
            await db.SaveChangesAsync();
        }

        // Seed Parent account
        var parentUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "parent");
        if (parentUser == null)
        {
            parentUser = new User
            {
                Username = "parent",
                Password = BCrypt.Net.BCrypt.HashPassword("Parent@123"),
                Email = "parent@cyborg.edu.vn",
                FullName = "Sample Parent",
                Role = "Parent",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1985, 5, 5)
            };
            db.Users.Add(parentUser);
            await db.SaveChangesAsync();
        }
        else
        {
            parentUser.Password = BCrypt.Net.BCrypt.HashPassword("Parent@123");
            parentUser.Status = "active";
            parentUser.Deleted = false;
            await db.SaveChangesAsync();
        }

        // Seed Wallet for Sample Parent with 99,000 VND
        var parentWallet = await db.Wallets.FirstOrDefaultAsync(w => w.ParentId == parentUser.UserId);
        if (parentWallet == null)
        {
            parentWallet = new Wallet
            {
                ParentId = parentUser.UserId,
                Balance = 99000,
                LastUpdated = DateTime.UtcNow
            };
            db.Wallets.Add(parentWallet);
        }
        else
        {
            parentWallet.Balance = 99000;
            parentWallet.LastUpdated = DateTime.UtcNow;
        }
        await db.SaveChangesAsync();

        // Seed Student account
        var studentUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "student");
        if (studentUser == null)
        {
            db.Users.Add(new User
            {
                Username = "student",
                Password = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                Email = "student@cyborg.edu.vn",
                FullName = "Sample Student",
                Role = "Student",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(2018, 10, 10), // Tuổi phù hợp lớp 1, 2
                ParentId = parentUser.UserId
            });
        }
        else
        {
            studentUser.Password = BCrypt.Net.BCrypt.HashPassword("Student@123");
            studentUser.Status = "active";
            studentUser.Deleted = false;
            studentUser.ParentId = parentUser.UserId;
        }

        // Seed Content Manager account
        var managerUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "manager");
        if (managerUser == null)
        {
            db.Users.Add(new User
            {
                Username = "manager",
                Password = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                Email = "manager@cyborg.edu.vn",
                FullName = "Content Manager",
                Role = "Content Manager",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1992, 2, 2)
            });
        }
        else
        {
            managerUser.Password = BCrypt.Net.BCrypt.HashPassword("Manager@123");
            managerUser.Status = "active";
            managerUser.Deleted = false;
        }

        // Seed Content Approver account
        var approverUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "approver");
        if (approverUser == null)
        {
            db.Users.Add(new User
            {
                Username = "approver",
                Password = BCrypt.Net.BCrypt.HashPassword("Approver@123"),
                Email = "approver@cyborg.edu.vn",
                FullName = "Content Approver",
                Role = "Content Approver",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1993, 3, 3)
            });
        }
        else
        {
            approverUser.Password = BCrypt.Net.BCrypt.HashPassword("Approver@123");
            approverUser.Status = "active";
            approverUser.Deleted = false;
        }

        await db.SaveChangesAsync();

        // ── Seed 50 parent-student pairs ─────────────────────────────────────
        var parentStudentPairs = new (string ParentEmail, string StudentEmail)[]
        {
            ("thanhhuyen89@gmail.com",  "quangminh08@gmail.com"),
            ("minhduc90@gmail.com",     "baongoc11@gmail.com"),
            ("thuylinh88@gmail.com",    "giabao09@gmail.com"),
            ("quocviet91@gmail.com",    "khanhan12@gmail.com"),
            ("kimngan87@gmail.com",     "nhatminh10@gmail.com"),
            ("anhthu92@gmail.com",      "hoanglong08@gmail.com"),
            ("thanhson89@gmail.com",    "linhchi11@gmail.com"),
            ("ngocmai90@gmail.com",     "ducmanh09@gmail.com"),
            ("phuongthao88@gmail.com",  "myduyen10@gmail.com"),
            ("hoangnam91@gmail.com",    "minhkhoa12@gmail.com"),
            ("thuha87@gmail.com",       "baotran08@gmail.com"),
            ("quanghuy90@gmail.com",    "thuytien11@gmail.com"),
            ("lananh89@gmail.com",      "tuankiet09@gmail.com"),
            ("thanhtruc92@gmail.com",   "ngocbich10@gmail.com"),
            ("minhchau88@gmail.com",    "anhkhoa12@gmail.com"),
            ("quynhchi91@gmail.com",    "giahan08@gmail.com"),
            ("hieupham87@gmail.com",    "khanhlinh10@gmail.com"),
            ("thutrang90@gmail.com",    "minhquan11@gmail.com"),
            ("ngocdiep89@gmail.com",    "phuonganh09@gmail.com"),
            ("vananh92@gmail.com",      "trungkien12@gmail.com"),
            ("kimdung88@gmail.com",     "thienan08@gmail.com"),
            ("quocbao91@gmail.com",     "ngochan10@gmail.com"),
            ("myanh87@gmail.com",       "hoangphuc11@gmail.com"),
            ("thanhnam90@gmail.com",    "dieulinh09@gmail.com"),
            ("huyenmy89@gmail.com",     "quoccuong12@gmail.com"),
            ("ngocanh92@gmail.com",     "thanhbinh08@gmail.com"),
            ("baochau88@gmail.com",     "quynhnga10@gmail.com"),
            ("minhthu91@gmail.com",     "huyhoang11@gmail.com"),
            ("thanhmai87@gmail.com",    "nhatlinh09@gmail.com"),
            ("kimloan90@gmail.com",     "phucnguyen12@gmail.com"),
            ("tuananh89@gmail.com",     "ngocmai08@gmail.com"),
            ("mydung91@gmail.com",      "giakhanh10@gmail.com"),
            ("thuvan88@gmail.com",      "minhnhat11@gmail.com"),
            ("quangvinh90@gmail.com",   "thanhha09@gmail.com"),
            ("ngocbich87@gmail.com",    "ducanh12@gmail.com"),
            ("hoangyen92@gmail.com",    "khanhan08@gmail.com"),
            ("thanhnga89@gmail.com",    "baokhanh10@gmail.com"),
            ("trongnghia91@gmail.com",  "linhdan11@gmail.com"),
            ("mylinh88@gmail.com",      "quochuy09@gmail.com"),
            ("anhduong90@gmail.com",    "kimngan12@gmail.com"),
            ("quoccuong87@gmail.com",   "ngoclinh08@gmail.com"),
            ("thuyvan92@gmail.com",     "hoangkhoi10@gmail.com"),
            ("ducmanh89@gmail.com",     "thuongvo11@gmail.com"),
            ("kimanh91@gmail.com",      "minhphuc09@gmail.com"),
            ("vietanh88@gmail.com",     "thuychi12@gmail.com"),
            ("phuongmai90@gmail.com",   "giabinh08@gmail.com"),
            ("thanhbinh87@gmail.com",   "nhuha10@gmail.com"),
            ("lanphuong92@gmail.com",   "quanghiep11@gmail.com"),
            ("ngocthao89@gmail.com",    "anhkiet09@gmail.com"),
            ("mytrang91@gmail.com",     "baolam12@gmail.com"),
        };

        foreach (var (parentEmail, studentEmail) in parentStudentPairs)
        {
            // ── Parent ──────────────────────────────────────────────────────
            var pUser = await db.Users.FirstOrDefaultAsync(u => u.Email == parentEmail);
            if (pUser == null)
            {
                pUser = new User
                {
                    Username  = parentEmail,
                    Password  = BCrypt.Net.BCrypt.HashPassword("Parent@123"),
                    Email     = parentEmail,
                    FullName  = parentEmail.Split('@')[0],
                    Role      = "Parent",
                    Status    = "active",
                    Deleted   = false,
                    Dob       = new DateOnly(1988, 1, 1)
                };
                db.Users.Add(pUser);
                await db.SaveChangesAsync();   // need UserId for FK
            }

            // ── Student ─────────────────────────────────────────────────────
            var sUser = await db.Users.FirstOrDefaultAsync(u => u.Email == studentEmail);
            if (sUser == null)
            {
                sUser = new User
                {
                    Username  = studentEmail,
                    Password  = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                    Email     = studentEmail,
                    FullName  = studentEmail.Split('@')[0],
                    Role      = "Student",
                    Status    = "active",
                    Deleted   = false,
                    Dob       = new DateOnly(2016, 6, 1),
                    ParentId  = pUser.UserId
                };
                db.Users.Add(sUser);
            }
            else if (sUser.ParentId != pUser.UserId)
            {
                sUser.ParentId = pUser.UserId;
            }
        }
        await db.SaveChangesAsync();
        // ── End 50 pairs ─────────────────────────────────────────────────────

        // Kiểm tra xem có cần dọn dẹp dữ liệu cũ (Lớp 10, 11, 12) và seed lại dữ liệu tiếng Anh hay không
        var firstChapter = await db.Chapters.FirstOrDefaultAsync(c => c.ChapterName == "Unit 1: Bản thân & Chữ cái đơn (Phonics A - G)");
        var firstLesson = await db.Lessons.FirstOrDefaultAsync();
        var needsReSeed = !await db.Grades.AnyAsync(g => g.GradeName == "Lớp 1") || 
                          await db.Grades.AnyAsync(g => g.GradeName == "Lớp 10" || g.GradeName == "Lớp 11" || g.GradeName == "Lớp 12") ||
                          firstChapter == null || 
                          firstChapter.Description == "LESSON 1: Chào hỏi & Tên" ||
                          firstLesson == null ||
                          (firstLesson.ContentText != null && firstLesson.ContentText.Contains("Từ vựng:")) ||
                          await db.Lessons.CountAsync() < 90;

        if (needsReSeed)
        {
            // 1. Gỡ bỏ liên kết GradeId của tất cả User để tránh vi phạm Foreign Key
            var allUsers = await db.Users.ToListAsync();
            foreach (var u in allUsers)
            {
                u.GradeId = null;
            }
            await db.SaveChangesAsync();

            // 2. Xóa các bản ghi liên quan theo thứ tự an toàn
            db.QuestionReports.RemoveRange(await db.QuestionReports.ToListAsync());
            db.TestQuestionResults.RemoveRange(await db.TestQuestionResults.ToListAsync());
            db.TestQuestions.RemoveRange(await db.TestQuestions.ToListAsync());
            db.TestAttempts.RemoveRange(await db.TestAttempts.ToListAsync());
            db.Tests.RemoveRange(await db.Tests.ToListAsync());
            db.Questions.RemoveRange(await db.Questions.ToListAsync());
            db.Lessons.RemoveRange(await db.Lessons.ToListAsync());
            db.Chapters.RemoveRange(await db.Chapters.ToListAsync());
            db.StudentPackages.RemoveRange(await db.StudentPackages.ToListAsync());
            db.Transactions.RemoveRange(await db.Transactions.ToListAsync());
            db.WalletTransactions.RemoveRange(await db.WalletTransactions.ToListAsync());
            db.Subjects.RemoveRange(await db.Subjects.ToListAsync());
            db.Packages.RemoveRange(await db.Packages.ToListAsync());
            db.Grades.RemoveRange(await db.Grades.ToListAsync());
            await db.SaveChangesAsync();

            // 3. Đọc curriculum_seed.json và Seed
            var jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "curriculum_seed.json");
            if (!File.Exists(jsonPath))
            {
                jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "curriculum_seed.json");
            }

            if (File.Exists(jsonPath))
            {
                var jsonStr = await File.ReadAllTextAsync(jsonPath);
                var curriculum = JsonConvert.DeserializeObject<List<GradeSeedDto>>(jsonStr);
                
                if (curriculum != null)
                {
                    foreach (var gDto in curriculum)
                    {
                        var grade = new Grade 
                        { 
                            GradeName = gDto.GradeName, 
                            Description = gDto.Description, 
                            Status = "Active" 
                        };
                        db.Grades.Add(grade);
                        await db.SaveChangesAsync();

                        var subject = new Subject
                        {
                            GradeId = grade.GradeId,
                            SubjectName = gDto.SubjectName,
                            Description = gDto.SubjectDescription,
                            Status = "Active",
                            Image = gDto.SubjectImage
                        };
                        db.Subjects.Add(subject);
                        await db.SaveChangesAsync();

                        // Add a test for this subject
                        var test = new Test
                        {
                            TestName = $"Bài Kiểm Tra Năng Lực {gDto.SubjectName}",
                            SubjectId = subject.SubjectId,
                            Duration = 10,
                            Status = "Active",
                            Types = "Quick Test",
                            CreatedAt = DateTime.UtcNow
                        };
                        db.Tests.Add(test);
                        await db.SaveChangesAsync();

                        foreach (var uDto in gDto.Units)
                        {
                            var chapter = new Chapter
                            {
                                SubjectId = subject.SubjectId,
                                ChapterName = uDto.UnitName,
                                Description = uDto.Description,
                                Status = "Active"
                            };
                            db.Chapters.Add(chapter);
                            await db.SaveChangesAsync();

                            foreach (var lDto in uDto.Lessons)
                            {
                                var lesson = new Lesson
                                {
                                    ChapterId = chapter.ChapterId,
                                    LessonName = lDto.LessonName,
                                    ContentText = lDto.ContentText,
                                    FileUrl = lDto.FileUrl,
                                    VocabularyJson = lDto.VocabularyJson,
                                    Status = "Active"
                                };
                                db.Lessons.Add(lesson);
                                await db.SaveChangesAsync();

                                foreach (var qDto in lDto.Questions)
                                {
                                    var question = new Question
                                    {
                                        LessonId = lesson.LessonId,
                                        QuestionContent = qDto.QuestionContent,
                                        Answer = qDto.Answer,
                                        CorrectAnswer = qDto.CorrectAnswer,
                                        Level = qDto.Level,
                                        IsMultipleChoice = qDto.IsMultipleChoice,
                                        Status = "Active"
                                    };
                                    db.Questions.Add(question);
                                    await db.SaveChangesAsync();

                                    // Link question to the test
                                    db.TestQuestions.Add(new TestQuestion
                                    {
                                        TestId = test.TestId,
                                        QuestionId = question.QuestionId
                                    });
                                }
                                await db.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
            else
            {
                throw new FileNotFoundException($"Could not find curriculum seed file at: {jsonPath}");
            }

            // 4. Gán học sinh mẫu cho Grade 1 để dễ test
            var student = await db.Users.FirstOrDefaultAsync(u => u.Username == "student");
            var grade1Obj = await db.Grades.FirstOrDefaultAsync(g => g.GradeName == "Lớp 1");
            if (student != null && grade1Obj != null)
            {
                student.GradeId = grade1Obj.GradeId;
                await db.SaveChangesAsync();

                var premiumPackage = await db.Packages.FirstOrDefaultAsync(p => p.PackageName.Contains("Cao Cấp"));
                if (premiumPackage != null)
                {
                    var hasStudentPkg = await db.StudentPackages.AnyAsync(sp => sp.StudentId == student.UserId && sp.GradeId == grade1Obj.GradeId);
                    if (!hasStudentPkg)
                    {
                        db.StudentPackages.Add(new StudentPackage
                        {
                            StudentId = student.UserId,
                            PackageId = premiumPackage.PackageId,
                            GradeId = grade1Obj.GradeId,
                            StartDate = DateOnly.FromDateTime(DateTime.Now),
                            EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(180))
                        });
                        await db.SaveChangesAsync();
                    }

                    // Gán hoàn thành toàn bộ bài học của Unit 1 để tự động cấp chứng chỉ
                    var ch1_1_Obj = await db.Chapters.FirstOrDefaultAsync(c => c.Subject.GradeId == grade1Obj.GradeId);
                    if (ch1_1_Obj != null)
                    {
                        var unit1Lessons = await db.Lessons.Where(l => l.ChapterId == ch1_1_Obj.ChapterId).ToListAsync();
                        foreach (var les in unit1Lessons)
                        {
                            var hasProgress = await db.StudentLessonProgresses.AnyAsync(p => p.StudentId == student.UserId && p.LessonId == les.LessonId);
                            if (!hasProgress)
                            {
                                db.StudentLessonProgresses.Add(new StudentLessonProgress
                                {
                                    StudentId = student.UserId,
                                    LessonId = les.LessonId,
                                    CompletedAt = DateTime.Now
                                });
                            }
                        }
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        // Seed Packages (always checked and updated to match the 3 packages)
        var existingPkgs = await db.Packages.ToListAsync();
        if (existingPkgs.Count != 3 || !existingPkgs.Any(p => p.PackageName.Contains("7 ngày")))
        {
            // Clear out conflicting data in correct FK order
            db.StudentPackages.RemoveRange(await db.StudentPackages.ToListAsync());
            db.Transactions.RemoveRange(await db.Transactions.ToListAsync());
            db.WalletTransactions.RemoveRange(await db.WalletTransactions.ToListAsync());
            db.Packages.RemoveRange(existingPkgs);
            await db.SaveChangesAsync();

            db.Packages.AddRange(
                new Package { PackageName = "Gói Học Thử Miễn Phí (7 ngày)", Description = "Dùng thử miễn phí đầy đủ tính năng trong 7 ngày học tập, không bao gồm chứng chỉ hoàn thành.", Price = 0, Duration = 7, Status = "Active" },
                new Package { PackageName = "Gói Cơ Bản (1 Tháng)", Description = "Học tiếng Anh 10 phút mỗi ngày bám sát lộ trình sách giáo khoa trong 1 tháng (30 ngày), không bao gồm chứng chỉ.", Price = 99000, Duration = 30, Status = "Active" },
                new Package { PackageName = "Gói Cao Cấp (6 Tháng)", Description = "Trọn bộ lộ trình cá nhân hóa + Báo cáo thông minh cho phụ huynh + Cấp chứng chỉ hoàn thành sau khi hoàn thành khóa học trong 6 tháng (180 ngày).", Price = 499000, Duration = 180, Status = "Active" }
            );
            await db.SaveChangesAsync();
        }
    }

    private class GradeSeedDto
    {
        public string GradeName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string SubjectName { get; set; } = null!;
        public string SubjectDescription { get; set; } = null!;
        public string SubjectImage { get; set; } = null!;
        public List<UnitSeedDto> Units { get; set; } = new();
    }

    private class UnitSeedDto
    {
        public string UnitName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public List<LessonSeedDto> Lessons { get; set; } = new();
    }

    private class LessonSeedDto
    {
        public string LessonName { get; set; } = null!;
        public string ContentText { get; set; } = null!;
        public string? FileUrl { get; set; }
        public string VocabularyJson { get; set; } = null!;
        public List<QuestionSeedDto> Questions { get; set; } = new();
    }

    private class QuestionSeedDto
    {
        public string QuestionContent { get; set; } = null!;
        public bool IsMultipleChoice { get; set; }
        public string Answer { get; set; } = null!;
        public string CorrectAnswer { get; set; } = null!;
        public string Level { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
