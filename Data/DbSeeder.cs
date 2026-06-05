using Microsoft.EntityFrameworkCore;
using POT_System_ASPNET.Data.Entities;

namespace POT_System_ASPNET.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // Seed Admin account
        var adminUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (adminUser == null)
        {
            db.Users.Add(new User
            {
                Username = "admin",
                Password = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Email = "admin@cyborg.edu.vn",
                FullName = "System Administrator",
                Role = "Admin",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1990, 1, 1)
            });
            await db.SaveChangesAsync();
        }
        else
        {
            adminUser.Password = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            adminUser.Status = "active";
            adminUser.Deleted = false;
            await db.SaveChangesAsync();
        }

        // Seed Parent account
        var parentUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "parent");
        if (parentUser == null)
        {
            parentUser = new User
            {
                Username = "parent",
                Password = BCrypt.Net.BCrypt.HashPassword("Parent@123"),
                Email = "parent@cyborg.edu.vn",
                FullName = "Sample Parent",
                Role = "Parent",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1985, 5, 5)
            };
            db.Users.Add(parentUser);
            await db.SaveChangesAsync();
        }
        else
        {
            parentUser.Password = BCrypt.Net.BCrypt.HashPassword("Parent@123");
            parentUser.Status = "active";
            parentUser.Deleted = false;
            await db.SaveChangesAsync();
        }

        // Seed Student account
        var studentUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "student");
        if (studentUser == null)
        {
            db.Users.Add(new User
            {
                Username = "student",
                Password = BCrypt.Net.BCrypt.HashPassword("Student@123"),
                Email = "student@cyborg.edu.vn",
                FullName = "Sample Student",
                Role = "Student",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(2010, 10, 10),
                ParentId = parentUser.UserId
            });
        }
        else
        {
            studentUser.Password = BCrypt.Net.BCrypt.HashPassword("Student@123");
            studentUser.Status = "active";
            studentUser.Deleted = false;
            studentUser.ParentId = parentUser.UserId;
        }

        // Seed Content Manager account
        var managerUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "manager");
        if (managerUser == null)
        {
            db.Users.Add(new User
            {
                Username = "manager",
                Password = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                Email = "manager@cyborg.edu.vn",
                FullName = "Content Manager",
                Role = "Content Manager",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1992, 2, 2)
            });
        }
        else
        {
            managerUser.Password = BCrypt.Net.BCrypt.HashPassword("Manager@123");
            managerUser.Status = "active";
            managerUser.Deleted = false;
        }

        // Seed Content Approver account
        var approverUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "approver");
        if (approverUser == null)
        {
            db.Users.Add(new User
            {
                Username = "approver",
                Password = BCrypt.Net.BCrypt.HashPassword("Approver@123"),
                Email = "approver@cyborg.edu.vn",
                FullName = "Content Approver",
                Role = "Content Approver",
                Status = "active",
                Deleted = false,
                Dob = new DateOnly(1993, 3, 3)
            });
        }
        else
        {
            approverUser.Password = BCrypt.Net.BCrypt.HashPassword("Approver@123");
            approverUser.Status = "active";
            approverUser.Deleted = false;
        }

        await db.SaveChangesAsync();

        // Seed sample Grades
        if (!await db.Grades.AnyAsync())
        {
            db.Grades.AddRange(
                new Grade { GradeName = "Lớp 10", Description = "Chương trình Toán - Lý - Hóa lớp 10", Status = "Active" },
                new Grade { GradeName = "Lớp 11", Description = "Chương trình Toán - Lý - Hóa lớp 11", Status = "Active" },
                new Grade { GradeName = "Lớp 12", Description = "Ôn thi THPT Quốc gia", Status = "Active" }
            );
        }

        // Seed sample Packages
        if (!await db.Packages.AnyAsync())
        {
            db.Packages.AddRange(
                new Package { PackageName = "Gói Cơ Bản", Description = "Học không giới hạn 1 khối lớp trong 1 tháng", Price = 99000, Duration = 1, Status = "Active" },
                new Package { PackageName = "Gói Tiêu Chuẩn", Description = "Học không giới hạn 1 khối lớp trong 3 tháng", Price = 259000, Duration = 3, Status = "Active" },
                new Package { PackageName = "Gói Cao Cấp", Description = "Học không giới hạn tất cả khối lớp trong 6 tháng", Price = 499000, Duration = 6, Status = "Active" }
            );
        }

        await db.SaveChangesAsync();
    }
}
