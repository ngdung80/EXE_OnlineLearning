using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IWalletService _walletService;

    public AccountController(IUserService userService, IEmailService emailService, IWalletService walletService)
    {
        _userService = userService;
        _emailService = emailService;
        _walletService = walletService;
    }

    // ── Login ────────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View();

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Please enter both username and password.";
            return View();
        }

        var user = await _userService.CheckLoginAsync(username.Trim().ToLower(), password);
        if (user == null)
        {
            ViewBag.Error = "Incorrect username or password.";
            return View();
        }

        if (user.Role == "Admin")
        {
            ViewBag.Error = "Incorrect username or password.";
            return View();
        }

        await SignInUserAsync(user);

        if (user.Role == "Parent")
        {
            var wallet = await _walletService.GetOrCreateAsync(user.UserId);
            HttpContext.Session.SetString("WalletBalance", wallet.Balance.ToString("F0"));
        }

        return RedirectToAction("Index", "Home");
    }

    // ── Admin Login ──────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult LoginAdmin() => View();

    [HttpPost]
    public async Task<IActionResult> LoginAdmin(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Please enter both username and password.";
            return View();
        }

        var user = await _userService.CheckLoginAsync(username.Trim().ToLower(), password);
        if (user == null || (user.Role != "Admin" && user.Role != "Content Approver"))
        {
            ViewBag.Error = "Invalid admin credentials.";
            return View();
        }

        await SignInUserAsync(user);
        return RedirectToAction("Index", "Home");
    }

    // ── Logout ───────────────────────────────────────────────────────────────
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    // ── Register ─────────────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(string username, string password, string confirmPassword,
        string email, string fullName, string phone, string dob, string role, string? specialization,
        string verificationCode)
    {
        var storedCode = HttpContext.Session.GetString("VerificationCode");
        var storedEmail = HttpContext.Session.GetString("VerificationEmail");
        var codeTimestamp = HttpContext.Session.GetString("CodeTimestamp");

        if (string.IsNullOrEmpty(storedCode) || verificationCode != storedCode || storedEmail != email)
        {
            ViewBag.Error = "Invalid verification code.";
            return View();
        }

        if (codeTimestamp != null && (DateTime.Now - DateTime.Parse(codeTimestamp)).TotalMinutes > 5)
        {
            HttpContext.Session.Remove("VerificationCode");
            ViewBag.Error = "Verification code expired.";
            return View();
        }

        if (await _userService.UsernameExistsAsync(username.ToLower())) { ViewBag.Error = "Username already exists."; return View(); }
        if (await _userService.EmailExistsAsync(email.ToLower())) { ViewBag.Error = "Email already in use."; return View(); }
        if (password != confirmPassword) { ViewBag.Error = "Passwords do not match."; return View(); }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$"))
        {
            ViewBag.Error = "Password must be 8+ chars with uppercase, lowercase, number, and special char.";
            return View();
        }

        if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
        {
            ViewBag.Error = "Phone must be 10 digits starting with 0.";
            return View();
        }

        if (!DateOnly.TryParse(dob, out var dobDate) || dobDate > DateOnly.FromDateTime(DateTime.Now))
        {
            ViewBag.Error = "Invalid date of birth.";
            return View();
        }

        var mappedRole = role.ToLower() switch
        {
            "mentee" => "Student",
            "content manager" => "Content Manager",
            _ => role
        };

        var user = new User
        {
            Username = username.ToLower(),
            Password = password,
            Email = email.ToLower(),
            Role = mappedRole,
            FullName = fullName,
            Phone = string.IsNullOrEmpty(phone) ? null : phone,
            Dob = dobDate,
            Status = mappedRole == "Content Manager" ? "pending" : "active",
            Specialization = mappedRole == "Content Manager" ? specialization : null
        };

        await _userService.InsertUserAsync(user);

        // Create wallet for Parent
        if (mappedRole == "Parent")
            await _walletService.GetOrCreateAsync(user.UserId);

        HttpContext.Session.Remove("VerificationCode");
        HttpContext.Session.Remove("VerificationEmail");
        HttpContext.Session.Remove("CodeTimestamp");

        ViewBag.Success = mappedRole == "Content Manager"
            ? "Registration successful! Please wait for Admin approval."
            : "Registration successful! Please login.";
        return View();
    }

    // ── Send Verification Code ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendCodeRequest req)
    {
        try
        {
            var code = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationEmail", req.Email);
            HttpContext.Session.SetString("CodeTimestamp", DateTime.Now.ToString("o"));
            await _emailService.SendVerificationCodeAsync(req.Email, code);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ── Forgot Password ──────────────────────────────────────────────────────
    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email, string verificationCode, string newPassword, string confirmPassword, string step)
    {
        if (step == "send")
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null) { ViewBag.Error = "Email not found."; return View(); }

            var code = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("ResetCode", code);
            HttpContext.Session.SetString("ResetEmail", email);
            HttpContext.Session.SetString("ResetTimestamp", DateTime.Now.ToString("o"));
            await _emailService.SendVerificationCodeAsync(email, code);
            ViewBag.EmailSent = true;
            ViewBag.Email = email;
        }
        else if (step == "verify")
        {
            var storedCode = HttpContext.Session.GetString("ResetCode");
            var storedEmail = HttpContext.Session.GetString("ResetEmail");

            if (storedCode != verificationCode || storedEmail != email)
            {
                ViewBag.Error = "Invalid verification code.";
                ViewBag.EmailSent = true;
                ViewBag.Email = email;
                return View();
            }

            if (newPassword != confirmPassword) { ViewBag.Error = "Passwords do not match."; ViewBag.Step = "reset"; ViewBag.Email = email; return View(); }

            await _userService.UpdatePasswordAsync(email, newPassword);
            HttpContext.Session.Remove("ResetCode");
            ViewBag.Success = "Password reset successfully! Please login.";
        }
        return View();
    }

    // ── Profile ──────────────────────────────────────────────────────────────
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> Profile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        return View(user);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet]
    public async Task<IActionResult> EditProfile()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        return View(user);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    public async Task<IActionResult> EditProfile(string fullName, string email, string? phone,
        string? dob, string? specialization, string? workTime, IFormFile? imageFile)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        user.FullName = fullName;
        user.Email = email;
        user.Phone = phone;
        if (!string.IsNullOrEmpty(dob) && DateOnly.TryParse(dob, out var d)) user.Dob = d;
        user.Specialization = specialization;
        user.WorkTime = workTime;

        if (imageFile != null && imageFile.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploads);
            var fileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            using var stream = new FileStream(Path.Combine(uploads, fileName), FileMode.Create);
            await imageFile.CopyToAsync(stream);
            user.Image = $"/uploads/profiles/{fileName}";
        }

        await _userService.UpdateUserAsync(user);
        ViewBag.Success = "Profile updated successfully.";
        return View(user);
    }

    // ── Change Password ──────────────────────────────────────────────────────
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View();

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
        {
            ViewBag.Error = "Current password is incorrect.";
            return View();
        }
        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "New passwords do not match.";
            return View();
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(newPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$"))
        {
            ViewBag.Error = "Password must be 8+ chars with uppercase, lowercase, number, and special char.";
            return View();
        }

        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _userService.UpdateUserAsync(user);
        ViewBag.Success = "Password changed successfully.";
        return View();
    }

    public IActionResult AccessDenied() => View();

    // ── Helper ───────────────────────────────────────────────────────────────
    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, user.Role),
            new("FullName", user.FullName ?? user.Username),
            new("Image", user.Image ?? "/assets/images/default-avatar.png")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

public class SendCodeRequest { public string Email { get; set; } = ""; }
