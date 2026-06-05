using System;

namespace POT_System_ASPNET.Data.Entities;

public class WalletTransaction
{
    public int WalletTransactionId { get; set; }
    public int WalletId { get; set; }
    public double Amount { get; set; }
    public string? TransactionType { get; set; }   // Purchase / TopUp / Refund
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? Status { get; set; }
    public int? PackageId { get; set; }
    public int? StudentPackageId { get; set; }

    public Wallet Wallet { get; set; } = null!;
    public Package? Package { get; set; }
}
