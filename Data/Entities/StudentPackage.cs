using System;

namespace POT_System_ASPNET.Data.Entities;

public class StudentPackage
{
    public int StudentPackageId { get; set; }
    public int StudentId { get; set; }
    public int PackageId { get; set; }
    public int? GradeId { get; set; }
    public int? SubjectId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Status { get; set; }

    public User Student { get; set; } = null!;
    public Package Package { get; set; } = null!;
    public Grade? Grade { get; set; }
    public Subject? Subject { get; set; }
}
