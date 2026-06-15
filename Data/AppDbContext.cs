using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Grade> Grades { get; set; }
    public DbSet<Subject> Subjects { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<TestQuestion> TestQuestions { get; set; }
    public DbSet<TestAttempt> TestAttempts { get; set; }
    public DbSet<TestQuestionResult> TestQuestionResults { get; set; }
    public DbSet<Package> Packages { get; set; }
    public DbSet<StudentPackage> StudentPackages { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<WalletTransaction> WalletTransactions { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<QuestionReport> QuestionReports { get; set; }
    public DbSet<MentorAssignment> MentorAssignments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("User");
            e.HasKey(u => u.UserId);
            e.Property(u => u.UserId).HasColumnName("user_id");
            e.Property(u => u.Username).HasColumnName("username");
            e.Property(u => u.Password).HasColumnName("password");
            e.Property(u => u.Email).HasColumnName("email");
            e.Property(u => u.Role).HasColumnName("role");
            e.Property(u => u.GradeId).HasColumnName("grade_id");
            e.Property(u => u.ParentId).HasColumnName("parent_id");
            e.Property(u => u.FullName).HasColumnName("full_name");
            e.Property(u => u.Dob).HasColumnName("dob");
            e.Property(u => u.Phone).HasColumnName("phone");
            e.Property(u => u.Specialization).HasColumnName("specialization");
            e.Property(u => u.WorkTime).HasColumnName("work_time");
            e.Property(u => u.Status).HasColumnName("status");
            e.Property(u => u.Image).HasColumnName("image");
            e.Property(u => u.Deleted).HasColumnName("deleted");

            e.HasOne(u => u.Grade).WithMany(g => g.Users).HasForeignKey(u => u.GradeId);
            e.HasOne(u => u.Parent).WithMany(u => u.Children).HasForeignKey(u => u.ParentId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Grade ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Grade>(e =>
        {
            e.ToTable("Grade");
            e.HasKey(g => g.GradeId);
            e.Property(g => g.GradeId).HasColumnName("grade_id");
            e.Property(g => g.GradeName).HasColumnName("grade_name");
            e.Property(g => g.Status).HasColumnName("status");
            e.Property(g => g.Description).HasColumnName("description");
        });

        // ── Subject ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Subject>(e =>
        {
            e.ToTable("Subject");
            e.HasKey(s => s.SubjectId);
            e.Property(s => s.SubjectId).HasColumnName("subject_id");
            e.Property(s => s.GradeId).HasColumnName("grade_id");
            e.Property(s => s.SubjectName).HasColumnName("subject_name");
            e.Property(s => s.Description).HasColumnName("description");
            e.Property(s => s.Status).HasColumnName("status");
            e.Property(s => s.Image).HasColumnName("image");
            e.HasOne(s => s.Grade).WithMany(g => g.Subjects).HasForeignKey(s => s.GradeId);
        });

        // ── Chapter ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Chapter>(e =>
        {
            e.ToTable("Chapter");
            e.HasKey(c => c.ChapterId);
            e.Property(c => c.ChapterId).HasColumnName("chapter_id");
            e.Property(c => c.SubjectId).HasColumnName("subject_id");
            e.Property(c => c.ChapterName).HasColumnName("chapter_name");
            e.Property(c => c.Description).HasColumnName("description");
            e.Property(c => c.Status).HasColumnName("status");
            e.HasOne(c => c.Subject).WithMany(s => s.Chapters).HasForeignKey(c => c.SubjectId);
        });

        // ── Lesson ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Lesson>(e =>
        {
            e.ToTable("Lesson");
            e.HasKey(l => l.LessonId);
            e.Property(l => l.LessonId).HasColumnName("lesson_id");
            e.Property(l => l.LessonName).HasColumnName("lesson_name");
            e.Property(l => l.ChapterId).HasColumnName("chapter_id");
            e.Property(l => l.ContentText).HasColumnName("content_text");
            e.Property(l => l.FileUrl).HasColumnName("file_url");
            e.Property(l => l.Status).HasColumnName("status");
            e.Property(l => l.InactiveDate).HasColumnName("inactive_date");
            e.Property(l => l.ImageUrl).HasColumnName("image_url");
            e.Property(l => l.DeletedDate).HasColumnName("deleted_date");
            e.HasOne(l => l.Chapter).WithMany(c => c.Lessons).HasForeignKey(l => l.ChapterId);
        });

        // ── Question ────────────────────────────────────────────────────────
        modelBuilder.Entity<Question>(e =>
        {
            e.ToTable("Question");
            e.HasKey(q => q.QuestionId);
            e.Property(q => q.QuestionId).HasColumnName("question_id");
            e.Property(q => q.QuestionContent).HasColumnName("question_content");
            e.Property(q => q.LessonId).HasColumnName("lesson_id");
            e.Property(q => q.Status).HasColumnName("status");
            e.Property(q => q.IsMultipleChoice).HasColumnName("is_multiple_choice");
            e.Property(q => q.Answer).HasColumnName("answer");
            e.Property(q => q.CorrectAnswer).HasColumnName("correct_answer");
            e.Property(q => q.Level).HasColumnName("level");
            e.HasOne(q => q.Lesson).WithMany(l => l.Questions).HasForeignKey(q => q.LessonId);
        });

        // ── Test ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Test>(e =>
        {
            e.ToTable("Test");
            e.HasKey(t => t.TestId);
            e.Property(t => t.TestId).HasColumnName("test_id");
            e.Property(t => t.TestName).HasColumnName("test_name");
            e.Property(t => t.SubjectId).HasColumnName("subject_id");
            e.Property(t => t.LessonId).HasColumnName("lesson_id");
            e.Property(t => t.Duration).HasColumnName("duration");
            e.Property(t => t.Status).HasColumnName("status");
            e.Property(t => t.Types).HasColumnName("types");
            e.Property(t => t.StudentId).HasColumnName("student_id");
            e.Property(t => t.CreatedBy).HasColumnName("created_by");
            e.Property(t => t.CreatedAt).HasColumnName("created_at");
            e.HasOne(t => t.Subject).WithMany().HasForeignKey(t => t.SubjectId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(t => t.Lesson).WithMany(l => l.Tests).HasForeignKey(t => t.LessonId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(t => t.Student).WithMany().HasForeignKey(t => t.StudentId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── TestQuestion ────────────────────────────────────────────────────
        modelBuilder.Entity<TestQuestion>(e =>
        {
            e.ToTable("TestQuestion");
            e.HasKey(tq => tq.TestQuestionId);
            e.Property(tq => tq.TestQuestionId).HasColumnName("test_question_id");
            e.Property(tq => tq.TestId).HasColumnName("test_id");
            e.Property(tq => tq.QuestionId).HasColumnName("question_id");
            e.HasOne(tq => tq.Test).WithMany(t => t.TestQuestions).HasForeignKey(tq => tq.TestId);
            e.HasOne(tq => tq.Question).WithMany(q => q.TestQuestions).HasForeignKey(tq => tq.QuestionId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── TestAttempt ────────────────────────────────────────────────────
        modelBuilder.Entity<TestAttempt>(e =>
        {
            e.ToTable("TestAttempt");
            e.HasKey(ta => ta.AttemptId);
            e.Property(ta => ta.AttemptId).HasColumnName("attempt_id");
            e.Property(ta => ta.TestId).HasColumnName("test_id");
            e.Property(ta => ta.StudentId).HasColumnName("student_id");
            e.Property(ta => ta.StartTime).HasColumnName("start_time");
            e.Property(ta => ta.EndTime).HasColumnName("end_time");
            e.Property(ta => ta.Score).HasColumnName("score");
            e.Property(ta => ta.TotalQuestions).HasColumnName("total_questions");
            e.Property(ta => ta.CorrectAnswers).HasColumnName("correct_answers");
            e.Property(ta => ta.Status).HasColumnName("status");
            e.HasOne(ta => ta.Test).WithMany(t => t.TestAttempts).HasForeignKey(ta => ta.TestId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(ta => ta.Student).WithMany().HasForeignKey(ta => ta.StudentId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── TestQuestionResult ──────────────────────────────────────────────
        modelBuilder.Entity<TestQuestionResult>(e =>
        {
            e.ToTable("TestQuestionResult");
            e.HasKey(r => r.ResultId);
            e.Property(r => r.ResultId).HasColumnName("result_id");
            e.Property(r => r.AttemptId).HasColumnName("attempt_id");
            e.Property(r => r.QuestionId).HasColumnName("question_id");
            e.Property(r => r.SelectedAnswer).HasColumnName("selected_answer");
            e.Property(r => r.IsCorrect).HasColumnName("is_correct");
            e.HasOne(r => r.Attempt).WithMany(ta => ta.TestQuestionResults).HasForeignKey(r => r.AttemptId);
            e.HasOne(r => r.Question).WithMany().HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Package ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Package>(e =>
        {
            e.ToTable("Package");
            e.HasKey(p => p.PackageId);
            e.Property(p => p.PackageId).HasColumnName("package_id");
            e.Property(p => p.PackageName).HasColumnName("package_name");
            e.Property(p => p.Description).HasColumnName("description");
            e.Property(p => p.Price).HasColumnName("price");
            e.Property(p => p.Duration).HasColumnName("duration");
            e.Property(p => p.Status).HasColumnName("status");
        });

        // ── StudentPackage ──────────────────────────────────────────────────
        modelBuilder.Entity<StudentPackage>(e =>
        {
            e.ToTable("StudentPackage");
            e.HasKey(sp => sp.StudentPackageId);
            e.Property(sp => sp.StudentPackageId).HasColumnName("student_package_id");
            e.Property(sp => sp.StudentId).HasColumnName("student_id");
            e.Property(sp => sp.PackageId).HasColumnName("package_id");
            e.Property(sp => sp.GradeId).HasColumnName("grade_id");
            e.Property(sp => sp.SubjectId).HasColumnName("subject_id");
            e.Property(sp => sp.StartDate).HasColumnName("start_date");
            e.Property(sp => sp.EndDate).HasColumnName("end_date");
            e.Property(sp => sp.Status).HasColumnName("status");
            e.HasOne(sp => sp.Student).WithMany().HasForeignKey(sp => sp.StudentId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(sp => sp.Package).WithMany(p => p.StudentPackages).HasForeignKey(sp => sp.PackageId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(sp => sp.Grade).WithMany().HasForeignKey(sp => sp.GradeId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(sp => sp.Subject).WithMany(s => s.StudentPackages).HasForeignKey(sp => sp.SubjectId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Transaction ─────────────────────────────────────────────────────
        modelBuilder.Entity<Transaction>(e =>
        {
            e.ToTable("Transaction");
            e.HasKey(t => t.TransactionId);
            e.Property(t => t.TransactionId).HasColumnName("transaction_id");
            e.Property(t => t.UserId).HasColumnName("user_id");
            e.Property(t => t.PackageId).HasColumnName("package_id");
            e.Property(t => t.StudentPackageId).HasColumnName("student_package_id");
            e.Property(t => t.StudentId).HasColumnName("student_id");
            e.Property(t => t.GradeId).HasColumnName("grade_id");
            e.Property(t => t.MenteeCount).HasColumnName("mentee_count");
            e.Property(t => t.Amount).HasColumnName("amount");
            e.Property(t => t.TransactionDate).HasColumnName("transaction_date");
            e.Property(t => t.Status).HasColumnName("status");
            e.HasOne(t => t.User).WithMany(u => u.Transactions).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(t => t.Package).WithMany(p => p.Transactions).HasForeignKey(t => t.PackageId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Wallet ──────────────────────────────────────────────────────────
        modelBuilder.Entity<Wallet>(e =>
        {
            e.ToTable("Wallet");
            e.HasKey(w => w.WalletId);
            e.Property(w => w.WalletId).HasColumnName("wallet_id");
            e.Property(w => w.ParentId).HasColumnName("parent_id");
            e.Property(w => w.Balance).HasColumnName("balance");
            e.Property(w => w.LastUpdated).HasColumnName("last_updated");
            e.HasOne(w => w.Parent).WithMany(u => u.Wallets).HasForeignKey(w => w.ParentId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── WalletTransaction ───────────────────────────────────────────────
        modelBuilder.Entity<WalletTransaction>(e =>
        {
            e.ToTable("WalletTransaction");
            e.HasKey(wt => wt.WalletTransactionId);
            e.Property(wt => wt.WalletTransactionId).HasColumnName("wallet_transaction_id");
            e.Property(wt => wt.WalletId).HasColumnName("wallet_id");
            e.Property(wt => wt.Amount).HasColumnName("amount");
            e.Property(wt => wt.TransactionType).HasColumnName("transaction_type");
            e.Property(wt => wt.Description).HasColumnName("description");
            e.Property(wt => wt.CreatedAt).HasColumnName("created_at");
            e.Property(wt => wt.Status).HasColumnName("status");
            e.Property(wt => wt.PackageId).HasColumnName("package_id");
            e.Property(wt => wt.StudentPackageId).HasColumnName("student_package_id");
            e.HasOne(wt => wt.Wallet).WithMany(w => w.WalletTransactions).HasForeignKey(wt => wt.WalletId);
            e.HasOne(wt => wt.Package).WithMany(p => p.WalletTransactions).HasForeignKey(wt => wt.PackageId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── Notification ────────────────────────────────────────────────────
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("Notification");
            e.HasKey(n => n.NotificationId);
            e.Property(n => n.NotificationId).HasColumnName("notification_id");
            e.Property(n => n.UserId).HasColumnName("user_id");
            e.Property(n => n.Message).HasColumnName("message");
            e.Property(n => n.IsRead).HasColumnName("is_read");
            e.Property(n => n.CreatedAt).HasColumnName("created_at");
            e.HasOne(n => n.User).WithMany(u => u.Notifications).HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        // ── QuestionReport ──────────────────────────────────────────────────
        modelBuilder.Entity<QuestionReport>(e =>
        {
            e.ToTable("QuestionReport");
            e.HasKey(r => r.ReportId);
            e.Property(r => r.ReportId).HasColumnName("report_id");
            e.Property(r => r.QuestionId).HasColumnName("question_id");
            e.Property(r => r.ReportedBy).HasColumnName("reported_by");
            e.Property(r => r.Reason).HasColumnName("reason");
            e.Property(r => r.Status).HasColumnName("status");
            e.Property(r => r.CreatedAt).HasColumnName("created_at");
            e.Property(r => r.ReviewNote).HasColumnName("review_note");
            e.Property(r => r.ReviewedBy).HasColumnName("reviewed_by");
            e.Property(r => r.Resolution).HasColumnName("resolution");
            e.HasOne(r => r.Question).WithMany(q => q.QuestionReports).HasForeignKey(r => r.QuestionId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(r => r.Reporter).WithMany().HasForeignKey(r => r.ReportedBy).OnDelete(DeleteBehavior.NoAction);
        });

        // ── MentorAssignment ────────────────────────────────────────────────
        modelBuilder.Entity<MentorAssignment>(e =>
        {
            e.ToTable("MentorAssignment");
            e.HasKey(ma => ma.AssignmentId);
            e.Property(ma => ma.AssignmentId).HasColumnName("assignment_id");
            e.Property(ma => ma.MentorId).HasColumnName("mentor_id");
            e.Property(ma => ma.StudentId).HasColumnName("student_id");
            e.Property(ma => ma.AssignedDate).HasColumnName("assigned_date");
            e.Property(ma => ma.Status).HasColumnName("status");
            e.HasOne(ma => ma.Mentor).WithMany().HasForeignKey(ma => ma.MentorId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(ma => ma.Student).WithMany().HasForeignKey(ma => ma.StudentId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
