using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface IWalletService
{
    Task<Wallet?> GetByParentIdAsync(int parentId);
    Task<Wallet?> GetWalletByIdAsync(int walletId);
    Task<Wallet> GetOrCreateAsync(int parentId);
    Task UpdateBalanceAsync(int walletId, double newBalance);
    Task TopUpAsync(int parentId, double amount, string description);
    Task<List<WalletTransaction>> GetTransactionsAsync(int walletId, int page, int pageSize);
    Task<int> CountTransactionsAsync(int walletId);
    Task InsertTransactionAsync(WalletTransaction wt);
    Task<WalletTransaction?> GetTransactionByIdAsync(int transactionId);
    Task UpdateTransactionAsync(WalletTransaction wt);
    /// <summary>Tìm giao dịch TopUp Pending của ví trong vòng 10 phút để tránh tạo duplicate</summary>
    Task<WalletTransaction?> GetPendingTopUpAsync(int walletId, double amount);
}

public class WalletService : IWalletService
{
    private readonly AppDbContext _db;
    public WalletService(AppDbContext db) => _db = db;

    public async Task<Wallet?> GetByParentIdAsync(int parentId)
        => await _db.Wallets.FirstOrDefaultAsync(w => w.ParentId == parentId);

    public async Task<Wallet?> GetWalletByIdAsync(int walletId)
        => await _db.Wallets.FindAsync(walletId);

    public async Task<Wallet> GetOrCreateAsync(int parentId)
    {
        var wallet = await GetByParentIdAsync(parentId);
        if (wallet == null)
        {
            wallet = new Wallet { ParentId = parentId, Balance = 0, LastUpdated = DateTime.Now };
            _db.Wallets.Add(wallet);
            await _db.SaveChangesAsync();
        }
        return wallet;
    }

    public async Task UpdateBalanceAsync(int walletId, double newBalance)
    {
        var wallet = await _db.Wallets.FindAsync(walletId);
        if (wallet != null) { wallet.Balance = newBalance; wallet.LastUpdated = DateTime.Now; await _db.SaveChangesAsync(); }
    }

    public async Task TopUpAsync(int parentId, double amount, string description)
    {
        var wallet = await GetOrCreateAsync(parentId);
        wallet.Balance += amount;
        wallet.LastUpdated = DateTime.Now;
        _db.WalletTransactions.Add(new WalletTransaction
        {
            WalletId = wallet.WalletId,
            Amount = amount,
            TransactionType = "TopUp",
            Description = description,
            CreatedAt = DateTime.Now,
            Status = "Completed"
        });
        await _db.SaveChangesAsync();
    }

    public async Task<List<WalletTransaction>> GetTransactionsAsync(int walletId, int page, int pageSize)
        => await _db.WalletTransactions.Where(wt => wt.WalletId == walletId)
            .OrderByDescending(wt => wt.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

    public async Task<int> CountTransactionsAsync(int walletId)
        => await _db.WalletTransactions.CountAsync(wt => wt.WalletId == walletId);

    public async Task InsertTransactionAsync(WalletTransaction wt) { _db.WalletTransactions.Add(wt); await _db.SaveChangesAsync(); }

    public async Task<WalletTransaction?> GetTransactionByIdAsync(int transactionId)
        => await _db.WalletTransactions.FindAsync(transactionId);

    public async Task UpdateTransactionAsync(WalletTransaction wt)
    {
        _db.WalletTransactions.Update(wt);
        await _db.SaveChangesAsync();
    }

    public async Task<WalletTransaction?> GetPendingTopUpAsync(int walletId, double amount)
    {
        var cutoff = DateTime.Now.AddMinutes(-10);
        return await _db.WalletTransactions
            .Where(wt => wt.WalletId == walletId
                      && wt.TransactionType == "TopUp"
                      && wt.Status == "Pending"
                      && wt.Amount == amount
                      && wt.CreatedAt >= cutoff)
            .OrderByDescending(wt => wt.CreatedAt)
            .FirstOrDefaultAsync();
    }
}

public interface ITransactionService
{
    Task<List<Transaction>> GetByUserIdAsync(int userId, string? transactionId, string? startDate, string? endDate, string? status, int page, int pageSize);
    Task<int> CountByUserIdAsync(int userId, string? transactionId, string? startDate, string? endDate, string? status);
    Task<Transaction?> GetByIdAsync(int transactionId, int userId);
    Task<List<Transaction>> GetAllAsync(DateTime? date, double? amount, int page, int pageSize);
    Task<int> CountAllAsync(DateTime? date, double? amount);
    Task InsertAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task<double> GetTotalIncomeAsync();
    Task<List<double>> GetMonthlyIncomeAsync(int year);
    Task<Transaction?> GetByIdAsync(int transactionId);
}

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _db;
    public TransactionService(AppDbContext db) => _db = db;

