using System;

namespace POT_System_ASPNET.Data.Entities;

public class MentorAssignment
{
    public int AssignmentId { get; set; }
    public int MentorId { get; set; }
    public int StudentId { get; set; }
    public DateTime? AssignedDate { get; set; }
    public string? Status { get; set; }

    public User Mentor { get; set; } = null!;
    public User Student { get; set; } = null!;
}
