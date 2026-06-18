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
                          await db.Grades.AnyAsync(g => g.GradeName == "Lớp 10" || g.GradeName == "Lớp 11" || g.GradeName == "Lớp 12") ||
                          !await db.Lessons.AnyAsync(l => l.LessonName == "Lesson 1: Chào hỏi & Tên");

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
            var ch1_1 = new Chapter { SubjectId = subject1.SubjectId, ChapterName = "Unit 1: Bản thân & Chữ cái đơn (Phonics A - G)", Description = "Bé làm quen với nhịp điệu tiếng Anh, nhận diện toàn bộ 26 âm đơn của bảng chữ cái (Phonics A-Z) và giao tiếp các chủ đề siêu quen thuộc.", Status = "Active" };
            var ch1_2 = new Chapter { SubjectId = subject1.SubjectId, ChapterName = "Unit 2: Alphabet & Numbers 1-10", Description = "Bảng chữ cái đơn giản và các con số từ 1 đến 10", Status = "Active" };
            var ch1_3 = new Chapter { SubjectId = subject1.SubjectId, ChapterName = "Unit 3: My School Things", Description = "Nhận biết các đồ dùng học tập phổ biến trong lớp học", Status = "Active" };
            
            // Lớp 2 Units
            var ch2_1 = new Chapter { SubjectId = subject2.SubjectId, ChapterName = "Unit 1: Me and My World", Description = "Làm quen với môi trường tiếng Anh, hình thành thói quen nghe và phản xạ tiếng Anh hằng ngày.", Status = "Active" };
            var ch2_2 = new Chapter { SubjectId = subject2.SubjectId, ChapterName = "Unit 2: My Family & Friends", Description = "Cách gọi tên các thành viên trong gia đình và bạn thân", Status = "Active" };
            var ch2_3 = new Chapter { SubjectId = subject2.SubjectId, ChapterName = "Unit 3: Animals & Pets", Description = "Thế giới động vật ngộ nghĩnh và các thú cưng trong nhà", Status = "Active" };
            
            db.Chapters.AddRange(ch1_1, ch1_2, ch1_3, ch2_1, ch2_2, ch2_3);
            await db.SaveChangesAsync();

            // 6. Seed Lessons
            // Unit 1 Lớp 1
            var les1_1_1 = new Lesson 
            { 
                ChapterId = ch1_1.ChapterId, 
                LessonName = "Lesson 1: Chào hỏi & Tên", 
                ContentText = "Mục tiêu: Chào hỏi & Tên\n\n- Từ vựng: Hello, Goodbye, Friend, Teacher.\n- Phản xạ: \"Hello! What's your name? -> Hello, I am [Tên con].\"\n- Phonics: Âm /æ/ (A - Apple, Ant) và /b/ (B - Ball, Boy).\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=F1P5Cr4hKfw\n- https://www.youtube.com/watch?v=BNRkyiPmtJI\n- https://www.youtube.com/watch?v=Hh2_Qs7dGDw", 
                FileUrl = "https://www.youtube.com/watch?v=F1P5Cr4hKfw",
                VocabularyJson = @"[
                    {""word"":""Hello"",""phonetic"":""/həˈləʊ/"",""meaning"":""Xin chào"",""emoji"":""👋""},
                    {""word"":""Goodbye"",""phonetic"":""/ˌɡʊdˈbaɪ/"",""meaning"":""Tạm biệt"",""emoji"":""👋""},
                    {""word"":""Friend"",""phonetic"":""/frend/"",""meaning"":""Người bạn"",""emoji"":""🧑‍🤝‍🧑""},
                    {""word"":""Teacher"",""phonetic"":""/ˈtiːtʃə(r)/"",""meaning"":""Giáo viên"",""emoji"":""👩‍🏫""},
                    {""word"":""Name"",""phonetic"":""/neɪm/"",""meaning"":""Tên"",""emoji"":""🏷️""},
                    {""word"":""What"",""phonetic"":""/wɒt/"",""meaning"":""Cái gì"",""emoji"":""❓""},
                    {""word"":""Apple"",""phonetic"":""/ˈæpl/"",""meaning"":""Quả táo"",""emoji"":""🍎""},
                    {""word"":""Ant"",""phonetic"":""/ænt/"",""meaning"":""Con kiến"",""emoji"":""🐜""},
                    {""word"":""Ball"",""phonetic"":""/bɔːl/"",""meaning"":""Quả bóng"",""emoji"":""⚽""},
                    {""word"":""Boy"",""phonetic"":""/bɔɪ/"",""meaning"":""Cậu bé"",""emoji"":""👦""},
                    {""word"":""Hi"",""phonetic"":""/haɪ/"",""meaning"":""Xin chào (thân mật)"",""emoji"":""👋""},
                    {""word"":""Class"",""phonetic"":""/klɑːs/"",""meaning"":""Lớp học"",""emoji"":""🏫""},
                    {""word"":""Book"",""phonetic"":""/bʊk/"",""meaning"":""Quyển sách"",""emoji"":""📖""},
                    {""word"":""Pen"",""phonetic"":""/pen/"",""meaning"":""Bút mực"",""emoji"":""🖊️""},
                    {""word"":""Bag"",""phonetic"":""/bæɡ/"",""meaning"":""Cặp sách"",""emoji"":""🎒""},
                    {""word"":""Baby"",""phonetic"":""/ˈbeɪbi/"",""meaning"":""Em bé"",""emoji"":""👶""},
                    {""word"":""Bird"",""phonetic"":""/bɜːd/"",""meaning"":""Con chim"",""emoji"":""🐦""},
                    {""word"":""Banana"",""phonetic"":""/bəˈnɑːnə/"",""meaning"":""Quả chuối"",""emoji"":""🍌""},
                    {""word"":""Bear"",""phonetic"":""/beə(r)/"",""meaning"":""Con gấu"",""emoji"":""🐻""},
                    {""word"":""Bee"",""phonetic"":""/biː/"",""meaning"":""Con ong"",""emoji"":""🐝""}
                ]",
                Status = "Active" 
            };

            var les1_1_2 = new Lesson 
            { 
                ChapterId = ch1_1.ChapterId, 
                LessonName = "Lesson 2: Tuổi tác", 
                ContentText = "Mục tiêu: Tuổi tác\n\n- Từ vựng: Số đếm 1, 2, 3, 4, 5, 6, Old, Years.\n- Phản xạ: \"How old are you? -> I am 6.\" / \"I am six years old.\"\n- Phonics: Âm /k/ (C - Cat, Cup) và /d/ (D - Dog, Desk).\n- Hoạt động: Ba mẹ gõ trống/tay x lần, bé đếm số tiếng gõ và bật nhảy x lần.\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=o75oVf8JDeQ\n- https://www.youtube.com/watch?v=Xy9IT280qc4\n- https://www.youtube.com/watch?v=RMIfR-CWH_4", 
                FileUrl = "https://www.youtube.com/watch?v=o75oVf8JDeQ",
                VocabularyJson = @"[
                    {""word"":""One"",""phonetic"":""/wʌn/"",""meaning"":""Số 1"",""emoji"":""1️⃣""},
                    {""word"":""Two"",""phonetic"":""/tuː/"",""meaning"":""Số 2"",""emoji"":""2️⃣""},
                    {""word"":""Three"",""phonetic"":""/θriː/"",""meaning"":""Số 3"",""emoji"":""3️⃣""},
                    {""word"":""Four"",""phonetic"":""/fɔː(r)/"",""meaning"":""Số 4"",""emoji"":""4️⃣""},
                    {""word"":""Five"",""phonetic"":""/faɪv/"",""meaning"":""Số 5"",""emoji"":""5️⃣""},
                    {""word"":""Six"",""phonetic"":""/sɪks/"",""meaning"":""Số 6"",""emoji"":""6️⃣""},
                    {""word"":""Old"",""phonetic"":""/əʊld/"",""meaning"":""Tuổi, già"",""emoji"":""👴""},
                    {""word"":""Years"",""phonetic"":""/jɪəz/"",""meaning"":""Năm"",""emoji"":""📅""},
                    {""word"":""How"",""phonetic"":""/haʊ/"",""meaning"":""Như thế nào"",""emoji"":""❓""},
                    {""word"":""Cat"",""phonetic"":""/kæt/"",""meaning"":""Con mèo"",""emoji"":""🐱""},
                    {""word"":""Cup"",""phonetic"":""/kʌp/"",""meaning"":""Cái cốc"",""emoji"":""🥤""},
                    {""word"":""Dog"",""phonetic"":""/dɒɡ/"",""meaning"":""Con chó"",""emoji"":""🐶""},
                    {""word"":""Desk"",""phonetic"":""/desk/"",""meaning"":""Bàn học"",""emoji"":""🪑""},
                    {""word"":""Seven"",""phonetic"":""/ˈsevn/"",""meaning"":""Số 7"",""emoji"":""7️⃣""},
                    {""word"":""Eight"",""phonetic"":""/eɪt/"",""meaning"":""Số 8"",""emoji"":""8️⃣""},
                    {""word"":""Nine"",""phonetic"":""/naɪn/"",""meaning"":""Số 9"",""emoji"":""9️⃣""},
                    {""word"":""Ten"",""phonetic"":""/ten/"",""meaning"":""Số 10"",""emoji"":""🔟""},
                    {""word"":""Car"",""phonetic"":""/kɑː(r)/"",""meaning"":""Ô tô"",""emoji"":""🚗""},
                    {""word"":""Cake"",""phonetic"":""/keɪk/"",""meaning"":""Bánh ngọt"",""emoji"":""🍰""},
                    {""word"":""Doll"",""phonetic"":""/dɒl/"",""meaning"":""Búp bê"",""emoji"":""🪆""}
                ]",
                Status = "Active" 
            };

            var les1_1_3 = new Lesson 
            { 
                ChapterId = ch1_1.ChapterId, 
                LessonName = "Lesson 3: Cảm xúc", 
                ContentText = "Mục tiêu: Cảm xúc\n\n- Từ vựng: Happy, Sad, Angry, Tired.\n- Phản xạ: \"How are you? -> I'm happy!\" / \"Are you tired? -> Yes/No.\"\n- Phonics: Âm /e/ (E - Egg, Elephant) và /f/ (F - Fish, Frog).\n- Hoạt động: Đóng vai (Role-play) làm các nét mặt cảm xúc tương ứng để đối phương đoán từ.\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=O13gITUS5t4\n- https://www.youtube.com/watch?v=JDaA5-XAOXY\n- https://www.youtube.com/watch?v=jgFDhDRuBNo", 
                FileUrl = "https://www.youtube.com/watch?v=O13gITUS5t4",
                VocabularyJson = @"[
                    {""word"":""Happy"",""phonetic"":""/ˈhæpi/"",""meaning"":""Vui vẻ"",""emoji"":""😊""},
                    {""word"":""Sad"",""phonetic"":""/sæd/"",""meaning"":""Buồn bã"",""emoji"":""😢""},
                    {""word"":""Angry"",""phonetic"":""/ˈæŋɡri/"",""meaning"":""Tức giận"",""emoji"":""😡""},
                    {""word"":""Tired"",""phonetic"":""/ˈtaɪəd/"",""meaning"":""Mệt mỏi"",""emoji"":""😫""},
                    {""word"":""Egg"",""phonetic"":""/eɡ/"",""meaning"":""Quả trứng"",""emoji"":""🥚""},
                    {""word"":""Elephant"",""phonetic"":""/ˈelɪfənt/"",""meaning"":""Con voi"",""emoji"":""🐘""},
                    {""word"":""Fish"",""phonetic"":""/fɪʃ/"",""meaning"":""Con cá"",""emoji"":""🐟""},
                    {""word"":""Frog"",""phonetic"":""/frɒɡ/"",""meaning"":""Con ếch"",""emoji"":""🐸""},
                    {""word"":""Good"",""phonetic"":""/ɡʊd/"",""meaning"":""Tốt, khỏe"",""emoji"":""👍""},
                    {""word"":""Great"",""phonetic"":""/ɡreɪt/"",""meaning"":""Tuyệt vời"",""emoji"":""🌟""},
                    {""word"":""Sleepy"",""phonetic"":""/ˈsliːpi/"",""meaning"":""Buồn ngủ"",""emoji"":""😴""},
                    {""word"":""Scared"",""phonetic"":""/skeəd/"",""meaning"":""Sợ hãi"",""emoji"":""😨""},
                    {""word"":""Smile"",""phonetic"":""/smaɪl/"",""meaning"":""Nụ cười"",""emoji"":""😄""},
                    {""word"":""Cry"",""phonetic"":""/kraɪ/"",""meaning"":""Khóc"",""emoji"":""😭""},
                    {""word"":""Face"",""phonetic"":""/feɪs/"",""meaning"":""Khuôn mặt"",""emoji"":""🧑""},
                    {""word"":""Eye"",""phonetic"":""/aɪ/"",""meaning"":""Con mắt"",""emoji"":""👁️""},
                    {""word"":""Ear"",""phonetic"":""/ɪə(r)/"",""meaning"":""Cái tai"",""emoji"":""👂""},
                    {""word"":""Fox"",""phonetic"":""/fɒks/"",""meaning"":""Con cáo"",""emoji"":""🦊""},
                    {""word"":""Fan"",""phonetic"":""/fæn/"",""meaning"":""Cái quạt"",""emoji"":""🪭""},
                    {""word"":""Elbow"",""phonetic"":""/ˈelbəʊ/"",""meaning"":""Khuỷu tay"",""emoji"":""💪""}
                ]",
                Status = "Active" 
            };

            var les1_1_4 = new Lesson 
            { 
                ChapterId = ch1_1.ChapterId, 
                LessonName = "Lesson 4: Ôn tập UNIT 1 & Phonics G", 
                ContentText = "Mục tiêu: Ôn tập UNIT 1 & Phonics G\n\n- Nội dung: Ôn tập chào hỏi, tên, tuổi, cảm xúc.\n- Phonics: Âm /g/ (G - Gorilla, Goat).\n- Hoạt động: Trò chơi \"Simon Says\" ôn tập cảm xúc và bảng chữ cái đơn từ A-G.\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=F1P5Cr4hKfw\n- https://www.youtube.com/watch?v=o75oVf8JDeQ\n- https://www.youtube.com/watch?v=O13gITUS5t4\n- https://www.youtube.com/watch?v=7TLBk0OxOqc", 
                FileUrl = "https://www.youtube.com/watch?v=F1P5Cr4hKfw",
                VocabularyJson = @"[
                    {""word"":""Gorilla"",""phonetic"":""/ɡəˈrɪlə/"",""meaning"":""Con đười ươi/khỉ đột"",""emoji"":""🦍""},
                    {""word"":""Goat"",""phonetic"":""/ɡəʊt/"",""meaning"":""Con dê"",""emoji"":""🐐""},
                    {""word"":""Girl"",""phonetic"":""/ɡɜːl/"",""meaning"":""Cô bé"",""emoji"":""👧""},
                    {""word"":""Game"",""phonetic"":""/ɡeɪm/"",""meaning"":""Trò chơi"",""emoji"":""🎮""},
                    {""word"":""Green"",""phonetic"":""/ɡriːn/"",""meaning"":""Màu xanh lá cây"",""emoji"":""🟢""},
                    {""word"":""Garden"",""phonetic"":""/ˈɡɑːdn/"",""meaning"":""Khu vườn"",""emoji"":""🏡""},
                    {""word"":""Gift"",""phonetic"":""/ɡɪft/"",""meaning"":""Món quà"",""emoji"":""🎁""},
                    {""word"":""Guitar"",""phonetic"":""/ɡɪˈtɑː(r)/"",""meaning"":""Đàn ghi-ta"",""emoji"":""🎸""},
                    {""word"":""Gum"",""phonetic"":""/ɡʌm/"",""meaning"":""Kẹo cao su"",""emoji"":""🍬""},
                    {""word"":""Grapes"",""phonetic"":""/greɪps/"",""meaning"":""Quả nho"",""emoji"":""🍇""},
                    {""word"":""Simon"",""phonetic"":""/ˈsaɪmən/"",""meaning"":""Tên riêng (trong trò chơi Simon Says)"",""emoji"":""🧑""},
                    {""word"":""Says"",""phonetic"":""/sez/"",""meaning"":""Nói"",""emoji"":""🗣️""},
                    {""word"":""All"",""phonetic"":""/ɔːl/"",""meaning"":""Tất cả"",""emoji"":""🌐""},
                    {""word"":""Am"",""phonetic"":""/æm/"",""meaning"":""Thì, là, ở"",""emoji"":""🔗""},
                    {""word"":""Are"",""phonetic"":""/ɑː(r)/"",""meaning"":""Thì, là, ở"",""emoji"":""🔗""},
                    {""word"":""You"",""phonetic"":""/juː/"",""meaning"":""Bạn, con"",""emoji"":""🫵""},
                    {""word"":""I"",""phonetic"":""/aɪ/"",""meaning"":""Tôi, con"",""emoji"":""🙋""},
                    {""word"":""Yes"",""phonetic"":""/jes/"",""meaning"":""Vâng, đúng vậy"",""emoji"":""✅""},
                    {""word"":""No"",""phonetic"":""/nəʊ/"",""meaning"":""Không"",""emoji"":""❌""},
                    {""word"":""Fine"",""phonetic"":""/faɪn/"",""meaning"":""Khỏe, tốt"",""emoji"":""👌""}
                ]",
                Status = "Active" 
            };

            // Unit 2 Lớp 1
            var les1_2_1 = new Lesson { ChapterId = ch1_2.ChapterId, LessonName = "Lesson 1: Alphabet Fun (A, B, C)", ContentText = "Làm quen với 3 chữ cái đầu tiên trong bảng chữ cái: A (Apple), B (Ball), C (Cat).", Status = "Active" };
            var les1_2_2 = new Lesson { ChapterId = ch1_2.ChapterId, LessonName = "Lesson 2: Count 1 to 5", ContentText = "Giúp bé tập đếm từ số 1 đến 5 bằng tiếng Anh: One, Two, Three, Four, Five.", Status = "Active" };

            // Unit 3 Lớp 1
            var les1_3_1 = new Lesson { ChapterId = ch1_3.ChapterId, LessonName = "Lesson 1: In the Classroom", ContentText = "Học các từ vựng về đồ dùng học tập phổ biến trong ba lô: Book (quyển sách), Pen (cây bút), Pencil (bút chì).", Status = "Active" };

            // Unit 1 Lớp 2
            var les2_1_1 = new Lesson 
            { 
                ChapterId = ch2_1.ChapterId, 
                LessonName = "Lesson 1: Greetings & Introductions", 
                ContentText = "Mục tiêu: Greetings & Introductions\n\n- Từ vựng: Hello, Hi, Goodbye, Friend, Teacher, Name.\n- Phản xạ:\n  + Hello!\n  + What's your name? -> My name is ____.\n  + Nice to meet you.\n- Phonics: A /æ/ (apple, ant), B /b/ (ball, boy).\n- Hoạt động:\n  + Chơi chuyền bóng và giới thiệu tên.\n  + Chào hỏi các thành viên trong gia đình bằng tiếng Anh.\n  + Hát Hello Song mỗi ngày.\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=F1P5Cr4hKfw\n- https://www.youtube.com/watch?v=BNRkyiPmtJI\n- https://www.starfall.com/h/ltr-classic/?mg=m", 
                FileUrl = "https://www.youtube.com/watch?v=F1P5Cr4hKfw",
                VocabularyJson = @"[
                    {""word"":""Hello"",""phonetic"":""/həˈləʊ/"",""meaning"":""Xin chào"",""emoji"":""👋""},
                    {""word"":""Hi"",""phonetic"":""/haɪ/"",""meaning"":""Xin chào (thân mật)"",""emoji"":""👋""},
                    {""word"":""Goodbye"",""phonetic"":""/ˌɡʊdˈbaɪ/"",""meaning"":""Tạm biệt"",""emoji"":""👋""},
                    {""word"":""Friend"",""phonetic"":""/frend/"",""meaning"":""Người bạn"",""emoji"":""🧑‍🤝‍🧑""},
                    {""word"":""Teacher"",""phonetic"":""/ˈtiːtʃə(r)/"",""meaning"":""Giáo viên"",""emoji"":""👩‍🏫""},
                    {""word"":""Name"",""phonetic"":""/neɪm/"",""meaning"":""Tên"",""emoji"":""🏷️""},
                    {""word"":""Nice"",""phonetic"":""/naɪs/"",""meaning"":""Đẹp, tốt, tử tế"",""emoji"":""😊""},
                    {""word"":""Meet"",""phonetic"":""/miːt/"",""meaning"":""Gặp gỡ"",""emoji"":""🤝""},
                    {""word"":""You"",""phonetic"":""/juː/"",""meaning"":""Bạn, các bạn"",""emoji"":""🫵""},
                    {""word"":""Apple"",""phonetic"":""/ˈæpl/"",""meaning"":""Quả táo"",""emoji"":""🍎""},
                    {""word"":""Ant"",""phonetic"":""/ænt/"",""meaning"":""Con kiến"",""emoji"":""🐜""},
                    {""word"":""Ball"",""phonetic"":""/bɔːl/"",""meaning"":""Quả bóng"",""emoji"":""⚽""},
                    {""word"":""Boy"",""phonetic"":""/bɔɪ/"",""meaning"":""Cậu bé"",""emoji"":""👦""},
                    {""word"":""Girl"",""phonetic"":""/ɡɜːl/"",""meaning"":""Cô bé"",""emoji"":""👧""},
                    {""word"":""Classmate"",""phonetic"":""/ˈklɑːsmeɪt/"",""meaning"":""Bạn cùng lớp"",""emoji"":""🧑‍🎓""},
                    {""word"":""Welcome"",""phonetic"":""/welkəm/"",""meaning"":""Chào mừng"",""emoji"":""🙌""},
                    {""word"":""Book"",""phonetic"":""/bʊk/"",""meaning"":""Quyển sách"",""emoji"":""📖""},
                    {""word"":""Bike"",""phonetic"":""/baɪk/"",""meaning"":""Xe đạp"",""emoji"":""🚲""},
                    {""word"":""Bag"",""phonetic"":""/bæɡ/"",""meaning"":""Cặp sách"",""emoji"":""🎒""},
                    {""word"":""Song"",""phonetic"":""/sɒŋ/"",""meaning"":""Bài hát"",""emoji"":""🎵""}
                ]",
                Status = "Active" 
            };

            var les2_1_2 = new Lesson 
            { 
                ChapterId = ch2_1.ChapterId, 
                LessonName = "Lesson 2: My Age", 
                ContentText = "Mục tiêu: My Age\n\n- Từ vựng: One, Two, Three, Four, Five, Six, Seven, Old, Birthday.\n- Phản xạ:\n  + How old are you? -> I am seven years old.\n  + When is your birthday? -> My birthday is in ______.\n- Phonics: C /k/ (cat, cup), D /d/ (dog, door).\n- Hoạt động:\n  + Đếm đồ vật trong phòng.\n  + Number Hunt.\n  + Đếm số bước chân từ phòng khách đến phòng ngủ.\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=o75oVf8JDeQ", 
                FileUrl = "https://www.youtube.com/watch?v=o75oVf8JDeQ",
                VocabularyJson = @"[
                    {""word"":""One"",""phonetic"":""/wʌn/"",""meaning"":""Số 1"",""emoji"":""1️⃣""},
                    {""word"":""Two"",""phonetic"":""/tuː/"",""meaning"":""Số 2"",""emoji"":""2️⃣""},
                    {""word"":""Three"",""phonetic"":""/θriː/"",""meaning"":""Số 3"",""emoji"":""3️⃣""},
                    {""word"":""Four"",""phonetic"":""/fɔː(r)/"",""meaning"":""Số 4"",""emoji"":""4️⃣""},
                    {""word"":""Five"",""phonetic"":""/faɪv/"",""meaning"":""Số 5"",""emoji"":""5️⃣""},
                    {""word"":""Six"",""phonetic"":""/sɪks/"",""meaning"":""Số 6"",""emoji"":""6️⃣""},
                    {""word"":""Seven"",""phonetic"":""/ˈsevn/"",""meaning"":""Số 7"",""emoji"":""7️⃣""},
                    {""word"":""Old"",""phonetic"":""/əʊld/"",""meaning"":""Tuổi, già"",""emoji"":""👴""},
                    {""word"":""Years"",""phonetic"":""/jɪəz/"",""meaning"":""Năm, tuổi"",""emoji"":""📅""},
                    {""word"":""Birthday"",""phonetic"":""/ˈbɜːθdeɪ/"",""meaning"":""Ngày sinh nhật"",""emoji"":""🎂""},
                    {""word"":""When"",""phonetic"":""/wen/"",""meaning"":""Khi nào"",""emoji"":""❓""},
                    {""word"":""In"",""phonetic"":""/ɪn/"",""meaning"":""Vào (tháng, năm)"",""emoji"":""📥""},
                    {""word"":""Cat"",""phonetic"":""/kæt/"",""meaning"":""Con mèo"",""emoji"":""🐱""},
                    {""word"":""Cup"",""phonetic"":""/kʌp/"",""meaning"":""Cái cốc"",""emoji"":""🥤""},
                    {""word"":""Dog"",""phonetic"":""/dɒɡ/"",""meaning"":""Con chó"",""emoji"":""🐶""},
                    {""word"":""Door"",""phonetic"":""/dɔː(r)/"",""meaning"":""Cửa ra vào"",""emoji"":""🚪""},
                    {""word"":""Month"",""phonetic"":""/mʌnθ/"",""meaning"":""Tháng (trong năm)"",""emoji"":""📅""},
                    {""word"":""Cake"",""phonetic"":""/keɪk/"",""meaning"":""Bánh kem sinh nhật"",""emoji"":""🍰""},
                    {""word"":""Candle"",""phonetic"":""/ˈkændl/"",""meaning"":""Ngọn nến"",""emoji"":""🕯️""},
                    {""word"":""Gift"",""phonetic"":""/ɡɪft/"",""meaning"":""Món quà"",""emoji"":""🎁""}
                ]",
                Status = "Active" 
            };

            var les2_1_3 = new Lesson 
            { 
                ChapterId = ch2_1.ChapterId, 
                LessonName = "Lesson 3: Feelings", 
                ContentText = "Mục tiêu: Feelings\n\n- Từ vựng: Happy, Sad, Angry, Excited, Tired, Scared.\n- Phản xạ:\n  + How are you? -> I am happy.\n  + Are you tired? -> Yes, I am. / No, I'm not.\n- Phonics: E /e/ (egg, elephant), F /f/ (fish, fan).\n- Hoạt động:\n  + Đóng vai cảm xúc.\n  + Trò chơi đoán cảm xúc qua nét mặt.\n  + Vẽ khuôn mặt thể hiện cảm xúc.\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=O13gITUS5t4\n- https://www.youtube.com/watch?v=jgFDhDRuBNo", 
                FileUrl = "https://www.youtube.com/watch?v=O13gITUS5t4",
                VocabularyJson = @"[
                    {""word"":""Happy"",""phonetic"":""/ˈhæpi/"",""meaning"":""Vui vẻ"",""emoji"":""😊""},
                    {""word"":""Sad"",""phonetic"":""/sæd/"",""meaning"":""Buồn bã"",""emoji"":""😢""},
                    {""word"":""Angry"",""phonetic"":""/ˈæŋɡri/"",""meaning"":""Tức giận"",""emoji"":""😡""},
                    {""word"":""Excited"",""phonetic"":""/ɪkˈsaɪtɪd/"",""meaning"":""Hào hứng, phấn khích"",""emoji"":""🤩""},
                    {""word"":""Tired"",""phonetic"":""/ˈtaɪəd/"",""meaning"":""Mệt mỏi"",""emoji"":""😫""},
                    {""word"":""Scared"",""phonetic"":""/skeəd/"",""meaning"":""Sợ hãi"",""emoji"":""😨""},
                    {""word"":""How"",""phonetic"":""/haʊ/"",""meaning"":""Như thế nào"",""emoji"":""❓""},
                    {""word"":""Today"",""phonetic"":""/təˈdeɪ/"",""meaning"":""Hôm nay"",""emoji"":""📅""},
                    {""word"":""Feel"",""phonetic"":""/fiːl/"",""meaning"":""Cảm thấy"",""emoji"":""❤️""},
                    {""word"":""Smile"",""phonetic"":""/smaɪl/"",""meaning"":""Nụ cười"",""emoji"":""😄""},
                    {""word"":""Cry"",""phonetic"":""/kraɪ/"",""meaning"":""Khóc"",""emoji"":""😭""},
                    {""word"":""Egg"",""phonetic"":""/eɡ/"",""meaning"":""Quả trứng"",""emoji"":""🥚""},
                    {""word"":""Elephant"",""phonetic"":""/ˈelɪfənt/"",""meaning"":""Con voi"",""emoji"":""🐘""},
                    {""word"":""Fish"",""phonetic"":""/fɪʃ/"",""meaning"":""Con cá"",""emoji"":""🐟""},
                    {""word"":""Fan"",""phonetic"":""/fæn/"",""meaning"":""Cái quạt"",""emoji"":""🪭""},
                    {""word"":""Friend"",""phonetic"":""/frend/"",""meaning"":""Người bạn"",""emoji"":""🧑‍🤝‍🧑""},
                    {""word"":""Fly"",""phonetic"":""/flaɪ/"",""meaning"":""Bay"",""emoji"":""🪰""},
                    {""word"":""Elbow"",""phonetic"":""/ˈelbəʊ/"",""meaning"":""Khuỷu tay"",""emoji"":""💪""},
                    {""word"":""Fine"",""phonetic"":""/faɪn/"",""meaning"":""Tốt, khỏe"",""emoji"":""👌""},
                    {""word"":""Great"",""phonetic"":""/ɡreɪt/"",""meaning"":""Tuyệt vời"",""emoji"":""🌟""}
                ]",
                Status = "Active" 
            };

            var les2_1_4 = new Lesson 
            { 
                ChapterId = ch2_1.ChapterId, 
                LessonName = "Lesson 4: Review Month 1", 
                ContentText = "Mục tiêu: Review Month 1\n\n- Từ vựng: Ôn tập toàn bộ từ vựng UNIT 1.\n- Phản xạ:\n  + What's your name?\n  + How old are you?\n  + How are you today?\n- Phonics: G /g/ (goat, girl), ôn A - G.\n- Hoạt động:\n  + Simon Says.\n  + Flashcard Race.\n  + Giới thiệu bản thân 30 giây.\n\nVideo bài học bổ sung:\n- https://www.youtube.com/watch?v=F1P5Cr4hKfw\n- https://www.youtube.com/watch?v=o75oVf8JDeQ\n- https://www.youtube.com/watch?v=O13gITUS5t4\n- https://www.youtube.com/watch?v=7TLBk0OxOqc", 
                FileUrl = "https://www.youtube.com/watch?v=F1P5Cr4hKfw",
                VocabularyJson = @"[
                    {""word"":""Goat"",""phonetic"":""/ɡəʊt/"",""meaning"":""Con dê"",""emoji"":""🐐""},
                    {""word"":""Girl"",""phonetic"":""/ɡɜːl/"",""meaning"":""Cô bé"",""emoji"":""👧""},
                    {""word"":""Garden"",""phonetic"":""/ˈɡɑːdn/"",""meaning"":""Khu vườn"",""emoji"":""🏡""},
                    {""word"":""Game"",""phonetic"":""/ɡeɪm/"",""meaning"":""Trò chơi"",""emoji"":""🎮""},
                    {""word"":""Guitar"",""phonetic"":""/ɡɪˈtɑː(r)/"",""meaning"":""Đàn ghi-ta"",""emoji"":""🎸""},
                    {""word"":""Gorilla"",""phonetic"":""/ɡəˈrɪlə/"",""meaning"":""Con khỉ đột"",""emoji"":""🦍""},
                    {""word"":""Review"",""phonetic"":""/rɪˈvjuː/"",""meaning"":""Ôn tập, xem lại"",""emoji"":""📝""},
                    {""word"":""Simon"",""phonetic"":""/ˈsaɪmən/"",""meaning"":""Tên riêng (Trò chơi Simon Says)"",""emoji"":""🧑""},
                    {""word"":""Says"",""phonetic"":""/sez/"",""meaning"":""Nói"",""emoji"":""🗣️""},
                    {""word"":""Race"",""phonetic"":""/reɪs/"",""meaning"":""Cuộc đua"",""emoji"":""🏃""},
                    {""word"":""Flashcard"",""phonetic"":""/flæʃkɑːd/"",""meaning"":""Thẻ từ vựng"",""emoji"":""🎴""},
                    {""word"":""Introduce"",""phonetic"":""/ˌɪntrəˈdjuːs/"",""meaning"":""Giới thiệu"",""emoji"":""🤝""},
                    {""word"":""Myself"",""phonetic"":""/maɪˈself/"",""meaning"":""Bản thân tôi"",""emoji"":""🙋""},
                    {""word"":""Seconds"",""phonetic"":""/ˈsekəndz/"",""meaning"":""Giây (đơn vị thời gian)"",""emoji"":""⏱️""},
                    {""word"":""Hello"",""phonetic"":""/həˈləʊ/"",""meaning"":""Xin chào"",""emoji"":""👋""},
                    {""word"":""Name"",""phonetic"":""/neɪm/"",""meaning"":""Tên"",""emoji"":""🏷️""},
                    {""word"":""Age"",""phonetic"":""/eɪdʒ/"",""meaning"":""Tuổi tác"",""emoji"":""🎂""},
                    {""word"":""Feelings"",""phonetic"":""/ˈfiːlɪŋz/"",""meaning"":""Cảm xúc"",""emoji"":""❤️""},
                    {""word"":""All"",""phonetic"":""/ɔːl/"",""meaning"":""Tất cả"",""emoji"":""🌐""},
                    {""word"":""Talk"",""phonetic"":""/tɔːk/"",""meaning"":""Bài nói, trò chuyện"",""emoji"":""💬""}
                ]",
                Status = "Active" 
            };

            // Unit 2 Lớp 2
            var les2_2_1 = new Lesson { ChapterId = ch2_2.ChapterId, LessonName = "Lesson 1: This is my Dad", ContentText = "Giới thiệu các thành viên trong gia đình yêu quý của bé: Father (bố), Mother (mẹ), Brother (anh/em trai), Sister (chị/em gái).", Status = "Active" };

            // Unit 3 Lớp 2
            var les2_3_1 = new Lesson { ChapterId = ch2_3.ChapterId, LessonName = "Lesson 1: Cute Pets", ContentText = "Học các từ vựng về thú cưng đáng yêu nuôi trong nhà: Dog (chó), Cat (mèo), Fish (cá), Bird (chim).", Status = "Active" };

            db.Lessons.AddRange(les1_1_1, les1_1_2, les1_1_3, les1_1_4, les1_2_1, les1_2_2, les1_3_1, les2_1_1, les2_1_2, les2_1_3, les2_1_4, les2_2_1, les2_3_1);
            await db.SaveChangesAsync();

            // 7. Seed Questions
            var qList = new List<Question>
            {
                // ==================== LỚP 1 LESSON 1 ====================
                new Question { LessonId = les1_1_1.LessonId, QuestionContent = "Khi gặp thầy cô vào lớp, con sẽ nói gì?", Answer = "Goodbye, Hello, Apple, Ball", CorrectAnswer = "Hello", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_1.LessonId, QuestionContent = "Từ nào sau đây có nghĩa là \"Người bạn\"?", Answer = "Teacher, Boy, Friend, Ant", CorrectAnswer = "Friend", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_1.LessonId, QuestionContent = "Hoàn thành câu sau: \"What's your ________? -> I am Nam.\"", Answer = "hello, friend, name, goodbye", CorrectAnswer = "name", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_1.LessonId, QuestionContent = "Từ \"Apple\" bắt đầu bằng âm Phonics nào?", Answer = "/b/, /æ/, /k/, /d/", CorrectAnswer = "/æ/", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_1.LessonId, QuestionContent = "Từ nào có nghĩa là \"Quả bóng\"?", Answer = "Ant, Boy, Ball, Bee", CorrectAnswer = "Ball", Level = "Remember", Status = "Active" },

                // ==================== LỚP 1 LESSON 2 ====================
                new Question { LessonId = les1_1_2.LessonId, QuestionContent = "Khi muốn hỏi tuổi của một người bạn, con dùng câu nào?", Answer = "What's your name?, Hello!, How old are you?, Goodbye.", CorrectAnswer = "How old are you?", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_2.LessonId, QuestionContent = "Bé 6 tuổi sẽ trả lời câu hỏi về tuổi như thế nào?", Answer = "I am happy., I am six years old., My name is Six., Hello, I am 6.", CorrectAnswer = "I am six years old.", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_2.LessonId, QuestionContent = "Số \"3\" trong tiếng Anh đọc là gì?", Answer = "One, Two, Three, Four", CorrectAnswer = "Three", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_2.LessonId, QuestionContent = "Tìm từ có âm Phonics đầu tiên là /d/:", Answer = "Cat, Cup, Dog, Cake", CorrectAnswer = "Dog", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_2.LessonId, QuestionContent = "Nghĩa của từ \"Desk\" là gì?", Answer = "Cái cốc, Con mèo, Bàn học, Con chó", CorrectAnswer = "Bàn học", Level = "Remember", Status = "Active" },

                // ==================== LỚP 1 LESSON 3 ====================
                new Question { LessonId = les1_1_3.LessonId, QuestionContent = "Khi con đang rất vui và có ai đó hỏi \"How are you?\", con sẽ đáp lại:", Answer = "I'm sad., I'm happy!, I am 6., Hello.", CorrectAnswer = "I'm happy!", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_3.LessonId, QuestionContent = "Từ \"Angry\" có nghĩa là gì?", Answer = "Mệt mỏi, Vui vẻ, Tức giận, Buồn bã", CorrectAnswer = "Tức giận", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_3.LessonId, QuestionContent = "Từ nào có nghĩa là \"Con voi\"?", Answer = "Egg, Elephant, Fish, Frog", CorrectAnswer = "Elephant", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_3.LessonId, QuestionContent = "Điền từ thích hợp vào chỗ trống: \"Are you tired? -> ________, I'm not.\"", Answer = "Yes, No, Happy, Sad", CorrectAnswer = "No", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_3.LessonId, QuestionContent = "Từ \"Fish\" và \"Frog\" đều có âm đầu là gì?", Answer = "/e/, /æ/, /f/, /b/", CorrectAnswer = "/f/", Level = "Understand", Status = "Active" },

                // ==================== LỚP 1 LESSON 4 ====================
                new Question { LessonId = les1_1_4.LessonId, QuestionContent = "Trong trò chơi \"Simon Says\", nếu quản trò hô \"Simon says: Be happy!\", con sẽ làm gì?", Answer = "Làm mặt khóc buồn bã, Cười thật tươi vui vẻ, Làm mặt tức giận, Nhắm mắt giả vờ ngủ", CorrectAnswer = "Cười thật tươi vui vẻ", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_4.LessonId, QuestionContent = "Từ nào bắt đầu bằng âm Phonics /g/?", Answer = "Cat, Dog, Goat, Boy", CorrectAnswer = "Goat", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_4.LessonId, QuestionContent = "Từ \"Gorilla\" có nghĩa là con gì?", Answer = "Con dê, Con voi, Con đười ươi/khỉ đột, Con kiến", CorrectAnswer = "Con đười ươi/khỉ đột", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_1_4.LessonId, QuestionContent = "Hoàn thành câu chào: \"Hello, ________ am Lan.\"", Answer = "you, I, what, are", CorrectAnswer = "I", Level = "Understand", Status = "Active" },
                new Question { LessonId = les1_1_4.LessonId, QuestionContent = "Cặp từ trái nghĩa về cảm xúc con đã học là gì?", Answer = "Hello - Goodbye, Happy - Sad, One - Two, Cat - Dog", CorrectAnswer = "Happy - Sad", Level = "Understand", Status = "Active" },

                // ==================== LỚP 2 LESSON 1 ====================
                new Question { LessonId = les2_1_1.LessonId, QuestionContent = "Khi một người bạn mới nói \"Nice to meet you\", con sẽ lịch sự đáp lại như thế nào?", Answer = "Goodbye!, Nice to meet you, too., Hello!, My name is Lan.", CorrectAnswer = "Nice to meet you, too.", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_1.LessonId, QuestionContent = "Khi ba mẹ đưa con đến lớp và con gặp thầy cô giáo, con dùng câu phản xạ nào để chào?", Answer = "Goodbye, teacher!, Hello! What's your name?, Hello, teacher!, Nice to meet you.", CorrectAnswer = "Hello, teacher!", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_1_1.LessonId, QuestionContent = "Điền từ thích hợp vào ô trống để hoàn thành câu hỏi tên: \"What's ________ name?\"", Answer = "you, nice, your, hi", CorrectAnswer = "your", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_1.LessonId, QuestionContent = "Từ nào sau đây có âm đầu phát âm là Phonics /æ/ (chữ A)?", Answer = "Ball, Boy, Ant, Book", CorrectAnswer = "Ant", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_1.LessonId, QuestionContent = "Trò chơi truyền bóng trong lớp học giúp con thực hành phản xạ điều gì?", Answer = "Đếm số lượng đồ vật, Chào hỏi và tự giới thiệu tên của bản thân, Diễn tả các cảm xúc khác nhau, Gọi tên các bộ phận cơ thể", CorrectAnswer = "Chào hỏi và tự giới thiệu tên của bản thân", Level = "Understand", Status = "Active" },

                // ==================== LỚP 2 LESSON 2 ====================
                new Question { LessonId = les2_1_2.LessonId, QuestionContent = "Thầy cô nước ngoài hỏi con: \"How old are you?\", hiện tại con đang học lớp 2 (7 tuổi), con trả lời:", Answer = "My name is Seven., I am seven years old., My birthday is in May., I am happy.", CorrectAnswer = "I am seven years old.", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_1_2.LessonId, QuestionContent = "Khi có một người bạn hỏi: \"When is your birthday?\", con sẽ chọn câu trả lời nào thích hợp?", Answer = "I am seven., My birthday is in October., Nice to meet you., It is a cup.", CorrectAnswer = "My birthday is in October.", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_2.LessonId, QuestionContent = "Từ \"Birthday\" có nghĩa là gì?", Answer = "Tuổi tác, Ngày sinh nhật, Lớp học, Số đếm", CorrectAnswer = "Ngày sinh nhật", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_1_2.LessonId, QuestionContent = "Từ nào sau đây có âm đầu phát âm là Phonics /k/ (chữ C)?", Answer = "Dog, Door, Cup, Boy", CorrectAnswer = "Cup", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_1_2.LessonId, QuestionContent = "Hoàn thành câu sau: \"How ________ are you? -> I am seven.\"", Answer = "name, nice, old, when", CorrectAnswer = "old", Level = "Remember", Status = "Active" },

                // ==================== LỚP 2 LESSON 3 ====================
                new Question { LessonId = les2_1_3.LessonId, QuestionContent = "Thầy cô hỏi con: \"How are you today?\", con cảm thấy rất phấn khích vì được đi chơi, con đáp:", Answer = "I am seven years old., I am excited., Nice to meet you., My name is Nam.", CorrectAnswer = "I am excited.", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_3.LessonId, QuestionContent = "Sau một ngày học tập mệt mỏi, ba mẹ hỏi con: \"Are you tired?\", con mệt thật thì trả lời ra sao?", Answer = "No, I'm not., Yes, I am., I am happy., Thank you.", CorrectAnswer = "Yes, I am.", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_3.LessonId, QuestionContent = "Khi con nhìn thấy một bộ phim ma đáng sợ, cảm xúc của con lúc đó là gì?", Answer = "Happy, Excited, Scared, Angry", CorrectAnswer = "Scared", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_1_3.LessonId, QuestionContent = "Điền từ thích hợp vào chỗ trống: \"Are you tired? -> No, I'm ________.\"", Answer = "am, not, happy, sad", CorrectAnswer = "not", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_3.LessonId, QuestionContent = "Từ nào có âm Phonics đầu tiên phát âm là /e/ (chữ E)?", Answer = "Fish, Fan, Egg, Dog", CorrectAnswer = "Egg", Level = "Remember", Status = "Active" },

                // ==================== LỚP 2 LESSON 4 ====================
                new Question { LessonId = les2_1_4.LessonId, QuestionContent = "Trong hoạt động \"Giới thiệu bản thân 30 giây\", thông tin nào con KHÔNG cần đưa vào bài nói?", Answer = "Tên của con (Name), Số tuổi của con (Age), Cảm xúc hiện tại của con (Feelings), Tên con vật con sợ nhất", CorrectAnswer = "Tên con vật con sợ nhất", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_4.LessonId, QuestionContent = "Khi tham gia trò chơi \"Simon Says\", quản trò hô: \"Simon says: Be angry!\", con sẽ phản xạ thế nào?", Answer = "Khóc buồn bã, Làm nét mặt tức giận, Cười thật tươi, Nhảy lò cò", CorrectAnswer = "Làm nét mặt tức giận", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_4.LessonId, QuestionContent = "Ba mẹ hỏi: \"How are you today?\", con đang cảm thấy rất tốt, con đáp:", Answer = "I am seven years old., My name is Linh., I am fine, thank you., Nice to meet you.", CorrectAnswer = "I am fine, thank you.", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_1_4.LessonId, QuestionContent = "Chữ cái \"G\" trong từ \"Goat\" và \"Girl\" được phát âm Phonics là gì?", Answer = "/æ/, /b/, /g/, /k/", CorrectAnswer = "/g/", Level = "Understand", Status = "Active" },
                new Question { LessonId = les2_1_4.LessonId, QuestionContent = "Từ nào có nghĩa là \"Cuộc đua thẻ từ vựng\"?", Answer = "Simon Says, Flashcard Race, Number Hunt, Color Hunt", CorrectAnswer = "Flashcard Race", Level = "Remember", Status = "Active" },

                // ==================== PLACEHOLDERS ====================
                // Alphabet A, B, C (Lớp 1 Unit 2)
                new Question { LessonId = les1_2_1.LessonId, QuestionContent = "Chữ cái đầu tiên trong bảng chữ cái tiếng Anh là chữ gì?", Answer = "A, B, C, D", CorrectAnswer = "A", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_2_1.LessonId, QuestionContent = "Con mèo tiếng Anh là 'Cat'. Chữ cái bắt đầu của từ 'Cat' là gì?", Answer = "C, A, T, B", CorrectAnswer = "C", Level = "Understand", Status = "Active" },
                
                // Numbers 1 to 5 (Lớp 1 Unit 2)
                new Question { LessonId = les1_2_2.LessonId, QuestionContent = "Số '2' trong tiếng Anh đọc là gì?", Answer = "Two, One, Three, Four", CorrectAnswer = "Two", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_2_2.LessonId, QuestionContent = "Bé hãy đếm xem có bao nhiêu quả táo trong tiếng Anh khi có 4 quả táo?", Answer = "Four, Five, Three, Two", CorrectAnswer = "Four", Level = "Apply", Status = "Active" },
                
                // Classroom Book/Pen (Lớp 1 Unit 3)
                new Question { LessonId = les1_3_1.LessonId, QuestionContent = "Từ nào nghĩa là 'quyển sách' trong tiếng Anh?", Answer = "Book, Pen, Ruler, Pencil", CorrectAnswer = "Book", Level = "Remember", Status = "Active" },
                new Question { LessonId = les1_3_1.LessonId, QuestionContent = "Bé dùng cái gì để viết và vẽ chì?", Answer = "Pencil, Book, Eraser, Desk", CorrectAnswer = "Pencil", Level = "Understand", Status = "Active" },
                
                // Family (Lớp 2 Unit 2)
                new Question { LessonId = les2_2_1.LessonId, QuestionContent = "Người 'Bố' yêu quý trong tiếng Anh gọi là gì?", Answer = "Father, Mother, Brother, Sister", CorrectAnswer = "Father", Level = "Remember", Status = "Active" },
                new Question { LessonId = les2_2_1.LessonId, QuestionContent = "Người 'Mẹ' kính yêu trong tiếng Anh gọi là gì?", Answer = "Mother, Father, Sister, Grandfather", CorrectAnswer = "Mother", Level = "Remember", Status = "Active" },
                
                // Pets (Lớp 2 Unit 3)
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
