using System;
using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Test
{
    public int TestId { get; set; }
    public string TestName { get; set; } = null!;
    public int? SubjectId { get; set; }
    public int? LessonId { get; set; }
    public int Duration { get; set; }   // minutes
    public string? Status { get; set; }
    public string? Types { get; set; }  // "Test" or "Practice"
    public int? StudentId { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Subject? Subject { get; set; }
    public Lesson? Lesson { get; set; }
    public User? Student { get; set; }
    public ICollection<TestQuestion> TestQuestions { get; set; } = new List<TestQuestion>();
    public ICollection<TestAttempt> TestAttempts { get; set; } = new List<TestAttempt>();
}
