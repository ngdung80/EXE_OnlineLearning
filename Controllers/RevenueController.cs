using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin")]
public class RevenueController : Controller
{
    private readonly AppDbContext _context;

    public RevenueController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var totalTopUp = await _context.WalletTransactions
            .Where(wt => wt.TransactionType == "TopUp" && wt.Status == "Completed")
            .SumAsync(wt => wt.Amount);

        var totalPurchase = await _context.Transactions
            .Where(t => t.Status == "Completed")
            .SumAsync(t => t.Amount);

        ViewBag.TotalRevenue = totalTopUp + totalPurchase;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRevenueData(int? year)
    {
        var targetYear = year ?? DateTime.Now.Year;

        var topups = await _context.WalletTransactions
            .Where(wt => wt.TransactionType == "TopUp" && wt.Status == "Completed" && wt.CreatedAt.HasValue && wt.CreatedAt.Value.Year == targetYear)
            .GroupBy(wt => wt.CreatedAt!.Value.Month)
            .Select(g => new { Month = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var purchases = await _context.Transactions
            .Where(t => t.Status == "Completed" && t.TransactionDate.HasValue && t.TransactionDate.Value.Year == targetYear)
            .GroupBy(t => t.TransactionDate!.Value.Month)
            .Select(g => new { Month = g.Key, Amount = g.Sum(x => x.Amount) })
            .ToListAsync();

        var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var combined = new List<object>();

        for (int i = 1; i <= 12; i++)
        {
            var topUpAmount = topups.FirstOrDefault(t => t.Month == i)?.Amount ?? 0;
            var purchaseAmount = purchases.FirstOrDefault(p => p.Month == i)?.Amount ?? 0;
            combined.Add(new 
            {
                Label = months[i - 1],
                TotalRevenue = topUpAmount + purchaseAmount
            });
        }

        return Json(combined);
    }
}
