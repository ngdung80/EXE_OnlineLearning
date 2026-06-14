using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;

namespace POT_System_ASPNET.Controllers;

[Authorize(Roles = "Admin")]
public class TransactionController : Controller
{
    private readonly AppDbContext _context;

    public TransactionController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Get all course purchases
        var purchases = await _context.Transactions
            .Include(t => t.User)
            .Include(t => t.Package)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();

        return View(purchases);
    }
}
