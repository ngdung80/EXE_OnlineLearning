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
    private readonly PayOSClient _payOS;
    private readonly IConfiguration _config;

    public PaymentController(
        IPackageService packageService,
        IStudentPackageService studentPackageService,
        ISubjectService subjectService,
        ITransactionService transactionService,
        PayOSClient payOS,
        IConfiguration config)
    {
        _packageService = packageService;
        _studentPackageService = studentPackageService;
        _subjectService = subjectService;
        _transactionService = transactionService;
        _payOS = payOS;
        _config = config;
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, (int studentId, int gradeId)> _pendingAssignments = new();

    [HttpPost]
    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> CreatePaymentUrl(int packageId, int studentId, int gradeId)
    {
        var package = await _packageService.GetByIdAsync(packageId);
        if (package == null) return NotFound();

        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var transaction = new Transaction
        {
            UserId = parentId,
            PackageId = packageId,
            MenteeCount = 1,
            Amount = package.Price,
            Status = "Pending",
            TransactionDate = DateTime.Now
        };
        await _transactionService.InsertAsync(transaction);

        _pendingAssignments[transaction.TransactionId] = (studentId, gradeId);

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

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Webhook([FromBody] PayOS.Models.Webhooks.Webhook webhookBody)
    {
        try
        {
            WebhookData data = await _payOS.Webhooks.VerifyAsync(webhookBody);

            if (data.Code == "00")
            {
                await CompleteTransaction((int)data.OrderCode);
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
            if (_pendingAssignments.TryGetValue(transactionId, out var assignment))
            {
                var package = await _packageService.GetByIdAsync(transaction.PackageId);
                if (package != null)
                {
                    var subjectIds = await _subjectService.GetSubjectIdsByGradeIdAsync(assignment.gradeId);
                    var startDate = DateOnly.FromDateTime(DateTime.Now);
                    var endDate = startDate.AddMonths(package.Duration);
                    
                    var studentPackageId = await _studentPackageService.InsertForGradeAsync(assignment.studentId, transaction.PackageId, assignment.gradeId, startDate, endDate, subjectIds);
                    
                    transaction.StudentPackageId = studentPackageId;
                    transaction.Status = "Completed";
                    await _transactionService.UpdateAsync(transaction);
                    
                    // Xóa khỏi cache sau khi hoàn thành
                    _pendingAssignments.TryRemove(transactionId, out _);
                }
            }
            else 
            {
                transaction.Status = "Completed";
                await _transactionService.UpdateAsync(transaction);
            }
        }
    }
}
