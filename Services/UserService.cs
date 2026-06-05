using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public async Task<User?> CheckLoginAsync(string username, string password)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.Status == "active");

        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.Password)) return null;
        return user;
    }

    public async Task<User?> GetByIdAsync(int userId)
        => await _db.Users.FindAsync(userId);

    public async Task<User?> GetByUsernameAsync(string username)
        => await _db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User?> GetByEmailAsync(string email)
        => await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> UsernameExistsAsync(string username)
        => await _db.Users.AnyAsync(u => u.Username == username);

    public async Task<bool> EmailExistsAsync(string email)
        => await _db.Users.AnyAsync(u => u.Email == email);

    public async Task InsertUserAsync(User user)
    {
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user != null)
        {
            user.Deleted = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task<bool> UpdatePasswordAsync(string email, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;
        user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<User>> GetAllAsync()
        => await _db.Users.Where(u => !u.Deleted).ToListAsync();

    public async Task<List<User>> SearchUsersAsync(string? search, string? role, string? status, int page, int pageSize)
    {
        var query = _db.Users.Where(u => !u.Deleted).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(u => u.Status == status);
        return await query.OrderBy(u => u.UserId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<int> CountUsersAsync(string? search, string? role, string? status)
    {
        var query = _db.Users.Where(u => !u.Deleted).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.Username.Contains(search) || u.Email.Contains(search));
        if (!string.IsNullOrEmpty(role))
            query = query.Where(u => u.Role == role);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(u => u.Status == status);
        return await query.CountAsync();
    }

    public async Task<List<User>> GetLinkedStudentsAsync(int parentId)
        => await _db.Users
            .Where(u => u.ParentId == parentId && u.Role == "Student" && u.Status == "active" && !u.Deleted)
            .ToListAsync();

    public async Task<List<User>> GetStudentsAsync()
        => await _db.Users.Where(u => u.Role == "Student" && !u.Deleted).ToListAsync();

    public async Task<List<int>> GetMentorIdsAsync()
        => await _db.Users
            .Where(u => u.Role == "Content Manager" && u.Status == "Active")
            .Select(u => u.UserId).ToListAsync();
}
