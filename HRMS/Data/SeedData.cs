using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS.Data;

public static class SeedData
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        context.Database.EnsureCreated();

        var timestamp = DateTime.UtcNow.ToString("o");

        if (!context.Roles.Any())
        {
            var roles = new[]
            {
                new Role { RoleName = "Super Admin" },
                new Role { RoleName = "HOA President" },
                new Role { RoleName = "Board Member" },
                new Role { RoleName = "Staff" },
                new Role { RoleName = "Homeowner" }
            };

            context.Roles.AddRange(roles);
            context.SaveChanges();
        }

        var roleIds = context.Roles
            .AsNoTracking()
            .ToDictionary(role => role.RoleName, role => role.RoleId);

        var demoUsers = new[]
        {
            new User
            {
                Username = "superadmin",
                PasswordHash = HashHelper.Hash("superadmin123"),
                RoleId = roleIds["Super Admin"],
                CreatedAt = timestamp
            },
            new User
            {
                Username = "president",
                PasswordHash = HashHelper.Hash("president123"),
                RoleId = roleIds["HOA President"],
                CreatedAt = timestamp
            },
            new User
            {
                Username = "boardmember",
                PasswordHash = HashHelper.Hash("boardmember123"),
                RoleId = roleIds["Board Member"],
                CreatedAt = timestamp
            },
            new User
            {
                Username = "staff",
                PasswordHash = HashHelper.Hash("staff123"),
                RoleId = roleIds["Staff"],
                CreatedAt = timestamp
            },
            new User
            {
                Username = "homeowner",
                PasswordHash = HashHelper.Hash("homeowner123"),
                RoleId = roleIds["Homeowner"],
                CreatedAt = timestamp
            }
        };

        foreach (var demoUser in demoUsers)
        {
            if (context.Users.Any(user => user.Username == demoUser.Username))
            {
                continue;
            }

            context.Users.Add(demoUser);
        }

        context.SaveChanges();

        var superAdmin = context.Users.First(user => user.Username == "superadmin");
        var homeownerUser = context.Users.First(user => user.Username == "homeowner");

        if (!context.HOASettings.Any())
        {
            context.HOASettings.Add(new HOASettings
            {
                SettingId = 1,
                HOAName = "Homeowners Association",
                Subdivision = "Sample Subdivision",
                City = "Davao City",
                Province = "Davao del Sur",
                UpdatedAt = timestamp,
                UpdatedBy = superAdmin.UserId
            });
        }

        if (homeownerUser.HomeownerId is null || !context.Homeowners.Any(h => h.HomeownerId == homeownerUser.HomeownerId && !h.IsDeleted))
        {
            var demoHomeowner = context.Homeowners.FirstOrDefault(h =>
                !h.IsDeleted &&
                h.Email == "homeowner@hrms.local");

            if (demoHomeowner is null)
            {
                demoHomeowner = new Homeowner
                {
                    FirstName = "Sample",
                    LastName = "Homeowner",
                    BirthDate = DateTime.SpecifyKind(new DateTime(1995, 1, 15), DateTimeKind.Utc).ToString("o"),
                    Gender = "Other",
                    ContactNumber = "09170000000",
                    Email = "homeowner@hrms.local",
                    Status = "Active",
                    ResidencySince = DateTime.SpecifyKind(new DateTime(2020, 1, 1), DateTimeKind.Utc).ToString("o"),
                    CreatedAt = timestamp,
                    CreatedBy = superAdmin.UserId
                };

                context.Homeowners.Add(demoHomeowner);
                context.SaveChanges();
            }

            homeownerUser.HomeownerId = demoHomeowner.HomeownerId;
        }

        context.SaveChanges();
    }
}
