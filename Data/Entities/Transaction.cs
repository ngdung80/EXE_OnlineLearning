using System;

namespace POT_System_ASPNET.Data.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    /// <summary>ParentId — người mua (phụ huynh)</summary>
    public int UserId { get; set; }
    public int PackageId { get; set; }
    public int? StudentPackageId { get; set; }
    /// <summary>StudentId — học sinh được mua gói (lưu xuống DB để không mất khi restart)</summary>
    public int? StudentId { get; set; }
    /// <summary>GradeId tương ứng khi mua gói</summary>
    public int? GradeId { get; set; }
    public int MenteeCount { get; set; }
    public double Amount { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? Status { get; set; }

    public User User { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
