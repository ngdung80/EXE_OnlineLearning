using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Grade
{
    public int GradeId { get; set; }
    public string GradeName { get; set; } = null!;
    public string? Status { get; set; }
    public string? Description { get; set; }

    public ICollection<Subject> Subjects { get; set; } = new List<Subject>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