    public async Task<List<Transaction>> GetByUserIdAsync(int userId, string? transactionId, string? startDate, string? endDate, string? status, int page, int pageSize)
    {
        var query = _db.Transactions.Include(t => t.Package).Where(t => t.UserId == userId).AsQueryable();
        if (!string.IsNullOrEmpty(transactionId) && int.TryParse(transactionId, out int tid)) query = query.Where(t => t.TransactionId == tid);
        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd)) query = query.Where(t => t.TransactionDate >= sd);
        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed)) query = query.Where(t => t.TransactionDate <= ed);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        return await query.OrderByDescending(t => t.TransactionDate).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> CountByUserIdAsync(int userId, string? transactionId, string? startDate, string? endDate, string? status)
    {
        var query = _db.Transactions.Where(t => t.UserId == userId).AsQueryable();
        if (!string.IsNullOrEmpty(transactionId) && int.TryParse(transactionId, out int tid)) query = query.Where(t => t.TransactionId == tid);
        if (!string.IsNullOrEmpty(startDate) && DateTime.TryParse(startDate, out var sd)) query = query.Where(t => t.TransactionDate >= sd);
        if (!string.IsNullOrEmpty(endDate) && DateTime.TryParse(endDate, out var ed)) query = query.Where(t => t.TransactionDate <= ed);
        if (!string.IsNullOrEmpty(status)) query = query.Where(t => t.Status == status);
        return await query.CountAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int transactionId, int userId)
        => await _db.Transactions.Include(t => t.Package).FirstOrDefaultAsync(t => t.TransactionId == transactionId && t.UserId == userId);

    public async Task<Transaction?> GetByIdAsync(int transactionId)
        => await _db.Transactions.Include(t => t.Package).FirstOrDefaultAsync(t => t.TransactionId == transactionId);

    public async Task<List<Transaction>> GetAllAsync(DateTime? date, double? amount, int page, int pageSize)
    {
        var query = _db.Transactions.Include(t => t.Package).Include(t => t.User).AsQueryable();
        if (date.HasValue) query = query.Where(t => t.TransactionDate.HasValue && t.TransactionDate.Value.Date == date.Value.Date);
        if (amount.HasValue) query = query.Where(t => t.Amount == amount.Value);
        return await query.OrderByDescending(t => t.TransactionId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> CountAllAsync(DateTime? date, double? amount)
    {
        var query = _db.Transactions.AsQueryable();
        if (date.HasValue) query = query.Where(t => t.TransactionDate.HasValue && t.TransactionDate.Value.Date == date.Value.Date);
        if (amount.HasValue) query = query.Where(t => t.Amount == amount.Value);
        return await query.CountAsync();
    }

    public async Task InsertAsync(Transaction transaction)
    {
        transaction.TransactionDate = DateTime.Now;
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction) { _db.Transactions.Update(transaction); await _db.SaveChangesAsync(); }

    public async Task<double> GetTotalIncomeAsync()
        => await _db.Transactions.Where(t => t.Status == "Completed").SumAsync(t => t.Amount);

    public async Task<List<double>> GetMonthlyIncomeAsync(int year)
    {
        var result = new List<double>();
        for (int m = 1; m <= 12; m++)
        {
            var sum = await _db.Transactions
                .Where(t => t.Status == "Completed" && t.TransactionDate.HasValue && t.TransactionDate.Value.Year == year && t.TransactionDate.Value.Month == m)
                .SumAsync(t => (double?)t.Amount) ?? 0;
            result.Add(sum);
        }
        return result;
    }
}
