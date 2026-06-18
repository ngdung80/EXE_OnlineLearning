using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Services;

public interface IUserService
{
    Task<User?> CheckLoginAsync(string username, string password);
    Task<User?> GetByIdAsync(int userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task InsertUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(int userId);
    Task<bool> UpdatePasswordAsync(string email, string newPassword);
    Task<List<User>> GetAllAsync();
    Task<List<User>> SearchUsersAsync(string? search, string? role, string? status, int page, int pageSize);
    Task<int> CountUsersAsync(string? search, string? role, string? status);
    Task<List<User>> GetLinkedStudentsAsync(int parentId);
    Task<List<User>> GetStudentsAsync();
    Task<List<int>> GetMentorIdsAsync();
}
