using System;

namespace POT_System_ASPNET.Data.Entities;

public class Transaction
{
    public int TransactionId { get; set; }
    public int UserId { get; set; }
    public int PackageId { get; set; }
    public int? StudentPackageId { get; set; }
    public int MenteeCount { get; set; }
    public double Amount { get; set; }
    public DateTime? TransactionDate { get; set; }
    public string? Status { get; set; }

    public User User { get; set; } = null!;
    public Package Package { get; set; } = null!;
}
