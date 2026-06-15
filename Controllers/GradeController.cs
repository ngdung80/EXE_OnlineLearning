using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Controllers;

[Authorize]
public class GradeController : Controller
{
    private readonly IGradeService _gradeService;
    public GradeController(IGradeService gradeService) => _gradeService = gradeService;

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Admin") || User.IsInRole("Content Manager") || User.IsInRole("Content Approver"))
        {
            return View(await _gradeService.GetAllAsync());
        }
        return View(await _gradeService.GetActiveAsync());
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create() => View();

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(Grade grade)
    {
        if (!ModelState.IsValid) return View(grade);
        await _gradeService.AddAsync(grade);
        TempData["Success"] = "Grade added successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var grade = await _gradeService.GetByIdAsync(id);
        if (grade == null) return NotFound();
        return View(grade);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Edit(Grade grade)
    {
        if (!ModelState.IsValid) return View(grade);
        await _gradeService.UpdateAsync(grade);
        TempData["Success"] = "Grade updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _gradeService.DeleteAsync(id);
        TempData["Success"] = "Grade deleted.";
        return RedirectToAction(nameof(Index));
    }
}
