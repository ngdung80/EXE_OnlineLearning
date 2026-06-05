using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Chapter
{
    public int ChapterId { get; set; }
    public int SubjectId { get; set; }
    public string ChapterName { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }

    public Subject Subject { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
