using System;
using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class Wallet
{
    public int WalletId { get; set; }
    public int ParentId { get; set; }
    public double Balance { get; set; }
    public DateTime? LastUpdated { get; set; }

    public User Parent { get; set; } = null!;
    public ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
