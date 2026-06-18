using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Package
{
    public int PackageId { get; set; }
    public string PackageName { get; set; } = null!;
    public string? Description { get; set; }
    public double Price { get; set; }
    public int Duration { get; set; }    // months
    public string? Status { get; set; }

    public ICollection<StudentPackage> StudentPackages { get; set; } = new List<StudentPackage>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
