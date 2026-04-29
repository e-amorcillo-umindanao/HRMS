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

        context.Database.Migrate();

        if (context.Roles.Any())
        {
            return;
        }

        Seed(context);
    }

    private static void Seed(AppDbContext context)
    {
        var roles = new List<Role>
        {
            new() { RoleName = "Super Admin" },
            new() { RoleName = "HOA President" },
            new() { RoleName = "Board Member" },
            new() { RoleName = "HOA Staff" },
            new() { RoleName = "Homeowner" }
        };

        context.Roles.AddRange(roles);
        context.SaveChanges();

        var superAdminRole = roles.Single(role => role.RoleName == "Super Admin");
        var presidentRole = roles.Single(role => role.RoleName == "HOA President");
        var boardRole = roles.Single(role => role.RoleName == "Board Member");
        var staffRole = roles.Single(role => role.RoleName == "HOA Staff");
        var homeownerRole = roles.Single(role => role.RoleName == "Homeowner");

        var superAdmin = new User
        {
            Username = "superadmin",
            PasswordHash = HashHelper.Hash("superadmin123"),
            RoleId = superAdminRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            SubdivisionId = null
        };

        context.Users.Add(superAdmin);
        context.SaveChanges();

        var goldenFields = new Subdivision
        {
            Name = "Golden Fields Residences",
            City = "Davao City",
            Province = "Davao del Sur",
            Status = "Active",
            SubscriptionStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).ToString("o"),
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = superAdmin.UserId
        };

        var palmeraSouth = new Subdivision
        {
            Name = "Palmera South Residences",
            City = "General Santos City",
            Province = "South Cotabato",
            Status = "Active",
            SubscriptionStart = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc).ToString("o"),
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = superAdmin.UserId
        };

        context.Subdivisions.AddRange(goldenFields, palmeraSouth);
        context.SaveChanges();

        var president = new User
        {
            Username = "president",
            PasswordHash = HashHelper.Hash("president123"),
            RoleId = presidentRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            SubdivisionId = goldenFields.SubdivisionId
        };

        var boardMember = new User
        {
            Username = "boardmember",
            PasswordHash = HashHelper.Hash("boardmember123"),
            RoleId = boardRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            SubdivisionId = goldenFields.SubdivisionId
        };

        var staff = new User
        {
            Username = "staff",
            PasswordHash = HashHelper.Hash("staff123"),
            RoleId = staffRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            SubdivisionId = goldenFields.SubdivisionId
        };

        context.Users.AddRange(president, boardMember, staff);
        context.SaveChanges();

        var phase1 = new Phase
        {
            Name = "Demo Phase 1",
            Description = "North cluster for demos.",
            SubdivisionId = goldenFields.SubdivisionId,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };
        var phase2 = new Phase
        {
            Name = "Demo Phase 2",
            Description = "Central cluster for demos.",
            SubdivisionId = goldenFields.SubdivisionId,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };
        var phase3 = new Phase
        {
            Name = "Demo Phase 3",
            Description = "Garden cluster for demos.",
            SubdivisionId = goldenFields.SubdivisionId,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        context.Phases.AddRange(phase1, phase2, phase3);
        context.SaveChanges();

        var unit1 = new Unit
        {
            UnitNumber = "D1-101",
            Address = "Blk 1 Lot 1, Demo Phase 1",
            PhaseId = phase1.PhaseId,
            SubdivisionId = goldenFields.SubdivisionId,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = president.UserId
        };
        var unit2 = new Unit
        {
            UnitNumber = "D2-201",
            Address = "Blk 2 Lot 1, Demo Phase 2",
            PhaseId = phase2.PhaseId,
            SubdivisionId = goldenFields.SubdivisionId,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = president.UserId
        };
        var unit3 = new Unit
        {
            UnitNumber = "D3-301",
            Address = "Blk 3 Lot 1, Demo Phase 3",
            PhaseId = phase3.PhaseId,
            SubdivisionId = goldenFields.SubdivisionId,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = president.UserId
        };

        context.Units.AddRange(unit1, unit2, unit3);
        context.SaveChanges();

        var sampleHomeowner = new Homeowner
        {
            FirstName = "Sample",
            LastName = "Homeowner",
            BirthDate = ToIsoDateString(new DateTime(2000, 1, 1)),
            Gender = "Male",
            Status = "Active",
            ResidencySince = ToIsoDateString(new DateTime(2020, 1, 1)),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = president.UserId,
            Email = "homeowner@hrms.local",
            ContactNumber = "09170000000",
            Address = unit1.Address,
            PhaseId = phase1.PhaseId,
            UnitId = unit1.UnitId,
            SubdivisionId = goldenFields.SubdivisionId
        };

        var liza = new Homeowner
        {
            FirstName = "Liza",
            MiddleName = "Mendoza",
            LastName = "Romero",
            BirthDate = ToIsoDateString(new DateTime(1960, 5, 14)),
            Gender = "Female",
            CivilStatus = "Widowed",
            ContactNumber = "09171230001",
            Email = "liza.romero@demo.hrms.local",
            Address = unit1.Address,
            PhaseId = phase1.PhaseId,
            UnitId = unit1.UnitId,
            Status = "Active",
            Categories = "Senior",
            ResidencySince = ToIsoDateString(new DateTime(2017, 6, 1)),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = president.UserId,
            SubdivisionId = goldenFields.SubdivisionId
        };

        var roberto = new Homeowner
        {
            FirstName = "Roberto",
            MiddleName = "Garcia",
            LastName = "Cruz",
            BirthDate = ToIsoDateString(new DateTime(1979, 8, 27)),
            Gender = "Male",
            CivilStatus = "Married",
            ContactNumber = "09171230004",
            Email = "roberto.cruz@demo.hrms.local",
            Address = unit2.Address,
            PhaseId = phase2.PhaseId,
            UnitId = unit2.UnitId,
            Status = "Active",
            Categories = "4Ps,Indigent",
            ResidencySince = ToIsoDateString(new DateTime(2016, 3, 12)),
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = president.UserId,
            SubdivisionId = goldenFields.SubdivisionId
        };

        context.Homeowners.AddRange(sampleHomeowner, liza, roberto);
        context.SaveChanges();

        unit1.HeadHomeownerId = sampleHomeowner.HomeownerId;
        unit2.HeadHomeownerId = roberto.HomeownerId;
        unit3.HeadHomeownerId = null;
        context.SaveChanges();

        var homeownerUser = new User
        {
            Username = "homeowner",
            PasswordHash = HashHelper.Hash("homeowner123"),
            RoleId = homeownerRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            SubdivisionId = goldenFields.SubdivisionId,
            HomeownerId = sampleHomeowner.HomeownerId
        };

        context.Users.Add(homeownerUser);
        context.SaveChanges();

        var event1 = new Event
        {
            Title = "Quarterly HOA Assembly - Demo",
            Description = "Quarterly updates, policy reminders, and open forum.",
            EventDate = ToIsoDateString(new DateTime(2026, 3, 15)),
            Venue = "Clubhouse",
            EventType = "Assembly",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = staff.UserId,
            SubdivisionId = goldenFields.SubdivisionId
        };

        var event2 = new Event
        {
            Title = "Neighborhood Wellness Check - Demo",
            Description = "Volunteer-led health screening and wellness consultation.",
            EventDate = ToIsoDateString(new DateTime(2026, 1, 18)),
            Venue = "Multipurpose Hall",
            EventType = "Health",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = staff.UserId,
            SubdivisionId = goldenFields.SubdivisionId
        };

        context.Events.AddRange(event1, event2);
        context.SaveChanges();

        context.Attendances.AddRange(
            new Attendance
            {
                EventId = event1.EventId,
                HomeownerId = sampleHomeowner.HomeownerId,
                Status = "Present",
                RecordedAt = DateTime.UtcNow.ToString("o"),
                RecordedBy = staff.UserId
            },
            new Attendance
            {
                EventId = event2.EventId,
                HomeownerId = sampleHomeowner.HomeownerId,
                Status = "Present",
                RecordedAt = DateTime.UtcNow.ToString("o"),
                RecordedBy = staff.UserId
            });

        var msme = new MSME
        {
            BusinessName = "Romero Corner Store - Demo",
            BusinessType = "Sari-sari Store",
            HomeownerId = liza.HomeownerId,
            UnitId = unit1.UnitId,
            ContactNumber = "09171231001",
            Description = "Neighborhood essentials and prepaid load services.",
            RegistrationDate = ToIsoDateString(new DateTime(2025, 7, 1)),
            ExpiryDate = ToIsoDateString(new DateTime(2026, 7, 1)),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = boardMember.UserId,
            SubdivisionId = goldenFields.SubdivisionId
        };

        context.MSMEs.Add(msme);
        context.SaveChanges();

        context.InteractionLogs.AddRange(
            new InteractionLog
            {
                HomeownerId = sampleHomeowner.HomeownerId,
                InteractionType = "Visit",
                Notes = "Introduced the homeowner portal and explained demo access.",
                InteractionDate = ToIsoDateString(new DateTime(2026, 4, 10)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = boardMember.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            },
            new InteractionLog
            {
                MSMEId = msme.MSMEId,
                InteractionType = "Follow-up",
                Notes = "Reviewed permit display and operating hours.",
                InteractionDate = ToIsoDateString(new DateTime(2026, 3, 6)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = boardMember.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });

        context.DuesRecords.AddRange(
            new DuesRecord
            {
                HomeownerId = sampleHomeowner.HomeownerId,
                Month = 3,
                Year = 2026,
                Amount = 1200m,
                DueDate = ToIsoDateString(new DateTime(2026, 3, 10)),
                Status = "Paid",
                PaidDate = ToIsoDateString(new DateTime(2026, 3, 8)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = staff.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            },
            new DuesRecord
            {
                HomeownerId = sampleHomeowner.HomeownerId,
                Month = 4,
                Year = 2026,
                Amount = 1200m,
                DueDate = ToIsoDateString(new DateTime(2026, 4, 10)),
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = staff.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });

        context.ViolationRecords.Add(
            new ViolationRecord
            {
                ViolationNumber = "VIO-2026-0001",
                HomeownerId = sampleHomeowner.HomeownerId,
                HomeownerName = "Sample Homeowner",
                ViolationType = "Noise Complaint",
                ViolationDate = ToIsoDateString(new DateTime(2026, 4, 2)),
                Details = "Reported amplified karaoke beyond permitted evening hours.",
                Status = "Open",
                FiledAt = DateTime.UtcNow.ToString("o"),
                FiledBy = boardMember.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });

        context.ClearanceRequests.Add(
            new ClearanceRequest
            {
                HomeownerId = sampleHomeowner.HomeownerId,
                ClearanceType = "HOA Clearance",
                Purpose = "Bank loan documentary requirement - demo",
                Status = "Pending",
                RequestedAt = ToIsoDateString(new DateTime(2026, 4, 10)),
                SubdivisionId = goldenFields.SubdivisionId
            });

        context.HOASettings.AddRange(
            new HOASettings
            {
                SubdivisionId = goldenFields.SubdivisionId,
                HOAName = "Golden Fields Homeowners Association",
                Subdivision = "Golden Fields Residences",
                City = "Davao City",
                Province = "Davao del Sur",
                PresidentName = "Patricia Gomez",
                SecretaryName = "Leah Fernandez",
                TreasurerName = "Marco Bautista",
                ContactNumber = "09170004567",
                UpdatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedBy = president.UserId
            },
            new HOASettings
            {
                SubdivisionId = palmeraSouth.SubdivisionId,
                HOAName = "Palmera South Homeowners Association",
                Subdivision = "Palmera South Residences",
                City = "General Santos City",
                Province = "South Cotabato",
                PresidentName = "Alicia Ramos",
                SecretaryName = "Victor Lim",
                TreasurerName = "Marlon Cruz",
                ContactNumber = "09181234567",
                UpdatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedBy = superAdmin.UserId
            });

        context.AuditLogs.AddRange(
            new AuditLog
            {
                UserId = superAdmin.UserId,
                Action = "Create",
                TableAffected = "Subdivisions",
                RecordId = goldenFields.SubdivisionId,
                Details = "[Seed] Added subdivision Golden Fields Residences.",
                Timestamp = DateTime.UtcNow.ToString("o")
            },
            new AuditLog
            {
                UserId = superAdmin.UserId,
                Action = "Create",
                TableAffected = "Subdivisions",
                RecordId = palmeraSouth.SubdivisionId,
                Details = "[Seed] Added subdivision Palmera South Residences.",
                Timestamp = DateTime.UtcNow.ToString("o")
            });

        context.SaveChanges();
    }

    private static string ToIsoDateString(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc).ToString("o");
}
