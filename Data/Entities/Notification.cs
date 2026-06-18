using System;

namespace POT_System_ASPNET.Data.Entities;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public string? Message { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
