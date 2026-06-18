using System;

namespace POT_System_ASPNET.Models;

public class CertificateViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = null!;
    public int GradeId { get; set; }
    public string GradeName { get; set; } = null!;
    public int ChapterId { get; set; }
    public string ChapterName { get; set; } = null!;
    public DateTime CompletedDate { get; set; }
}
