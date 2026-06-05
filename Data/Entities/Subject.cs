using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Subject
{
    public int SubjectId { get; set; }
    public int GradeId { get; set; }
    public string SubjectName { get; set; } = null!;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Image { get; set; }

    public Grade Grade { get; set; } = null!;
    public ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
    public ICollection<StudentPackage> StudentPackages { get; set; } = new List<StudentPackage>();
}
