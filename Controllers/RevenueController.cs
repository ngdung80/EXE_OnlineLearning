using ClosedXML.Excel;
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
        var totalPurchase = await _context.Transactions
            .Where(t => t.Status == "Completed")
            .SumAsync(t => t.Amount);

        ViewBag.TotalRevenue = totalPurchase;

        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetRevenueData(int? year, int? month, int? day)
    {
        var targetYear = year ?? DateTime.Now.Year;

        // Query Transactions (Gói học)
        var purchasesQuery = _context.Transactions
            .Where(t => t.Status == "Completed" && t.TransactionDate.HasValue)
            .Where(t => t.TransactionDate!.Value.Year == targetYear);

        if (month.HasValue && month.Value > 0)
        {
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Month == month.Value);
        }

        if (day.HasValue && day.Value > 0)
        {
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Day == day.Value);
        }

        var purchasesList = await purchasesQuery.ToListAsync();

        var combined = new List<object>();

        // Scenario 1: Year, Month, and Day are all selected (Show hourly breakdown)
        if (month.HasValue && month.Value > 0 && day.HasValue && day.Value > 0)
        {
            var hourlyPurchases = purchasesList
                .GroupBy(t => t.TransactionDate!.Value.Hour)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            for (int h = 0; h < 24; h++)
            {
                var purchaseAmount = hourlyPurchases.ContainsKey(h) ? hourlyPurchases[h] : 0;
                combined.Add(new
                {
                    Label = $"{h:D2}:00",
                    TotalRevenue = purchaseAmount
                });
            }
        }
        // Scenario 2: Year and Month are selected, Day is "Tất cả" (Show daily breakdown)
        else if (month.HasValue && month.Value > 0)
        {
            int daysInMonth = DateTime.DaysInMonth(targetYear, month.Value);

            var dailyPurchases = purchasesList
                .GroupBy(t => t.TransactionDate!.Value.Day)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            for (int d = 1; d <= daysInMonth; d++)
            {
                var purchaseAmount = dailyPurchases.ContainsKey(d) ? dailyPurchases[d] : 0;
                combined.Add(new
                {
                    Label = $"Ngày {d}",
                    TotalRevenue = purchaseAmount
                });
            }
        }
        // Scenario 3: Only Year is selected, Month and Day are "Tất cả" (Show monthly breakdown)
        else
        {
            var monthlyPurchases = purchasesList
                .GroupBy(t => t.TransactionDate!.Value.Month)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var months = new[] { "Thg 1", "Thg 2", "Thg 3", "Thg 4", "Thg 5", "Thg 6", "Thg 7", "Thg 8", "Thg 9", "Thg 10", "Thg 11", "Thg 12" };

            for (int m = 1; m <= 12; m++)
            {
                var purchaseAmount = monthlyPurchases.ContainsKey(m) ? monthlyPurchases[m] : 0;
                combined.Add(new
                {
                    Label = months[m - 1],
                    TotalRevenue = purchaseAmount
                });
            }
        }

        return Json(combined);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(int? year, int? month, int? day)
    {
        var targetYear = year ?? DateTime.Now.Year;

        var purchasesQuery = _context.Transactions
            .Include(t => t.Package)
            .Where(t => t.Status == "Completed" && t.TransactionDate.HasValue)
            .Where(t => t.TransactionDate!.Value.Year == targetYear);

        if (month.HasValue && month.Value > 0)
        {
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Month == month.Value);
        }
        if (day.HasValue && day.Value > 0)
        {
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Day == day.Value);
        }

        var purchases = await purchasesQuery
            .Select(t => new
            {
                Date = t.TransactionDate!.Value,
                Type = "Mua gói " + t.Package.PackageName,
                t.Amount,
                t.Status
            }).ToListAsync();

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Doanh thu");

        // Title
        ws.Cell(1, 1).Value = "BÁO CÁO DOANH THU";
        var titleRange = ws.Range(1, 1, 1, 5);
        titleRange.Merge();
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 14;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Period info
        string period = $"Năm {targetYear}";
        if (month.HasValue && month.Value > 0) period += $" - Tháng {month.Value}";
        if (day.HasValue && day.Value > 0) period += $" - Ngày {day.Value}";
        ws.Cell(2, 1).Value = $"Kỳ báo cáo: {period}";
        ws.Range(2, 1, 2, 5).Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Headers
        int headerRow = 4;
        ws.Cell(headerRow, 1).Value = "STT";
        ws.Cell(headerRow, 2).Value = "Ngày / Giờ";
        ws.Cell(headerRow, 3).Value = "Loại giao dịch";
        ws.Cell(headerRow, 4).Value = "Số tiền (VNĐ)";
        ws.Cell(headerRow, 5).Value = "Trạng thái";

        var headerRange = ws.Range(headerRow, 1, headerRow, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#3498DB");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        // Data rows
        int row = headerRow + 1;
        int stt = 1;

        var allRows = purchases.Select(x => new { x.Date, x.Type, x.Amount, x.Status })
            .OrderBy(x => x.Date)
            .ToList();

        foreach (var item in allRows)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = item.Date.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 3).Value = item.Type;
            ws.Cell(row, 4).Value = (double)item.Amount;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 5).Value = item.Status;

            if (row % 2 == 0)
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF5FB");

            ws.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Range(row, 1, row, 5).Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            row++;
        }

        // Summary row
        if (allRows.Any())
        {
            ws.Cell(row, 3).Value = "TỔNG CỘNG";
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 4).Value = (double)allRows.Sum(x => x.Amount);
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 4).Style.Font.Bold = true;
            ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = XLColor.FromHtml("#D6EAF8");
            ws.Range(row, 1, row, 5).Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        }

        // Auto-fit columns
        ws.Columns(1, 5).AdjustToContents();
        ws.Column(1).Width = 6;
        ws.Column(2).Width = 20;
        ws.Column(3).Width = 22;
        ws.Column(4).Width = 18;
        ws.Column(5).Width = 14;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Seek(0, SeekOrigin.Begin);

        string fileName = $"BaoCaDoanhThu_{period.Replace(" ", "").Replace("/", "-")}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdfData(int? year, int? month, int? day)
    {
        // Returns detailed transaction rows for client-side PDF rendering via jsPDF
        var targetYear = year ?? DateTime.Now.Year;

        var purchasesQuery = _context.Transactions
            .Include(t => t.Package)
            .Where(t => t.Status == "Completed" && t.TransactionDate.HasValue)
            .Where(t => t.TransactionDate!.Value.Year == targetYear);

        if (month.HasValue && month.Value > 0)
        {
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Month == month.Value);
        }
        if (day.HasValue && day.Value > 0)
        {
            purchasesQuery = purchasesQuery.Where(t => t.TransactionDate!.Value.Day == day.Value);
        }

        var purchases = await purchasesQuery.Select(t => new
        {
            date = t.TransactionDate!.Value.ToString("dd/MM/yyyy HH:mm"),
            type = "Mua gói " + t.Package.PackageName,
            amount = t.Amount,
            status = t.Status
        }).ToListAsync();

        string period = $"Năm {targetYear}";
        if (month.HasValue && month.Value > 0) period += $" - Tháng {month.Value}";
        if (day.HasValue && day.Value > 0) period += $" - Ngày {day.Value}";

        return Json(new { period, rows = purchases, total = purchases.Sum(x => x.amount) });
    }
}
