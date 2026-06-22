using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using System.Text;
using System.Xml;

namespace POT_System_ASPNET.Controllers;

public class SitemapController : Controller
{
    private readonly AppDbContext _context;
    private const string BaseUrl = "https://plo-learning.com";

    public SitemapController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Index()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // ── Static pages ────────────────────────────────────────────────────
        var staticUrls = new[]
        {
            ("",                     "1.0",  "daily"),
            ("/Account/Login",       "0.8",  "monthly"),
            ("/Account/Register",    "0.7",  "monthly"),
            ("/Home/Index",          "1.0",  "daily"),
            ("/Grade",               "0.9",  "weekly"),
            ("/Package",             "0.8",  "weekly"),
        };

        foreach (var (path, priority, freq) in staticUrls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{BaseUrl}{path}</loc>");
            sb.AppendLine($"    <changefreq>{freq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }

        // ── Grades ──────────────────────────────────────────────────────────
        var grades = await _context.Grades.ToListAsync();
        foreach (var grade in grades)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{BaseUrl}/Grade/Detail/{grade.GradeId}</loc>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.8</priority>");
            sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }

        // ── Subjects ────────────────────────────────────────────────────────
        var subjects = await _context.Subjects.ToListAsync();
        foreach (var subject in subjects)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{BaseUrl}/Subject/Detail/{subject.SubjectId}</loc>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.7</priority>");
            sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }

        // ── Lessons ─────────────────────────────────────────────────────────
        var lessons = await _context.Lessons.ToListAsync();
        foreach (var lesson in lessons)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{BaseUrl}/Lesson/Detail/{lesson.LessonId}</loc>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.6</priority>");
            sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
