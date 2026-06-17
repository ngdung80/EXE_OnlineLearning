using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    public UserController(IUserService userService) => _userService = userService;

    public async Task<IActionResult> Index(string? search, string? role, string? status, int page = 1)
    {
        const int pageSize = 10;
        var users = await _userService.SearchUsersAsync(search, role, status, page, pageSize);
        var total = await _userService.CountUsersAsync(search, role, status);
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Filter = new { search, role, status };
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.DeleteUserAsync(id);
        TempData["Success"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user != null)
        {
            user.Status = user.Status == "active" ? "inactive" : "active";
            await _userService.UpdateUserAsync(user);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, string fullName, string email, string role, string status)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
        {
            TempData["Error"] = "Họ tên và Email không được để trống.";
            return RedirectToAction(nameof(Index));
        }

        if (email.Trim().ToLower() != user.Email?.ToLower())
        {
            if (await _userService.EmailExistsAsync(email.Trim().ToLower()))
            {
                TempData["Error"] = "Email này đã được sử dụng bởi tài khoản khác.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.FullName = fullName;
        user.Email = email.Trim().ToLower();
        user.Role = role;
        user.Status = status.ToLower();

        await _userService.UpdateUserAsync(user);
        TempData["Success"] = "Cập nhật thông tin người dùng thành công.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Roles = "Parent")]
public class ParentController : Controller
{
    private readonly IUserService _userService;
    private readonly ITestAttemptService _testAttemptService;
    private readonly IStudentPackageService _studentPackageService;
    private readonly IEmailService _emailService;

    public ParentController(IUserService userService, ITestAttemptService testAttemptService, 
        IStudentPackageService studentPackageService, IEmailService emailService)
    {
        _userService = userService;
        _testAttemptService = testAttemptService;
        _studentPackageService = studentPackageService;
        _emailService = emailService;
    }

    public async Task<IActionResult> LinkedStudents()
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var students = await _userService.GetLinkedStudentsAsync(parentId);
        return View(students);
    }

    [HttpPost]
    public async Task<IActionResult> SendLinkCode([FromBody] SendCodeRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Email))
            {
                return Json(new { success = false, message = "Email không được để trống." });
            }

            var student = await _userService.GetByEmailAsync(req.Email.Trim().ToLower());
            if (student == null || student.Role != "Student")
            {
                return Json(new { success = false, message = "Không tìm thấy tài khoản Học sinh với email này." });
            }

            var code = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("LinkStudentOTP", code);
            HttpContext.Session.SetString("LinkStudentEmail", req.Email.Trim().ToLower());
            
            Console.WriteLine($"\n[LINK STUDENT OTP DEBUG] =========================================");
            Console.WriteLine($"[LINK STUDENT OTP DEBUG] EMAIL: {req.Email}");
            Console.WriteLine($"[LINK STUDENT OTP DEBUG] CODE: {code}");
            Console.WriteLine($"[LINK STUDENT OTP DEBUG] =========================================\n");

            await _emailService.SendVerificationCodeAsync(req.Email.Trim().ToLower(), code);

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            var code = HttpContext.Session.GetString("LinkStudentOTP");
            return Json(new { 
                success = false, 
                message = $"Lỗi gửi Email: {ex.Message}. [DEBUG]: Do SMTP chưa cấu hình, mã OTP của bạn là: {code}." 
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> LinkStudent(string studentEmail, string verificationCode)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        
        var storedCode = HttpContext.Session.GetString("LinkStudentOTP");
        var storedEmail = HttpContext.Session.GetString("LinkStudentEmail");

        if (string.IsNullOrEmpty(storedCode) || verificationCode != storedCode || storedEmail != studentEmail.Trim().ToLower())
        {
            TempData["Error"] = "Mã xác thực không chính xác hoặc đã hết hạn.";
            return RedirectToAction(nameof(LinkedStudents));
        }

        var student = await _userService.GetByEmailAsync(studentEmail);
        if (student == null || student.Role != "Student")
        {
            TempData["Error"] = "Không tìm thấy học sinh.";
            return RedirectToAction(nameof(LinkedStudents));
        }

        student.ParentId = parentId;
        await _userService.UpdateUserAsync(student);

        HttpContext.Session.Remove("LinkStudentOTP");
        HttpContext.Session.Remove("LinkStudentEmail");

        TempData["Success"] = "Liên kết tài khoản con thành công.";
        return RedirectToAction(nameof(LinkedStudents));
    }

    [HttpGet]
    public async Task<IActionResult> StudentProgress(int studentId)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var student = await _userService.GetByIdAsync(studentId);
        
        if (student == null || student.ParentId != parentId)
            return RedirectToAction(nameof(LinkedStudents));
            
        var attempts = await _testAttemptService.GetByStudentIdAsync(studentId);
        var packages = await _studentPackageService.GetByStudentIdAsync(studentId);
        
        ViewBag.Student = student;
        ViewBag.Packages = packages;
        
        return View(attempts);
    }

    /// <summary>Xem tiến trình học của các con trong một khối lớp cụ thể.</summary>
    [HttpGet]
    public async Task<IActionResult> ProgressByGrade(int gradeId)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var today = DateOnly.FromDateTime(DateTime.Now);

        // Lấy danh sách con
        var allChildren = await _userService.GetLinkedStudentsAsync(parentId);

        // Lọc các con có gói học active trong gradeId này
        var childrenInGrade = new List<POT_System_ASPNET.Data.Entities.User>();
        foreach (var child in allChildren)
        {
            var packages = await _studentPackageService.GetByStudentIdAsync(child.UserId);
            if (packages.Any(sp => sp.GradeId == gradeId && sp.Status == "Active" && sp.EndDate >= today))
                childrenInGrade.Add(child);
        }

        // Nếu chỉ có 1 con → redirect thẳng đến trang tiến trình
        if (childrenInGrade.Count == 1)
            return RedirectToAction(nameof(StudentProgress), new { studentId = childrenInGrade[0].UserId });

        // Nếu không có con nào → redirect về danh sách
        if (!childrenInGrade.Any())
            return RedirectToAction(nameof(LinkedStudents));

        // Nhiều con → hiển thị danh sách để chọn
        ViewBag.GradeId = gradeId;
        return View("SelectStudentForProgress", childrenInGrade);
    }
}

[Authorize(Roles = "Parent")]
public class WalletController : Controller
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService) => _walletService = walletService;

    public async Task<IActionResult> Index(int page = 1)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var wallet = await _walletService.GetOrCreateAsync(parentId);
        var transactions = await _walletService.GetTransactionsAsync(wallet.WalletId, page, 10);
        var total = await _walletService.CountTransactionsAsync(wallet.WalletId);
        ViewBag.Wallet = wallet;
        ViewBag.TotalPages = (int)Math.Ceiling((double)total / 10);
        ViewBag.CurrentPage = page;
        return View(transactions);
    }
}

[Authorize]
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;
    public NotificationController(INotificationService notificationService) => _notificationService = notificationService;

    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return View(await _notificationService.GetByUserIdAsync(userId));
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(int id)
    {
        await _notificationService.MarkReadAsync(id);
        return Ok();
    }
}
