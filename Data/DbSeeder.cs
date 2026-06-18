using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // Kiểm tra xem có cần dọn dẹp dữ liệu cũ (Lớp 10, 11, 12) và seed lại dữ liệu tiếng Anh hay không
        var needsReSeed = !await db.Grades.AnyAsync(g => g.GradeName == "Lớp 1") || 
                          await db.Grades.AnyAsync(g => g.GradeName == "Lớp 10" || g.GradeName == "Lớp 11" || g.GradeName == "Lớp 12");

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

            // 3. Seed Grades mới
            var grade1 = new Grade { GradeName = "Lớp 1", Description = "Chương trình học Tiếng Anh cho trẻ 6 tuổi", Status = "Active" };
            var grade2 = new Grade { GradeName = "Lớp 2", Description = "Chương trình học Tiếng Anh cho trẻ 7 tuổi", Status = "Active" };
            db.Grades.AddRange(grade1, grade2);
            await db.SaveChangesAsync();

            // 4. Seed Subjects mới
            var subject1 = new Subject 
            { 
                GradeId = grade1.GradeId, 
                SubjectName = "Tiếng Anh Lớp 1", 
                Description = "Lộ trình học từ vựng, phát âm và giao tiếp cơ bản nhất cho trẻ 6 tuổi.", 
                Status = "Active",
                Image = "/images/subjects/english1.png"
            };
            var subject2 = new Subject 
            { 
                GradeId = grade2.GradeId, 
                SubjectName = "Tiếng Anh Lớp 2", 
                Description = "Nâng cao từ vựng bám sát đời sống, rèn luyện nghe và phản xạ giao tiếp đơn giản cho trẻ 7 tuổi.", 
                Status = "Active",
                Image = "/images/subjects/english2.png"
            };
            db.Subjects.AddRange(subject1, subject2);
            await db.SaveChangesAsync();

            // 5. Seed Chapters (Units)
            // Lớp 1 Units
            var ch1_1 = new Chapter { SubjectId = subject1.SubjectId, ChapterName = "Unit 1: Hello & Goodbye", Description = "Chào hỏi và làm quen cơ bản bằng tiếng Anh", Status = "Active" };
            var ch1_2 = new Chapter { SubjectId = subject1.SubjectId, ChapterName = "Unit 2: Alphabet & Numbers 1-10", Description = "Bảng chữ cái đơn giản và các con số từ 1 đến 10", Status = "Active" };
            var ch1_3 = new Chapter { SubjectId = subject1.SubjectId, ChapterName = "Unit 3: My School Things", Description = "Nhận biết các đồ dùng học tập phổ biến trong lớp học", Status = "Active" };
            
            // Lớp 2 Units
            var ch2_1 = new Chapter { SubjectId = subject2.SubjectId, ChapterName = "Unit 1: Colors & Shapes", Description = "Màu sắc sống động xung quanh bé và các hình khối cơ bản", Status = "Active" };
            var ch2_2 = new Chapter { SubjectId = subject2.SubjectId, ChapterName = "Unit 2: My Family & Friends", Description = "Cách gọi tên các thành viên trong gia đình và bạn thân", Status = "Active" };
            var ch2_3 = new Chapter { SubjectId = subject2.SubjectId, ChapterName = "Unit 3: Animals & Pets", Description = "Thế giới động vật ngộ nghĩnh và các thú cưng trong nhà", Status = "Active" };
            
            db.Chapters.AddRange(ch1_1, ch1_2, ch1_3, ch2_1, ch2_2, ch2_3);
            await db.SaveChangesAsync();

            // 6. Seed Lessons
            // Unit 1 Lớp 1
            var les1_1_1 = new Lesson { ChapterId = ch1_1.ChapterId, LessonName = "Lesson 1: Say Hello", ContentText = "Trong bài học này, bé sẽ học cách chào hỏi cơ bản bằng tiếng Anh: Hello, Hi, Good morning.", Status = "Active" };
            var les1_1_2 = new Lesson { ChapterId = ch1_1.ChapterId, LessonName = "Lesson 2: Say Goodbye", ContentText = "Bài học giúp bé biết cách chào tạm biệt thân thiện với bạn bè và thầy cô: Goodbye, Bye, See you later.", Status = "Active" };

            // Unit 2 Lớp 1
            var les1_2_1 = new Lesson { ChapterId = ch1_2.ChapterId, LessonName = "Lesson 1: Alphabet Fun (A, B, C)", ContentText = "Làm quen với 3 chữ cái đầu tiên trong bảng chữ cái: A (Apple), B (Ball), C (Cat).", Status = "Active" };
            var les1_2_2 = new Lesson { ChapterId = ch1_2.ChapterId, LessonName = "Lesson 2: Count 1 to 5", ContentText = "Giúp bé tập đếm từ số 1 đến 5 bằng tiếng Anh: One, Two, Three, Four, Five.", Status = "Active" };

            // Unit 3 Lớp 1
            var les1_3_1 = new Lesson { ChapterId = ch1_3.ChapterId, LessonName = "Lesson 1: In the Classroom", ContentText = "Học các từ vựng về đồ dùng học tập phổ biến trong ba lô: Book (quyển sách), Pen (cây bút), Pencil (bút chì).", Status = "Active" };

            // Unit 1 Lớp 2
            var les2_1_1 = new Lesson { ChapterId = ch2_1.ChapterId, LessonName = "Lesson 1: Let's Paint", ContentText = "Nhận biết các màu sắc cơ bản bằng tiếng Anh: Red (đỏ), Blue (xanh dương), Green (xanh lá), Yellow (vàng).", Status = "Active" };
            var les2_1_2 = new Lesson { ChapterId = ch2_1.ChapterId, LessonName = "Lesson 2: Circles and Squares", ContentText = "Làm quen với các hình khối đơn giản: Circle (hình tròn), Square (hình vuông), Triangle (hình tam giác).", Status = "Active" };

            // Unit 2 Lớp 2
            var les2_2_1 = new Lesson { ChapterId = ch2_2.ChapterId, LessonName = "Lesson 1: This is my Dad", ContentText = "Giới thiệu các thành viên trong gia đình yêu quý của bé: Father (bố), Mother (mẹ), Brother (anh/em trai), Sister (chị/em gái).", Status = "Active" };

            // Unit 3 Lớp 2
            var les2_3_1 = new Lesson { ChapterId = ch2_3.ChapterId, LessonName = "Lesson 1: Cute Pets", ContentText = "Học các từ vựng về thú cưng đáng yêu nuôi trong nhà: Dog (chó), Cat (mèo), Fish (cá), Bird (chim).", Status = "Active" };

            db.Lessons.AddRange(les1_1_1, les1_1_2, les1_2_1, les1_2_2, les1_3_1, les2_1_1, les2_1_2, les2_2_1, les2_3_1);
            await db.SaveChangesAsync();

            // 7. Seed Questions
            var qList = new List<Question>
            {
                // Greetings
                new Question { LessonId = les1_1_1.LessonId, QuestionContent = "Khi gặp người khác, em chào bằng tiếng Anh thế nào?", Answer = "Hello, Goodbye, Thank you, Sorry", CorrectAnswer = "Hello", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_1.LessonId, QuestionContent = "Em tự giới thiệu tên mình bằng cách nói: My ... is Nick.", Answer = "name, book, dog, school", CorrectAnswer = "name", Level = "Understand", Status = "Active" },
                
                // Goodbye
                new Question { LessonId = les1_1_2.LessonId, QuestionContent = "Từ nào mang ý nghĩa 'Tạm biệt' trong tiếng Anh?", Answer = "Goodbye, Hello, Good morning, Hi", CorrectAnswer = "Goodbye", Level = "Remember", Status = "Active" },
                
                // Alphabet A, B, C
                new Question { LessonId = les1_2_1.LessonId, QuestionContent = "Chữ cái đầu tiên trong bảng chữ cái tiếng Anh là chữ gì?", Answer = "A, B, C, D", CorrectAnswer = "A", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_2_1.LessonId, QuestionContent = "Con mèo tiếng Anh là 'Cat'. Chữ cái bắt đầu của từ 'Cat' là gì?", Answer = "C, A, T, B", CorrectAnswer = "C", Level = "Understand", Status = "Active" },
                
                // Numbers 1 to 5
                new Question { LessonId = les1_2_2.LessonId, QuestionContent = "Số '2' trong tiếng Anh đọc là gì?", Answer = "Two, One, Three, Four", CorrectAnswer = "Two", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_2_2.LessonId, QuestionContent = "Bé hãy đếm xem có bao nhiêu quả táo trong tiếng Anh khi có 4 quả táo?", Answer = "Four, Five, Three, Two", CorrectAnswer = "Four", Level = "Apply", Status = "Active" },
                
                // Classroom Book/Pen
                new Question { LessonId = les1_3_1.LessonId, QuestionContent = "Từ nào nghĩa là 'quyển sách' trong tiếng Anh?", Answer = "Book, Pen, Ruler, Pencil", CorrectAnswer = "Book", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_3_1.LessonId, QuestionContent = "Bé dùng cái gì để viết và vẽ chì?", Answer = "Pencil, Book, Eraser, Desk", CorrectAnswer = "Pencil", Level = "Understand", Status = "Active" },
                
                // Let's Paint Colors
                new Question { LessonId = les2_1_1.LessonId, QuestionContent = "Quả táo chín đỏ sẽ có màu gì trong tiếng Anh?", Answer = "Red, Blue, Yellow, Green", CorrectAnswer = "Red", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_1.LessonId, QuestionContent = "Màu xanh dương trong tiếng Anh đọc là gì?", Answer = "Blue, Yellow, Green, Pink", CorrectAnswer = "Blue", Level = "Remember", Status = "Active" },
                
                // Shapes
                new Question { LessonId = les2_1_2.LessonId, QuestionContent = "Hình tròn trong tiếng Anh là gì?", Answer = "Circle, Square, Triangle, Rectangle", CorrectAnswer = "Circle", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_1_2.LessonId, QuestionContent = "Hình vuông tiếng Anh đọc là gì?", Answer = "Square, Circle, Triangle, Star", CorrectAnswer = "Square", Level = "Remember", Status = "Active" },
                
                // Family
                new Question { LessonId = les2_2_1.LessonId, QuestionContent = "Người 'Bố' yêu quý trong tiếng Anh gọi là gì?", Answer = "Father, Mother, Brother, Sister", CorrectAnswer = "Father", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_2_1.LessonId, QuestionContent = "Người 'Mẹ' kính yêu trong tiếng Anh gọi là gì?", Answer = "Mother, Father, Sister, Grandfather", CorrectAnswer = "Mother", Level = "Remember", Status = "Active" },
                
                // Pets
                new Question { LessonId = les2_3_1.LessonId, QuestionContent = "Chú mèo đáng yêu trong tiếng Anh là gì?", Answer = "Cat, Dog, Bird, Rabbit", CorrectAnswer = "Cat", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_3_1.LessonId, QuestionContent = "Chú chó trung thành trong tiếng Anh đọc là gì?", Answer = "Dog, Cat, Fish, Pig", CorrectAnswer = "Dog", Level = "Remember", Status = "Active" }
            };
            db.Questions.AddRange(qList);
            await db.SaveChangesAsync();

            // 8. Seed Tests & TestQuestions
            var test1 = new Test
            {
                TestName = "Bài Kiểm Tra Năng Lực Tiếng Anh Lớp 1",
                SubjectId = subject1.SubjectId,
                Duration = 10,
                Status = "Active",
                Types = "Quick Test",
                CreatedAt = DateTime.UtcNow
            };
            var test2 = new Test
            {
                TestName = "Bài Kiểm Tra Năng Lực Tiếng Anh Lớp 2",
                SubjectId = subject2.SubjectId,
                Duration = 10,
                Status = "Active",
                Types = "Quick Test",
                CreatedAt = DateTime.UtcNow
            };
            db.Tests.AddRange(test1, test2);
            await db.SaveChangesAsync();

            // Gán câu hỏi
            foreach (var q in qList)
            {
                if (q.Lesson.Chapter.SubjectId == subject1.SubjectId)
                {
                    db.TestQuestions.Add(new TestQuestion { TestId = test1.TestId, QuestionId = q.QuestionId });
                }
                else if (q.Lesson.Chapter.SubjectId == subject2.SubjectId)
                {
                    db.TestQuestions.Add(new TestQuestion { TestId = test2.TestId, QuestionId = q.QuestionId });
                }
            }
            await db.SaveChangesAsync();

            // 10. Gán học sinh mẫu cho Grade 1 để dễ test
            var student = await db.Users.FirstOrDefaultAsync(u => u.Username == "student");
            if (student != null)
            {
                student.GradeId = grade1.GradeId;
                await db.SaveChangesAsync();

                // 11. Gán gói học cao cấp cho học sinh mẫu để kiểm thử Chứng chỉ
                var premiumPackage = await db.Packages.FirstOrDefaultAsync(p => p.PackageName.Contains("Cao Cấp"));
                if (premiumPackage != null)
                {
                    var hasStudentPkg = await db.StudentPackages.AnyAsync(sp => sp.StudentId == student.UserId && sp.GradeId == grade1.GradeId);
                    if (!hasStudentPkg)
                    {
                        db.StudentPackages.Add(new StudentPackage
                        {
                            StudentId = student.UserId,
                            PackageId = premiumPackage.PackageId,
                            GradeId = grade1.GradeId,
                            StartDate = DateOnly.FromDateTime(DateTime.Now),
                            EndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(180))
                        });
                        await db.SaveChangesAsync();
                    }

                    // 12. Gán hoàn thành toàn bộ bài học của Unit 1 để tự động cấp chứng chỉ
                    var unit1Lessons = await db.Lessons.Where(l => l.ChapterId == ch1_1.ChapterId).ToListAsync();
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
}
