using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;

namespace POT_System_ASPNET.Controllers;

public class HomeController : Controller
{
    private readonly IGradeService _gradeService;
    private readonly IPackageService _packageService;
    private readonly ILessonService _lessonService;

    public HomeController(IGradeService gradeService, IPackageService packageService, ILessonService lessonService)
    {
        _gradeService = gradeService;
        _packageService = packageService;
        _lessonService = lessonService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var grades = (await _gradeService.GetActiveAsync()).Take(3).ToList();
            var packages = (await _packageService.GetActiveAsync()).Take(3).ToList();
            ViewBag.Grades = grades;
            ViewBag.Packages = packages;
        }
        catch
        {
            ViewBag.Grades = new List<POT_System_ASPNET.Data.Entities.Grade>();
            ViewBag.Packages = new List<POT_System_ASPNET.Data.Entities.Package>();
            ViewBag.DbError = true;
        }
        return View();
    }

    public IActionResult Error() => View();
}
