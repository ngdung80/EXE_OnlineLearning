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
    public async Task<IActionResult> GetRevenueData(int? year, int? month, int? day)
    {
        var targetYear = year ?? DateTime.Now.Year;
        
        // Query WalletTransactions (TopUp)
        var topupsQuery = _context.WalletTransactions
            .Where(wt => wt.TransactionType == "TopUp" && wt.Status == "Completed" && wt.CreatedAt.HasValue);

        // Query Transactions (Gói học)
        var purchasesQuery = _context.Transactions
            .Where(t => t.Status == "Completed" && t.TransactionDate.HasValue);

        // Filter queries by year
        topupsQuery = topupsQuery.Where(wt => wt.CreatedAt!.Value.Year == targetYear);
        purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Year == targetYear);

        if (month.HasValue && month.Value > 0)
        {
            topupsQuery = topupsQuery.Where(wt => wt.CreatedAt!.Value.Month == month.Value);
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Month == month.Value);
        }

        if (day.HasValue && day.Value > 0)
        {
            topupsQuery = topupsQuery.Where(wt => wt.CreatedAt!.Value.Day == day.Value);
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Day == day.Value);
        }

        var topupsList = await topupsQuery.ToListAsync();
        var purchasesList = await purchasesQuery.ToListAsync();

        var combined = new List<object>();

        // Scenario 1: Year, Month, and Day are all selected (Show hourly breakdown)
        if (month.HasValue && month.Value > 0 && day.HasValue && day.Value > 0)
        {
            var hourlyTopups = topupsList
                .GroupBy(wt => wt.CreatedAt!.Value.Hour)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var hourlyPurchases = purchasesList
                .GroupBy(t => t.TransactionDate!.Value.Hour)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            for (int h = 0; h < 24; h++)
            {
                var topUpAmount = hourlyTopups.ContainsKey(h) ? hourlyTopups[h] : 0;
                var purchaseAmount = hourlyPurchases.ContainsKey(h) ? hourlyPurchases[h] : 0;
                combined.Add(new
                {
                    Label = $"{h:D2}:00",
                    TotalRevenue = topUpAmount + purchaseAmount
                });
            }
        }
        // Scenario 2: Year and Month are selected, Day is "Tất cả" (Show daily breakdown)
        else if (month.HasValue && month.Value > 0)
        {
            int daysInMonth = DateTime.DaysInMonth(targetYear, month.Value);

            var dailyTopups = topupsList
                .GroupBy(wt => wt.CreatedAt!.Value.Day)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var dailyPurchases = purchasesList
                .GroupBy(t => t.TransactionDate!.Value.Day)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            for (int d = 1; d <= daysInMonth; d++)
            {
                var topUpAmount = dailyTopups.ContainsKey(d) ? dailyTopups[d] : 0;
                var purchaseAmount = dailyPurchases.ContainsKey(d) ? dailyPurchases[d] : 0;
                combined.Add(new
                {
                    Label = $"Ngày {d}",
                    TotalRevenue = topUpAmount + purchaseAmount
                });
            }
        }
        // Scenario 3: Only Year is selected, Month and Day are "Tất cả" (Show monthly breakdown)
        else
        {
            var monthlyTopups = topupsList
                .GroupBy(wt => wt.CreatedAt!.Value.Month)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var monthlyPurchases = purchasesList
                .GroupBy(t => t.TransactionDate!.Value.Month)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var months = new[] { "Thg 1", "Thg 2", "Thg 3", "Thg 4", "Thg 5", "Thg 6", "Thg 7", "Thg 8", "Thg 9", "Thg 10", "Thg 11", "Thg 12" };

            for (int m = 1; m <= 12; m++)
            {
                var topUpAmount = monthlyTopups.ContainsKey(m) ? monthlyTopups[m] : 0;
                var purchaseAmount = monthlyPurchases.ContainsKey(m) ? monthlyPurchases[m] : 0;
                combined.Add(new
                {
                    Label = months[m - 1],
                    TotalRevenue = topUpAmount + purchaseAmount
                });
            }
        }

        return Json(combined);
    }
}
