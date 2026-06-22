using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Services;
using POT_System_ASPNET.Data.Entities;
using System.Security.Claims;

namespace POT_System_ASPNET.Controllers;

public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IWalletService _walletService;
    private readonly POT_System_ASPNET.Data.AppDbContext _db;

    public AccountController(IUserService userService, IEmailService emailService, IWalletService walletService, POT_System_ASPNET.Data.AppDbContext db)
    {
        _userService = userService;
        _emailService = emailService;
        _walletService = walletService;
        _db = db;
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
        else if (user.Role == "Student")
        {
            var totalCorrect = await _db.TestAttempts
                .Where(ta => ta.StudentId == user.UserId && ta.Score.HasValue)
                .SumAsync(ta => ta.CorrectAnswers ?? 0);
            int totalEarnedStars = totalCorrect * 10;
            
            int spentStars = 0;
            if (!string.IsNullOrEmpty(user.Specialization))
            {
                var unlockedList = user.Specialization.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var avId in unlockedList)
                {
                    spentStars += avId switch
                    {
                        "VIP_1" => 400,
                        "VIP_2" => 800,
                        "VIP_3" => 1200,
                        "VIP_4" => 1600,
                        "VIP_5" => 2000,
                        _ => 0
                    };
                }
            }
            int availableStars = Math.Max(0, totalEarnedStars - spentStars);
            HttpContext.Session.SetString("StudentStars", availableStars.ToString());
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

    [HttpGet]
    public async Task<IActionResult> CheckUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username)) return Json(new { exists = false });
        var exists = await _userService.UsernameExistsAsync(username.Trim().ToLower());
        return Json(new { exists });
    }

    [HttpGet]
    public async Task<IActionResult> CheckEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return Json(new { exists = false });
        var exists = await _userService.EmailExistsAsync(email.Trim().ToLower());
        return Json(new { exists });
    }

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
            ViewBag.Error = "Mã xác thực không đúng.";
            return View();
        }

        if (codeTimestamp != null && (DateTime.Now - DateTime.Parse(codeTimestamp)).TotalMinutes > 5)
        {
            HttpContext.Session.Remove("VerificationCode");
            ViewBag.Error = "Mã xác thực đã hết hạn (quá 5 phút).";
            return View();
        }

        var mappedRole = role.ToLower() switch
        {
            "mentee" => "Student",
            "content manager" => "Content Manager",
            _ => role
        };

        if (string.IsNullOrEmpty(username) && mappedRole == "Parent")
        {
            username = email;
        }

        if (string.IsNullOrEmpty(username))
        {
            ViewBag.Error = "Tên đăng nhập không được để trống.";
            return View();
        }

        if (await _userService.UsernameExistsAsync(username.ToLower())) { ViewBag.Error = "Tên đăng nhập đã tồn tại trên hệ thống."; return View(); }
        if (await _userService.EmailExistsAsync(email.ToLower())) { ViewBag.Error = "Email đã được sử dụng."; return View(); }
        if (password != confirmPassword) { ViewBag.Error = "Mật khẩu xác nhận không trùng khớp."; return View(); }

        if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$"))
        {
            ViewBag.Error = "Mật khẩu phải từ 8 ký tự trở lên, chứa nhất 1 chữ hoa, 1 chữ thường, 1 chữ số và 1 ký tự đặc biệt.";
            return View();
        }

        if (!string.IsNullOrEmpty(phone) && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^0\d{9}$"))
        {
            ViewBag.Error = "Số điện thoại phải gồm 10 chữ số và bắt đầu bằng số 0.";
            return View();
        }

        DateOnly dobDate = DateOnly.FromDateTime(DateTime.Now);
        if (!string.IsNullOrEmpty(dob))
        {
            if (!DateOnly.TryParse(dob, out dobDate) || dobDate > DateOnly.FromDateTime(DateTime.Now))
            {
                ViewBag.Error = "Ngày sinh không hợp lệ.";
                return View();
            }
        }

        var user = new User
        {
            Username = username.ToLower(),
            Password = password,
            Email = email.ToLower(),
            Role = mappedRole,
            FullName = fullName,
            Phone = string.IsNullOrEmpty(phone) ? null : phone,
            Dob = mappedRole == "Parent" ? null : dobDate,
            Status = mappedRole == "Content Manager" ? "pending" : "active",
            Specialization = mappedRole == "Content Manager" ? specialization : null
        };

        await _userService.InsertUserAsync(user);

        // Tạo ví cho Phụ huynh
        if (mappedRole == "Parent")
            await _walletService.GetOrCreateAsync(user.UserId);

        HttpContext.Session.Remove("VerificationCode");
        HttpContext.Session.Remove("VerificationEmail");
        HttpContext.Session.Remove("CodeTimestamp");

        TempData["Success"] = mappedRole == "Content Manager"
            ? "Đăng ký tài khoản thành công! Vui lòng chờ Ban quản trị phê duyệt vai trò Giáo viên."
            : "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";

        return RedirectToAction("Login");
    }

    // ── Send Verification Code ───────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendCodeRequest req)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(req.Email))
            {
                return Json(new { success = false, message = "Email không được để trống." });
            }

            if (await _userService.EmailExistsAsync(req.Email.Trim().ToLower()))
            {
                return Json(new { success = false, message = "Email này đã được sử dụng để đăng ký tài khoản khác." });
            }

            var code = new Random().Next(100000, 999999).ToString();
            HttpContext.Session.SetString("VerificationCode", code);
            HttpContext.Session.SetString("VerificationEmail", req.Email);
            HttpContext.Session.SetString("CodeTimestamp", DateTime.Now.ToString("o"));
            
            Console.WriteLine($"\n[OTP DEBUG] =========================================");
            Console.WriteLine($"[OTP DEBUG] EMAIL: {req.Email}");
            Console.WriteLine($"[OTP DEBUG] VERIFICATION CODE: {code}");
            Console.WriteLine($"[OTP DEBUG] =========================================\n");

            await _emailService.SendVerificationCodeAsync(req.Email, code);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            var code = HttpContext.Session.GetString("VerificationCode");
            return Json(new { 
                success = false, 
                message = $"Lỗi gửi Email: {ex.Message}. [DEBUG]: Do SMTP chưa cấu hình, vui lòng lấy mã xác thực trong Console Terminal của Server (Mã OTP: {code})." 
            });
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

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    public async Task<IActionResult> UpdateAvatar(string avatarPath)
    {
        if (string.IsNullOrEmpty(avatarPath)) return Json(new { success = false, message = "Đường dẫn trống." });
        if (!avatarPath.StartsWith("/assets/images/avatars/"))
        {
            return Json(new { success = false, message = "Đường dẫn không hợp lệ." });
        }

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        user.Image = avatarPath;
        await _userService.UpdateUserAsync(user);
        await SignInUserAsync(user);

        return Json(new { success = true });
    }

    // ── Avatar Shop ─────────────────────────────────────────────────────────
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> AvatarShop()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        // Calculate stars
        var totalCorrect = await _db.TestAttempts
            .Where(ta => ta.StudentId == userId && ta.Score.HasValue)
            .SumAsync(ta => ta.CorrectAnswers ?? 0);
        int totalEarnedStars = totalCorrect * 10;

        int spentStars = 0;
        var unlockedList = new List<string>();
        if (!string.IsNullOrEmpty(user.Specialization))
        {
            unlockedList = user.Specialization.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (var avId in unlockedList)
            {
                spentStars += avId switch
                {
                    "VIP_1" => 400,
                    "VIP_2" => 800,
                    "VIP_3" => 1200,
                    "VIP_4" => 1600,
                    "VIP_5" => 2000,
                    _ => 0
                };
            }
        }

        int availableStars = Math.Max(0, totalEarnedStars - spentStars);
        HttpContext.Session.SetString("StudentStars", availableStars.ToString());

        ViewBag.TotalEarnedStars = totalEarnedStars;
        ViewBag.AvailableStars = availableStars;
        ViewBag.UnlockedAvatars = unlockedList;
        ViewBag.EquippedAvatar = user.Image;

        return View(user);
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> UnlockAvatar(string avatarId)
    {
        if (string.IsNullOrEmpty(avatarId)) return Json(new { success = false, message = "Hình nền không hợp lệ." });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        int cost = avatarId switch
        {
            "VIP_1" => 400,
            "VIP_2" => 800,
            "VIP_3" => 1200,
            "VIP_4" => 1600,
            "VIP_5" => 2000,
            _ => -1
        };

        if (cost == -1) return Json(new { success = false, message = "Hình nền không tồn tại." });

        var unlockedList = new List<string>();
        if (!string.IsNullOrEmpty(user.Specialization))
        {
            unlockedList = user.Specialization.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        if (unlockedList.Contains(avatarId))
        {
            return Json(new { success = false, message = "Bạn đã mở khóa hình nền này rồi." });
        }

        // Calculate current stars
        var totalCorrect = await _db.TestAttempts
            .Where(ta => ta.StudentId == userId && ta.Score.HasValue)
            .SumAsync(ta => ta.CorrectAnswers ?? 0);
        int totalEarnedStars = totalCorrect * 10;

        int spentStars = 0;
        foreach (var avId in unlockedList)
        {
            spentStars += avId switch
            {
                "VIP_1" => 400,
                "VIP_2" => 800,
                "VIP_3" => 1200,
                "VIP_4" => 1600,
                "VIP_5" => 2000,
                _ => 0
            };
        }

        int availableStars = totalEarnedStars - spentStars;
        if (availableStars < cost)
        {
            return Json(new { success = false, message = $"Bạn không đủ sao! Cần thêm {cost - availableStars} sao để mở khóa." });
        }

        unlockedList.Add(avatarId);
        user.Specialization = string.Join(",", unlockedList);
        await _userService.UpdateUserAsync(user);

        // Update session
        availableStars -= cost;
        HttpContext.Session.SetString("StudentStars", availableStars.ToString());

        return Json(new { success = true, message = "Mở khóa thành công!" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> EquipAvatar(string avatarId)
    {
        if (string.IsNullOrEmpty(avatarId)) return Json(new { success = false, message = "Hình nền không hợp lệ." });

        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        var unlockedList = new List<string>();
        if (!string.IsNullOrEmpty(user.Specialization))
        {
            unlockedList = user.Specialization.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        if (!unlockedList.Contains(avatarId) && avatarId.StartsWith("VIP_"))
        {
            return Json(new { success = false, message = "Bạn chưa mở khóa hình nền này." });
        }

        user.Image = avatarId;
        await _userService.UpdateUserAsync(user);
        await SignInUserAsync(user);

        return Json(new { success = true, message = "Sử dụng hình nền thành công!" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> UnequipAvatar()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null) return NotFound();

        user.Image = null;
        await _userService.UpdateUserAsync(user);
        await SignInUserAsync(user);

        return Json(new { success = true, message = "Đã gỡ bỏ hình nền VIP, chuyển về mặc định!" });
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
