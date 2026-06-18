using System;

namespace POT_System_ASPNET.Data.Entities;

public class StudentLessonProgress
{
    public int ProgressId { get; set; }
    public int StudentId { get; set; }
    public int LessonId { get; set; }
    public DateTime CompletedAt { get; set; }

    public User Student { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
