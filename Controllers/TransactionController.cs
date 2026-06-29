using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;

using POT_System_ASPNET.Services;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin")]
public class TransactionController : Controller
{
    private readonly AppDbContext _context;
    private readonly ITransactionService _transactionService;
    private readonly IPackageService _packageService;
    private readonly IStudentPackageService _studentPackageService;
    private readonly ISubjectService _subjectService;

    public TransactionController(
        AppDbContext context,
        ITransactionService transactionService,
        IPackageService packageService,
        IStudentPackageService studentPackageService,
        ISubjectService subjectService)
    {
        _context = context;
        _transactionService = transactionService;
        _packageService = packageService;
        _studentPackageService = studentPackageService;
        _subjectService = subjectService;
    }

    public async Task<IActionResult> Index()
    {
        // Get all course purchases
        var purchases = await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Package)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        // Get all top-up/wallet transactions
        var walletTx = await _context.WalletTransactions
            .Where(wt => wt.TransactionType == "TopUp")
            .Include(wt => wt.Wallet)
            .ThenInclude(w => w.Parent)
            .OrderByDescending(wt => wt.CreatedAt)
            .ToListAsync();

        ViewBag.WalletTransactions = walletTx;

        return View(purchases);
    }

    [HttpPost]
    public async Task<IActionResult> ApproveWalletTransaction(int transactionId)
    {
        var tx = await _context.WalletTransactions
            .Include(w => w.Wallet)
            .FirstOrDefaultAsync(w => w.WalletTransactionId == transactionId);
            
        if (tx == null) return NotFound();

        if (tx.Status == "Pending")
        {
            tx.Status = "Completed";
            _context.WalletTransactions.Update(tx);
            
            var wallet = tx.Wallet;
            if (wallet != null)
            {
                wallet.Balance += tx.Amount;
                wallet.LastUpdated = DateTime.Now;
                _context.Wallets.Update(wallet);
            }
            await _context.SaveChangesAsync();
            TempData["Success"] = "Duyệt giao dịch nạp tiền thành công.";
        }
        else
        {
            TempData["Error"] = "Chỉ có thể duyệt giao dịch đang chờ (Pending).";
        }
        return RedirectToAction("Index", new { tab = "wallet" });
    }

    [HttpPost]
    public async Task<IActionResult> CancelWalletTransaction(int transactionId)
    {
        var tx = await _context.WalletTransactions.FindAsync(transactionId);
        if (tx == null) return NotFound();

        if (tx.Status == "Pending")
        {
            tx.Status = "Cancelled";
            _context.WalletTransactions.Update(tx);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã từ chối/hủy giao dịch nạp tiền.";
        }
        else
        {
            TempData["Error"] = "Chỉ có thể từ chối giao dịch đang chờ (Pending).";
        }
        return RedirectToAction("Index", new { tab = "wallet" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteWalletTransaction(int transactionId)
    {
        var tx = await _context.WalletTransactions.FindAsync(transactionId);
        if (tx == null) return NotFound();

        _context.WalletTransactions.Remove(tx);
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Đã xóa lịch sử giao dịch.";
        return RedirectToAction("Index", new { tab = "wallet" });
    }

    [HttpPost]
    public async Task<IActionResult> ApprovePackageTransaction(int transactionId)
    {
        var transaction = await _transactionService.GetByIdAsync(transactionId);
        if (transaction == null) return NotFound();

        if (transaction.Status == "Pending")
        {
            if (transaction.StudentId.HasValue && transaction.GradeId.HasValue)
            {
                var package = await _packageService.GetByIdAsync(transaction.PackageId);
                if (package != null)
                {
                    var subjectIds = await _subjectService.GetSubjectIdsByGradeIdAsync(transaction.GradeId.Value);
                    var startDate = DateOnly.FromDateTime(DateTime.Now);
                    var endDate = startDate.AddDays(package.Duration);

                    var studentPackageId = await _studentPackageService.InsertForGradeAsync(
                        transaction.StudentId.Value, transaction.PackageId,
                        transaction.GradeId.Value, startDate, endDate, subjectIds);

                    transaction.StudentPackageId = studentPackageId;
                    transaction.Status = "Completed";
                    await _transactionService.UpdateAsync(transaction);
                    TempData["Success"] = "Duyệt giao dịch mua gói học thành công. Học sinh đã được kích hoạt môn học.";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy gói học tương ứng.";
                }
            }
            else
            {
                transaction.Status = "Completed";
                await _transactionService.UpdateAsync(transaction);
                TempData["Success"] = "Duyệt giao dịch mua gói học thành công.";
            }
        }
        else
        {
            TempData["Error"] = "Chỉ có thể duyệt giao dịch đang chờ (Pending).";
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> CancelPackageTransaction(int transactionId)
    {
        var transaction = await _transactionService.GetByIdAsync(transactionId);
        if (transaction == null) return NotFound();

        if (transaction.Status == "Pending")
        {
            transaction.Status = "Cancelled";
            await _transactionService.UpdateAsync(transaction);
            TempData["Success"] = "Đã từ chối/hủy giao dịch mua gói học.";
        }
        else
        {
            TempData["Error"] = "Chỉ có thể từ chối giao dịch đang chờ (Pending).";
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> DeletePackageTransaction(int transactionId)
    {
        var transaction = await _context.Transactions.FindAsync(transactionId);
        if (transaction == null) return NotFound();

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Đã xóa lịch sử giao dịch mua gói.";
        return RedirectToAction("Index");
    }
}
