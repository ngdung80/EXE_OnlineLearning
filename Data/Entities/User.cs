using System;
using System.Collections.Generic;

namespace POT_System_ASPNET.Data.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int? GradeId { get; set; }
    public int? ParentId { get; set; }
    public string? FullName { get; set; }
    public DateOnly? Dob { get; set; }
    public string? Phone { get; set; }
    public string? Specialization { get; set; }
    public string? WorkTime { get; set; }
    public string Status { get; set; } = "active";
    public string? Image { get; set; }
    public bool Deleted { get; set; } = false;

    public Grade? Grade { get; set; }
    public User? Parent { get; set; }
    public ICollection<User> Children { get; set; } = new List<User>();
    public ICollection<Wallet> Wallets { get; set; } = new List<Wallet>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
