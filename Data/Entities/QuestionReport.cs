using System;

namespace POT_System_ASPNET.Data.Entities;

public class QuestionReport
{
    public int ReportId { get; set; }
    public int QuestionId { get; set; }
    public int ReportedBy { get; set; }
    public string? Reason { get; set; }
    public string? Status { get; set; }   // Pending / Reviewed / Resolved
    public DateTime? CreatedAt { get; set; }
    public string? ReviewNote { get; set; }
    public string? Resolution { get; set; }
    public int? ReviewedBy { get; set; }

    public Question Question { get; set; } = null!;
    public User Reporter { get; set; } = null!;
    // Alias for backward compat
    public User? Student => Reporter;
    public int StudentId => ReportedBy;
}
