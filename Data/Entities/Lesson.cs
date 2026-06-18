using System;
using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Lesson
{
    public int LessonId { get; set; }
    public string LessonName { get; set; } = null!;
    public int ChapterId { get; set; }
    public string? ContentText { get; set; }
    public string? FileUrl { get; set; }
    public string? Status { get; set; }
    public DateTime? InactiveDate { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? DeletedDate { get; set; }
    public string? VocabularyJson { get; set; }

    public Chapter Chapter { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Test> Tests { get; set; } = new List<Test>();
}
