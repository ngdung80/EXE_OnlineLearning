using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using PayOS.Models.Webhooks;
using PayOS.Models.V2.PaymentRequests;
using POT_System_ASPNET.Data.Entities;
using POT_System_ASPNET.Services;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class PaymentController : Controller
{
    private readonly IPackageService _packageService;
    private readonly IStudentPackageService _studentPackageService;
    private readonly ISubjectService _subjectService;
    private readonly ITransactionService _transactionService;
    private readonly IWalletService _walletService;
    private readonly PayOSClient _payOS;
    private readonly IConfiguration _config;

    public PaymentController(
        IPackageService packageService,
        IStudentPackageService studentPackageService,
        ISubjectService subjectService,
        ITransactionService transactionService,
        IWalletService walletService,
        PayOSClient payOS,
        IConfiguration config)
    {
        _packageService = packageService;
        _studentPackageService = studentPackageService;
        _subjectService = subjectService;
        _transactionService = transactionService;
        _walletService = walletService;
        _payOS = payOS;
        _config = config;
    }

    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> CreatePaymentUrl(int packageId, int studentId, int gradeId)
    {
        var package = await _packageService.GetByIdAsync(packageId);
        if (package == null) return NotFound();

        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Lưu StudentId và GradeId xuống DB ngay — không dùng RAM để tránh mất khi restart
        var transaction = new Transaction
        {
            UserId = parentId,
            PackageId = packageId,
            StudentId = studentId,
            GradeId = gradeId,
            MenteeCount = 1,
            Amount = package.Price,
            Status = "Pending",
            TransactionDate = DateTime.Now
        };
        await _transactionService.InsertAsync(transaction);

        var returnUrl = _config["PayOS:ReturnUrl"] + $"?transactionId={transaction.TransactionId}";
        var cancelUrl = _config["PayOS:CancelUrl"] + $"?transactionId={transaction.TransactionId}";

        long orderCode = transaction.TransactionId;
        long amountInVND = (long)package.Price;
        
        PaymentLinkItem item = new PaymentLinkItem { Name = package.PackageName, Quantity = 1, Price = amountInVND };
        List<PaymentLinkItem> items = new List<PaymentLinkItem> { item };
        
        CreatePaymentLinkRequest paymentData = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amountInVND,
            Description = $"ma don {orderCode}", // Simplified description to avoid payOS validation issues or special chars
            Items = items,
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl
        };

        CreatePaymentLinkResponse createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);

        return Redirect(createPayment.CheckoutUrl);
    }

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Success(int transactionId)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var transaction = await _transactionService.GetByIdAsync(transactionId, parentId);
        
        if (transaction == null) return NotFound();

        if (transaction.Status != "Completed")
        {
            try 
            {
                PaymentLink paymentInfo = await _payOS.PaymentRequests.GetAsync(transactionId);
                if (paymentInfo.Status == PaymentLinkStatus.Paid)
                {
                    await CompleteTransaction(transactionId);
                    transaction = await _transactionService.GetByIdAsync(transactionId, parentId);
                }
            } 
            catch { }
        }

        if (transaction?.Status == "Completed")
        {
            ViewBag.PackageName = transaction.Package?.PackageName ?? "Gói học tập";
            ViewBag.Amount = transaction.Amount;
            ViewBag.TransactionId = transactionId.ToString();
            return View("PaymentSuccess");
        }
        else
        {
            TempData["Error"] = "Thanh toán chưa hoàn tất. Nếu bạn đã chuyển tiền, vui lòng đợi trong giây lát hoặc liên hệ hỗ trợ.";
            return View("PaymentFail");
        }
    }

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Cancel(int transactionId)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var transaction = await _transactionService.GetByIdAsync(transactionId, parentId);
        
        if (transaction != null && transaction.Status == "Pending")
        {
            transaction.Status = "Cancelled";
            await _transactionService.UpdateAsync(transaction);
        }

        TempData["Error"] = "Bạn đã hủy thanh toán.";
        return View("PaymentFail");
    }

    [Authorize(Roles = "Parent")]
    [HttpPost]
    public async Task<IActionResult> CreateTopUpUrl(double amount)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var wallet = await _walletService.GetOrCreateAsync(parentId);

        // Kiểm tra xem đã có Pending transaction cùng số tiền trong 10 phút qua chưa
        // → dùng lại để tránh sinh record Pending thừa khi user bấm nút nhiều lần
        var walletTransaction = await _walletService.GetPendingTopUpAsync(wallet.WalletId, amount);

        if (walletTransaction == null)
        {
            walletTransaction = new WalletTransaction
            {
                WalletId = wallet.WalletId,
                Amount = amount,
                TransactionType = "TopUp",
                Description = "Nạp tiền vào ví",
                CreatedAt = DateTime.Now,
                Status = "Pending"
            };
            await _walletService.InsertTransactionAsync(walletTransaction);
        }

        long orderCode = walletTransaction.WalletTransactionId + 100000000;
        long amountInVND = (long)amount;

        var returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/TopUpSuccess?walletTransactionId={walletTransaction.WalletTransactionId}";
        var cancelUrl = $"{Request.Scheme}://{Request.Host}/Payment/TopUpCancel?walletTransactionId={walletTransaction.WalletTransactionId}";

        PaymentLinkItem item = new PaymentLinkItem { Name = "Nạp tiền vào ví", Quantity = 1, Price = amountInVND };
        List<PaymentLinkItem> items = new List<PaymentLinkItem> { item };

        CreatePaymentLinkRequest paymentData = new CreatePaymentLinkRequest
        {
            OrderCode = orderCode,
            Amount = amountInVND,
            Description = $"nap tien {orderCode}",
            Items = items,
            CancelUrl = cancelUrl,
            ReturnUrl = returnUrl
        };

        CreatePaymentLinkResponse createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);
        return Redirect(createPayment.CheckoutUrl);
    }

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> TopUpSuccess(int walletTransactionId, string code = "", string status = "")
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var wallet = await _walletService.GetByParentIdAsync(parentId);
        
        if (wallet == null) return NotFound();

        long orderCode = walletTransactionId + 100000000;
        bool isSuccess = false;
        try 
        {
            PaymentLink paymentInfo = await _payOS.PaymentRequests.GetAsync((int)orderCode);
            if (paymentInfo.Status == PaymentLinkStatus.Paid)
            {
                isSuccess = true;
            }
        } 
        catch { }

        if (isSuccess || (code == "00" && (status == "PAID" || status == "SUCCESS")))
        {
            await CompleteTopUpTransaction(walletTransactionId);
            TempData["Success"] = "Nạp tiền thành công.";
        }
        else
        {
            TempData["Success"] = "Giao dịch nạp tiền đang được xử lý.";
        }

        return RedirectToAction("Index", "Wallet");
    }

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> TopUpCancel(int walletTransactionId)
    {
        // For simplicity, we just redirect back with a message
        TempData["Error"] = "Bạn đã hủy nạp tiền.";
        return RedirectToAction("Index", "Wallet");
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Webhook([FromBody] PayOS.Models.Webhooks.Webhook webhookBody)
    {
        try
        {
            WebhookData data = await _payOS.Webhooks.VerifyAsync(webhookBody);

            if (data.Code == "00")
            {
                if (data.OrderCode > 100000000)
                {
                    await CompleteTopUpTransaction((int)(data.OrderCode - 100000000));
                }
                else
                {
                    await CompleteTransaction((int)data.OrderCode);
                }
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    private async Task CompleteTransaction(int transactionId)
    {
        var transaction = await _transactionService.GetByIdAsync(transactionId);
        if (transaction != null && transaction.Status == "Pending")
        {
            // Đọc StudentId và GradeId từ DB (đã lưu lúc tạo đơn) — không còn phụ thuộc RAM
            if (transaction.StudentId.HasValue && transaction.GradeId.HasValue)
            {
                var package = await _packageService.GetByIdAsync(transaction.PackageId);
                if (package != null)
                {
                    var subjectIds = await _subjectService.GetSubjectIdsByGradeIdAsync(transaction.GradeId.Value);
                    var startDate = DateOnly.FromDateTime(DateTime.Now);
                    var endDate = startDate.AddMonths(package.Duration);

                    var studentPackageId = await _studentPackageService.InsertForGradeAsync(
                        transaction.StudentId.Value, transaction.PackageId,
                        transaction.GradeId.Value, startDate, endDate, subjectIds);

                    transaction.StudentPackageId = studentPackageId;
                    transaction.Status = "Completed";
                    await _transactionService.UpdateAsync(transaction);
                }
            }
            else
            {
                transaction.Status = "Completed";
                await _transactionService.UpdateAsync(transaction);
            }
        }
    }

    private async Task CompleteTopUpTransaction(int walletTransactionId)
    {
        var walletTransaction = await _walletService.GetTransactionByIdAsync(walletTransactionId);
        if (walletTransaction != null && walletTransaction.Status == "Pending")
        {
            walletTransaction.Status = "Completed";
            await _walletService.UpdateTransactionAsync(walletTransaction);

            var wallet = await _walletService.GetWalletByIdAsync(walletTransaction.WalletId);
            if (wallet != null)
            {
                await _walletService.UpdateBalanceAsync(wallet.WalletId, wallet.Balance + walletTransaction.Amount);
            }
        }
    }
}
