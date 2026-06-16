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
}

[Authorize(Roles = "Parent")]
public class ParentController : Controller
{
    private readonly IUserService _userService;
    private readonly ITestAttemptService _testAttemptService;
    private readonly IStudentPackageService _studentPackageService;

    public ParentController(IUserService userService, ITestAttemptService testAttemptService, IStudentPackageService studentPackageService)
    {
        _userService = userService;
        _testAttemptService = testAttemptService;
        _studentPackageService = studentPackageService;
    }

    public async Task<IActionResult> LinkedStudents()
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var students = await _userService.GetLinkedStudentsAsync(parentId);
        return View(students);
    }

    [HttpPost]
    public async Task<IActionResult> LinkStudent(string studentEmail)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var student = await _userService.GetByEmailAsync(studentEmail);
        if (student == null || student.Role != "Student")
        {
            TempData["Error"] = "Student not found.";
            return RedirectToAction(nameof(LinkedStudents));
        }
        student.ParentId = parentId;
        await _userService.UpdateUserAsync(student);
        TempData["Success"] = "Student linked successfully.";
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
