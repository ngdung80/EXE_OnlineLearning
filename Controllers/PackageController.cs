using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class PackageController : Controller
{
    private readonly IPackageService _packageService;
    private readonly IStudentPackageService _studentPackageService;
    private readonly IUserService _userService;
    private readonly IWalletService _walletService;
    private readonly ITransactionService _transactionService;
    private readonly IGradeService _gradeService;
    private readonly ISubjectService _subjectService;

    public PackageController(IPackageService packageService, IStudentPackageService studentPackageService,
        IUserService userService, IWalletService walletService, ITransactionService transactionService,
        IGradeService gradeService, ISubjectService subjectService)
    {
        _packageService = packageService;
        _studentPackageService = studentPackageService;
        _userService = userService;
        _walletService = walletService;
        _transactionService = transactionService;
        _gradeService = gradeService;
        _subjectService = subjectService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> List()
    {
        var packages = await _packageService.GetActiveAsync();
        return View(packages);
    }

    [Authorize(Roles = "Parent")]
    public async Task<IActionResult> Purchase(int packageId, int? gradeId, int? studentId)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var package = await _packageService.GetByIdAsync(packageId);
        if (package == null) return NotFound();

        ViewBag.Package = package;
        ViewBag.Packages = await _packageService.GetActiveAsync();
        ViewBag.Students = await _userService.GetLinkedStudentsAsync(parentId);
        ViewBag.Grades = await _gradeService.GetActiveAsync();
        ViewBag.SelectedGradeId = gradeId;
        ViewBag.SelectedStudentId = studentId;
        return View();
    }

    [Authorize(Roles = "Parent")]
    [HttpPost]
    public async Task<IActionResult> Purchase(int packageId, int studentId, int gradeId)
    {
        var parentId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var package = await _packageService.GetByIdAsync(packageId);
        var wallet = await _walletService.GetByParentIdAsync(parentId);

        if (wallet == null || wallet.Balance < package!.Price)
        {
            TempData["Error"] = "Insufficient wallet balance!";
            return RedirectToAction(nameof(Purchase), new { packageId, gradeId, studentId });
        }

        if (await _studentPackageService.HasActivePackageForGradeAsync(studentId, packageId, gradeId))
        {
            TempData["Error"] = "Student already has an active package for this grade!";
            return RedirectToAction(nameof(Purchase), new { packageId, gradeId, studentId });
        }

        var subjectIds = await _subjectService.GetSubjectIdsByGradeIdAsync(gradeId);
        if (!subjectIds.Any()) { TempData["Error"] = "No subjects found for this grade."; return RedirectToAction(nameof(Purchase), new { packageId }); }

        var startDate = DateOnly.FromDateTime(DateTime.Now);
        var endDate = startDate.AddDays(package!.Duration);
        var studentPackageId = await _studentPackageService.InsertForGradeAsync(studentId, packageId, gradeId, startDate, endDate, subjectIds);

        // Deduct from wallet
        await _walletService.UpdateBalanceAsync(wallet.WalletId, wallet.Balance - package.Price);

        // Record wallet transaction
        await _walletService.InsertTransactionAsync(new WalletTransaction
        {
            WalletId = wallet.WalletId,
            Amount = package.Price,
            TransactionType = "Purchase",
            Description = $"Purchased package {package.PackageName}",
            CreatedAt = DateTime.Now,
            Status = "Completed",
            PackageId = packageId,
            StudentPackageId = studentPackageId
        });

        // Record transaction
        await _transactionService.InsertAsync(new Transaction
        {
            UserId = parentId,
            PackageId = packageId,
            StudentPackageId = studentPackageId,
            MenteeCount = 1,
            Amount = package.Price,
            Status = "Completed"
        });

        TempData["Success"] = "Package purchased successfully!";
        return RedirectToAction(nameof(Purchase), new { packageId, studentId, gradeId });
    }

    // --- ADMIN ACTIONS ---
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var packages = await _packageService.GetAllAsync();
        return View(packages);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(Package package)
    {
        await _packageService.AddAsync(package);
        TempData["Success"] = "Package created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var package = await _packageService.GetByIdAsync(id);
        if (package == null) return NotFound();
        return View(package);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Edit(Package package)
    {
        await _packageService.UpdateAsync(package);
        TempData["Success"] = "Package updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _packageService.DeleteAsync(id);
        TempData["Success"] = "Package deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
