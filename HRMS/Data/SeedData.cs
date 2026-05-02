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
        Seed(context);
    }

    private static void Seed(AppDbContext context)
    {
        EnsureRoles(context);

        var superAdminRole = context.Roles.Single(role => role.RoleName == "Super Admin");
        var presidentRole = context.Roles.Single(role => role.RoleName == "HOA President");
        var boardRole = context.Roles.Single(role => role.RoleName == "Board Member");
        var staffRole = context.Roles.Single(role => role.RoleName == "HOA Staff");
        var homeownerRole = context.Roles.Single(role => role.RoleName == "Homeowner");

        if (!context.Users.Any(user => user.Username == "superadmin"))
        {
            context.Users.Add(new User
            {
                Username = "superadmin",
                PasswordHash = HashHelper.Hash("superadmin123"),
                RoleId = superAdminRole.RoleId,
                SubdivisionId = null,
                HomeownerId = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
        }

        context.SaveChanges();

        var superAdmin = context.Users.Single(user => user.Username == "superadmin");

        if (!context.Subdivisions.Any(subdivision => subdivision.Name == "Golden Fields Residences"))
        {
            context.Subdivisions.Add(new Subdivision
            {
                Name = "Golden Fields Residences",
                Address = "Golden Fields Street",
                City = "Davao City",
                Province = "Davao del Sur",
                Status = "Active",
                SubscriptionStart = ToIsoDateString(new DateTime(2024, 1, 1)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = superAdmin.UserId
            });
        }

        if (!context.Subdivisions.Any(subdivision => subdivision.Name == "Palmera South Residences"))
        {
            context.Subdivisions.Add(new Subdivision
            {
                Name = "Palmera South Residences",
                Address = "Palmera Avenue",
                City = "General Santos City",
                Province = "South Cotabato",
                Status = "Active",
                SubscriptionStart = ToIsoDateString(new DateTime(2024, 6, 1)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = superAdmin.UserId
            });
        }

        context.SaveChanges();

        var goldenFields = context.Subdivisions.Single(subdivision => subdivision.Name == "Golden Fields Residences");
        var palmeraSouth = context.Subdivisions.Single(subdivision => subdivision.Name == "Palmera South Residences");

        EnsureUser(context, "gf.president", "president123", presidentRole.RoleId, goldenFields.SubdivisionId, null);
        EnsureUser(context, "gf.boardmember", "boardmember123", boardRole.RoleId, goldenFields.SubdivisionId, null);
        EnsureUser(context, "gf.staff", "staff123", staffRole.RoleId, goldenFields.SubdivisionId, null);
        EnsureUser(context, "ps.president", "president123", presidentRole.RoleId, palmeraSouth.SubdivisionId, null);
        EnsureUser(context, "ps.boardmember", "boardmember123", boardRole.RoleId, palmeraSouth.SubdivisionId, null);
        EnsureUser(context, "ps.staff", "staff123", staffRole.RoleId, palmeraSouth.SubdivisionId, null);

        context.SaveChanges();

        var gfPresident = context.Users.Single(user => user.Username == "gf.president");
        var gfBoardMember = context.Users.Single(user => user.Username == "gf.boardmember");
        var gfStaff = context.Users.Single(user => user.Username == "gf.staff");
        var psPresident = context.Users.Single(user => user.Username == "ps.president");
        var psBoardMember = context.Users.Single(user => user.Username == "ps.boardmember");
        var psStaff = context.Users.Single(user => user.Username == "ps.staff");

        if (!context.HOASettings.Any(setting => setting.SubdivisionId == goldenFields.SubdivisionId))
        {
            context.HOASettings.Add(new HOASettings
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
                UpdatedBy = gfPresident.UserId
            });
        }

        if (!context.HOASettings.Any(setting => setting.SubdivisionId == palmeraSouth.SubdivisionId))
        {
            context.HOASettings.Add(new HOASettings
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
                UpdatedBy = psPresident.UserId
            });
        }

        context.SaveChanges();

        if (!context.Phases.Any(phase => phase.SubdivisionId == goldenFields.SubdivisionId && phase.Name == "Phase 1"))
        {
            context.Phases.Add(new Phase
            {
                Name = "Phase 1",
                Description = "Primary residential cluster for Golden Fields demo data.",
                SubdivisionId = goldenFields.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
        }

        if (!context.Phases.Any(phase => phase.SubdivisionId == goldenFields.SubdivisionId && phase.Name == "Phase 2"))
        {
            context.Phases.Add(new Phase
            {
                Name = "Phase 2",
                Description = "Secondary residential cluster for Golden Fields demo data.",
                SubdivisionId = goldenFields.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
        }

        if (!context.Phases.Any(phase => phase.SubdivisionId == palmeraSouth.SubdivisionId && phase.Name == "Phase 1"))
        {
            context.Phases.Add(new Phase
            {
                Name = "Phase 1",
                Description = "Primary residential cluster for Palmera South demo data.",
                SubdivisionId = palmeraSouth.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
        }

        if (!context.Phases.Any(phase => phase.SubdivisionId == palmeraSouth.SubdivisionId && phase.Name == "Phase 2"))
        {
            context.Phases.Add(new Phase
            {
                Name = "Phase 2",
                Description = "Secondary residential cluster for Palmera South demo data.",
                SubdivisionId = palmeraSouth.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o")
            });
        }

        context.SaveChanges();

        var gfPhase1 = context.Phases.Single(phase => phase.SubdivisionId == goldenFields.SubdivisionId && phase.Name == "Phase 1");
        var gfPhase2 = context.Phases.Single(phase => phase.SubdivisionId == goldenFields.SubdivisionId && phase.Name == "Phase 2");
        var psPhase1 = context.Phases.Single(phase => phase.SubdivisionId == palmeraSouth.SubdivisionId && phase.Name == "Phase 1");
        var psPhase2 = context.Phases.Single(phase => phase.SubdivisionId == palmeraSouth.SubdivisionId && phase.Name == "Phase 2");

        if (!context.Units.Any(unit => unit.SubdivisionId == goldenFields.SubdivisionId && unit.UnitNumber == "GF-101"))
        {
            context.Units.Add(new Unit
            {
                UnitNumber = "GF-101",
                Address = "Blk 1 Lot 7, Phase 1",
                PhaseId = gfPhase1.PhaseId,
                SubdivisionId = goldenFields.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfPresident.UserId
            });
        }

        if (!context.Units.Any(unit => unit.SubdivisionId == goldenFields.SubdivisionId && unit.UnitNumber == "GF-202"))
        {
            context.Units.Add(new Unit
            {
                UnitNumber = "GF-202",
                Address = "Blk 2 Lot 3, Phase 2",
                PhaseId = gfPhase2.PhaseId,
                SubdivisionId = goldenFields.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfPresident.UserId
            });
        }

        if (!context.Units.Any(unit => unit.SubdivisionId == palmeraSouth.SubdivisionId && unit.UnitNumber == "PS-001"))
        {
            context.Units.Add(new Unit
            {
                UnitNumber = "PS-001",
                Address = "1 Palmera Avenue, Phase 1",
                PhaseId = psPhase1.PhaseId,
                SubdivisionId = palmeraSouth.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psPresident.UserId
            });
        }

        if (!context.Units.Any(unit => unit.SubdivisionId == palmeraSouth.SubdivisionId && unit.UnitNumber == "PS-002"))
        {
            context.Units.Add(new Unit
            {
                UnitNumber = "PS-002",
                Address = "2 Palmera Avenue, Phase 2",
                PhaseId = psPhase2.PhaseId,
                SubdivisionId = palmeraSouth.SubdivisionId,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psPresident.UserId
            });
        }

        context.SaveChanges();

        var gfUnit1 = context.Units.Single(unit => unit.SubdivisionId == goldenFields.SubdivisionId && unit.UnitNumber == "GF-101");
        var gfUnit2 = context.Units.Single(unit => unit.SubdivisionId == goldenFields.SubdivisionId && unit.UnitNumber == "GF-202");
        var psUnit1 = context.Units.Single(unit => unit.SubdivisionId == palmeraSouth.SubdivisionId && unit.UnitNumber == "PS-001");
        var psUnit2 = context.Units.Single(unit => unit.SubdivisionId == palmeraSouth.SubdivisionId && unit.UnitNumber == "PS-002");

        var gfHomeowner = context.Homeowners
            .SingleOrDefault(homeowner => homeowner.SubdivisionId == goldenFields.SubdivisionId &&
                                          homeowner.FirstName == "Maria" &&
                                          homeowner.LastName == "Santos" &&
                                          !homeowner.IsDeleted);

        if (gfHomeowner is null)
        {
            gfHomeowner = new Homeowner
            {
                FirstName = "Maria",
                MiddleName = "Reyes",
                LastName = "Santos",
                BirthDate = ToIsoDateString(new DateTime(1985, 6, 15)),
                Gender = "Female",
                CivilStatus = "Married",
                ContactNumber = "09171234567",
                Email = "maria.santos@email.com",
                Address = gfUnit1.Address,
                PhaseId = gfPhase1.PhaseId,
                UnitId = gfUnit1.UnitId,
                Status = "Active",
                ResidencySince = ToIsoDateString(new DateTime(2015, 1, 1)),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfPresident.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            };

            context.Homeowners.Add(gfHomeowner);
            context.SaveChanges();
        }

        if (!context.Homeowners.Any(homeowner => homeowner.SubdivisionId == goldenFields.SubdivisionId &&
                                                 homeowner.FirstName == "Roberto" &&
                                                 homeowner.LastName == "Cruz"))
        {
            context.Homeowners.Add(new Homeowner
            {
                FirstName = "Roberto",
                MiddleName = "Garcia",
                LastName = "Cruz",
                BirthDate = ToIsoDateString(new DateTime(1979, 8, 27)),
                Gender = "Male",
                CivilStatus = "Married",
                ContactNumber = "09171230004",
                Email = "roberto.cruz@demo.hrms.local",
                Address = gfUnit2.Address,
                PhaseId = gfPhase2.PhaseId,
                UnitId = gfUnit2.UnitId,
                Status = "Active",
                Categories = "4Ps,Indigent",
                ResidencySince = ToIsoDateString(new DateTime(2016, 3, 12)),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfPresident.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });
        }

        var psHomeowner = context.Homeowners
            .SingleOrDefault(homeowner => homeowner.SubdivisionId == palmeraSouth.SubdivisionId &&
                                          homeowner.FirstName == "Juan" &&
                                          homeowner.LastName == "Dela Cruz" &&
                                          !homeowner.IsDeleted);

        if (psHomeowner is null)
        {
            psHomeowner = new Homeowner
            {
                FirstName = "Juan",
                MiddleName = "Miguel",
                LastName = "Dela Cruz",
                BirthDate = ToIsoDateString(new DateTime(1979, 3, 22)),
                Gender = "Male",
                CivilStatus = "Married",
                ContactNumber = "09281234567",
                Email = "juan.delacruz@email.com",
                Address = psUnit1.Address,
                PhaseId = psPhase1.PhaseId,
                UnitId = psUnit1.UnitId,
                Status = "Active",
                ResidencySince = ToIsoDateString(new DateTime(2018, 6, 1)),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psPresident.UserId,
                SubdivisionId = palmeraSouth.SubdivisionId
            };

            context.Homeowners.Add(psHomeowner);
            context.SaveChanges();
        }

        if (!context.Homeowners.Any(homeowner => homeowner.SubdivisionId == palmeraSouth.SubdivisionId &&
                                                 homeowner.FirstName == "Ana" &&
                                                 homeowner.LastName == "Mercado"))
        {
            context.Homeowners.Add(new Homeowner
            {
                FirstName = "Ana",
                MiddleName = "Lopez",
                LastName = "Mercado",
                BirthDate = ToIsoDateString(new DateTime(1988, 11, 9)),
                Gender = "Female",
                CivilStatus = "Single",
                ContactNumber = "09281230011",
                Email = "ana.mercado@demo.hrms.local",
                Address = psUnit2.Address,
                PhaseId = psPhase2.PhaseId,
                UnitId = psUnit2.UnitId,
                Status = "Active",
                Categories = "Solo Parent",
                ResidencySince = ToIsoDateString(new DateTime(2020, 2, 1)),
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psPresident.UserId,
                SubdivisionId = palmeraSouth.SubdivisionId
            });
        }

        context.SaveChanges();

        var gfAdditionalHomeowner = context.Homeowners.Single(homeowner => homeowner.SubdivisionId == goldenFields.SubdivisionId &&
                                                                           homeowner.FirstName == "Roberto" &&
                                                                           homeowner.LastName == "Cruz");
        var psAdditionalHomeowner = context.Homeowners.Single(homeowner => homeowner.SubdivisionId == palmeraSouth.SubdivisionId &&
                                                                           homeowner.FirstName == "Ana" &&
                                                                           homeowner.LastName == "Mercado");

        if (gfUnit1.HeadHomeownerId != gfHomeowner.HomeownerId)
        {
            gfUnit1.HeadHomeownerId = gfHomeowner.HomeownerId;
        }

        if (gfUnit2.HeadHomeownerId != gfAdditionalHomeowner.HomeownerId)
        {
            gfUnit2.HeadHomeownerId = gfAdditionalHomeowner.HomeownerId;
        }

        if (psUnit1.HeadHomeownerId != psHomeowner.HomeownerId)
        {
            psUnit1.HeadHomeownerId = psHomeowner.HomeownerId;
        }

        if (psUnit2.HeadHomeownerId != psAdditionalHomeowner.HomeownerId)
        {
            psUnit2.HeadHomeownerId = psAdditionalHomeowner.HomeownerId;
        }

        if (gfHomeowner.UnitId != gfUnit1.UnitId)
        {
            gfHomeowner.UnitId = gfUnit1.UnitId;
        }

        if (psHomeowner.UnitId != psUnit1.UnitId)
        {
            psHomeowner.UnitId = psUnit1.UnitId;
        }

        context.SaveChanges();

        EnsureUser(context, "gf.homeowner", "homeowner123", homeownerRole.RoleId, goldenFields.SubdivisionId, gfHomeowner.HomeownerId);
        EnsureUser(context, "ps.homeowner", "homeowner123", homeownerRole.RoleId, palmeraSouth.SubdivisionId, psHomeowner.HomeownerId);

        context.SaveChanges();

        var gfUserToRepair = context.Users
            .SingleOrDefault(user => user.Username == "gf.homeowner");

        if (gfUserToRepair is not null &&
            (gfUserToRepair.HomeownerId != gfHomeowner.HomeownerId ||
             gfUserToRepair.SubdivisionId != goldenFields.SubdivisionId))
        {
            gfUserToRepair.HomeownerId = gfHomeowner.HomeownerId;
            gfUserToRepair.SubdivisionId = goldenFields.SubdivisionId;
        }

        var psUserToRepair = context.Users
            .SingleOrDefault(user => user.Username == "ps.homeowner");

        if (psUserToRepair is not null &&
            (psUserToRepair.HomeownerId != psHomeowner.HomeownerId ||
             psUserToRepair.SubdivisionId != palmeraSouth.SubdivisionId))
        {
            psUserToRepair.HomeownerId = psHomeowner.HomeownerId;
            psUserToRepair.SubdivisionId = palmeraSouth.SubdivisionId;
        }

        context.SaveChanges();

        if (!context.Events.Any(record => record.SubdivisionId == goldenFields.SubdivisionId && record.Title == "Golden Fields HOA Assembly"))
        {
            context.Events.Add(new Event
            {
                Title = "Golden Fields HOA Assembly",
                Description = "Quarterly updates, policy reminders, and homeowner open forum.",
                EventDate = ToIsoDateString(new DateTime(2026, 3, 15)),
                Venue = "Golden Fields Clubhouse",
                EventType = "Assembly",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfStaff.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });
        }

        if (!context.Events.Any(record => record.SubdivisionId == palmeraSouth.SubdivisionId && record.Title == "PS General Assembly 2026"))
        {
            context.Events.Add(new Event
            {
                Title = "PS General Assembly 2026",
                Description = "Annual general assembly for all Palmera South residents.",
                EventDate = DateTime.SpecifyKind(new DateTime(2026, 3, 15, 9, 0, 0), DateTimeKind.Utc).ToString("o"),
                Venue = "Palmera South Clubhouse",
                EventType = "Assembly",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psStaff.UserId,
                SubdivisionId = palmeraSouth.SubdivisionId
            });
        }

        context.SaveChanges();

        var gfEvent = context.Events.Single(record => record.SubdivisionId == goldenFields.SubdivisionId && record.Title == "Golden Fields HOA Assembly");
        var psEvent = context.Events.Single(record => record.SubdivisionId == palmeraSouth.SubdivisionId && record.Title == "PS General Assembly 2026");

        if (!context.Attendances.Any(record => record.EventId == gfEvent.EventId && record.HomeownerId == gfHomeowner.HomeownerId))
        {
            context.Attendances.Add(new Attendance
            {
                EventId = gfEvent.EventId,
                HomeownerId = gfHomeowner.HomeownerId,
                Status = "Present",
                RecordedAt = DateTime.UtcNow.ToString("o"),
                RecordedBy = gfStaff.UserId
            });
        }

        if (!context.Attendances.Any(record => record.EventId == psEvent.EventId && record.HomeownerId == psHomeowner.HomeownerId))
        {
            context.Attendances.Add(new Attendance
            {
                EventId = psEvent.EventId,
                HomeownerId = psHomeowner.HomeownerId,
                Status = "Present",
                RecordedAt = DateTime.UtcNow.ToString("o"),
                RecordedBy = psStaff.UserId
            });
        }

        if (!context.MSMEs.Any(record => record.SubdivisionId == goldenFields.SubdivisionId && record.BusinessName == "Santos Corner Store"))
        {
            context.MSMEs.Add(new MSME
            {
                BusinessName = "Santos Corner Store",
                BusinessType = "Sari-sari Store",
                HomeownerId = gfHomeowner.HomeownerId,
                UnitId = gfUnit1.UnitId,
                ContactNumber = "09171231001",
                Description = "Neighborhood essentials and prepaid load services.",
                RegistrationDate = ToIsoDateString(new DateTime(2025, 7, 1)),
                ExpiryDate = ToIsoDateString(new DateTime(2026, 7, 1)),
                Status = "Active",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfBoardMember.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });
        }

        if (!context.MSMEs.Any(record => record.SubdivisionId == palmeraSouth.SubdivisionId && record.BusinessName == "Dela Cruz Refreshments"))
        {
            context.MSMEs.Add(new MSME
            {
                BusinessName = "Dela Cruz Refreshments",
                BusinessType = "Food Stall",
                HomeownerId = psHomeowner.HomeownerId,
                UnitId = psUnit1.UnitId,
                ContactNumber = "09281231001",
                Description = "Light snacks and beverages for Palmera South residents.",
                RegistrationDate = ToIsoDateString(new DateTime(2025, 9, 1)),
                ExpiryDate = ToIsoDateString(new DateTime(2026, 9, 1)),
                Status = "Active",
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psBoardMember.UserId,
                SubdivisionId = palmeraSouth.SubdivisionId
            });
        }

        context.SaveChanges();

        var gfMsme = context.MSMEs.Single(record => record.SubdivisionId == goldenFields.SubdivisionId && record.BusinessName == "Santos Corner Store");
        var psMsme = context.MSMEs.Single(record => record.SubdivisionId == palmeraSouth.SubdivisionId && record.BusinessName == "Dela Cruz Refreshments");

        if (!context.InteractionLogs.Any(record => record.SubdivisionId == goldenFields.SubdivisionId &&
                                                   record.HomeownerId == gfHomeowner.HomeownerId &&
                                                   record.InteractionType == "Visit"))
        {
            context.InteractionLogs.Add(new InteractionLog
            {
                HomeownerId = gfHomeowner.HomeownerId,
                InteractionType = "Visit",
                Notes = "Introduced the Golden Fields homeowner portal.",
                InteractionDate = ToIsoDateString(new DateTime(2026, 4, 10)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfBoardMember.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });
        }

        if (!context.InteractionLogs.Any(record => record.SubdivisionId == palmeraSouth.SubdivisionId &&
                                                   record.HomeownerId == psHomeowner.HomeownerId &&
                                                   record.InteractionType == "Follow-up"))
        {
            context.InteractionLogs.Add(new InteractionLog
            {
                HomeownerId = psHomeowner.HomeownerId,
                InteractionType = "Follow-up",
                Notes = "Reviewed Palmera South dues reminders and community updates.",
                InteractionDate = ToIsoDateString(new DateTime(2026, 4, 12)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psBoardMember.UserId,
                SubdivisionId = palmeraSouth.SubdivisionId
            });
        }

        if (!context.InteractionLogs.Any(record => record.SubdivisionId == goldenFields.SubdivisionId &&
                                                   record.MSMEId == gfMsme.MSMEId &&
                                                   record.InteractionType == "Compliance Check"))
        {
            context.InteractionLogs.Add(new InteractionLog
            {
                MSMEId = gfMsme.MSMEId,
                InteractionType = "Compliance Check",
                Notes = "Checked business permit display and operating hours.",
                InteractionDate = ToIsoDateString(new DateTime(2026, 3, 6)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = gfBoardMember.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });
        }

        if (!context.InteractionLogs.Any(record => record.SubdivisionId == palmeraSouth.SubdivisionId &&
                                                   record.MSMEId == psMsme.MSMEId &&
                                                   record.InteractionType == "Site Visit"))
        {
            context.InteractionLogs.Add(new InteractionLog
            {
                MSMEId = psMsme.MSMEId,
                InteractionType = "Site Visit",
                Notes = "Confirmed food stall signage and sanitation reminders.",
                InteractionDate = ToIsoDateString(new DateTime(2026, 3, 8)),
                CreatedAt = DateTime.UtcNow.ToString("o"),
                CreatedBy = psBoardMember.UserId,
                SubdivisionId = palmeraSouth.SubdivisionId
            });
        }

        EnsureDuesRecord(context, gfHomeowner.HomeownerId, goldenFields.SubdivisionId, gfStaff.UserId, 3, 2026, 1200m, new DateTime(2026, 3, 10), "Paid", new DateTime(2026, 3, 8));
        EnsureDuesRecord(context, gfHomeowner.HomeownerId, goldenFields.SubdivisionId, gfStaff.UserId, 4, 2026, 1200m, new DateTime(2026, 4, 10), "Unpaid", null);
        EnsureDuesRecord(context, psHomeowner.HomeownerId, palmeraSouth.SubdivisionId, psStaff.UserId, 1, 2026, 500.00m, new DateTime(2026, 1, 31), "Unpaid", null);
        EnsureDuesRecord(context, psHomeowner.HomeownerId, palmeraSouth.SubdivisionId, psStaff.UserId, 2, 2026, 500.00m, new DateTime(2026, 2, 28), "Paid", new DateTime(2026, 2, 20));

        if (!context.ViolationRecords.Any(record => record.ViolationNumber == "GF-VIO-2026-0001"))
        {
            context.ViolationRecords.Add(new ViolationRecord
            {
                ViolationNumber = "GF-VIO-2026-0001",
                HomeownerId = gfHomeowner.HomeownerId,
                HomeownerName = "Maria Reyes Santos",
                ViolationType = "Noise Complaint",
                ViolationDate = ToIsoDateString(new DateTime(2026, 4, 2)),
                Details = "Reported amplified karaoke beyond permitted evening hours.",
                Status = "Open",
                FiledAt = DateTime.UtcNow.ToString("o"),
                FiledBy = gfBoardMember.UserId,
                SubdivisionId = goldenFields.SubdivisionId
            });
        }

        if (!context.ViolationRecords.Any(record => record.ViolationNumber == "VIO-2026-0001"))
        {
            context.ViolationRecords.Add(new ViolationRecord
            {
                ViolationNumber = "VIO-2026-0001",
                HomeownerId = psHomeowner.HomeownerId,
                HomeownerName = "Juan Miguel Dela Cruz",
                ViolationType = "Noise Complaint",
                ViolationDate = ToIsoDateString(new DateTime(2026, 2, 10)),
                Details = "Loud music past 10 PM reported by neighboring unit.",
                Status = "Resolved",
                Resolution = "Homeowner notified and acknowledged the complaint.",
                FiledAt = DateTime.UtcNow.ToString("o"),
                FiledBy = psBoardMember.UserId,
                SubdivisionId = palmeraSouth.SubdivisionId
            });
        }

        if (!context.ClearanceRequests.Any(record => record.SubdivisionId == goldenFields.SubdivisionId &&
                                                     record.HomeownerId == gfHomeowner.HomeownerId &&
                                                     record.ClearanceType == "HOA Clearance"))
        {
            context.ClearanceRequests.Add(new ClearanceRequest
            {
                HomeownerId = gfHomeowner.HomeownerId,
                ClearanceType = "HOA Clearance",
                Purpose = "Bank loan documentary requirement",
                Status = "Pending",
                RequestedAt = ToIsoDateString(new DateTime(2026, 4, 10)),
                SubdivisionId = goldenFields.SubdivisionId
            });
        }

        if (!context.ClearanceRequests.Any(record => record.SubdivisionId == palmeraSouth.SubdivisionId &&
                                                     record.HomeownerId == psHomeowner.HomeownerId &&
                                                     record.ClearanceType == "Certificate of Residency"))
        {
            context.ClearanceRequests.Add(new ClearanceRequest
            {
                HomeownerId = psHomeowner.HomeownerId,
                ClearanceType = "Certificate of Residency",
                Purpose = "Employment",
                Status = "Pending",
                RequestedAt = DateTime.UtcNow.ToString("o"),
                SubdivisionId = palmeraSouth.SubdivisionId
            });
        }

        if (!context.AuditLogs.Any(record => record.TableAffected == "Subdivisions" &&
                                             record.RecordId == goldenFields.SubdivisionId &&
                                             record.Details == "[Seed] Added subdivision Golden Fields Residences."))
        {
            context.AuditLogs.Add(new AuditLog
            {
                UserId = superAdmin.UserId,
                Action = "Create",
                TableAffected = "Subdivisions",
                RecordId = goldenFields.SubdivisionId,
                Details = "[Seed] Added subdivision Golden Fields Residences.",
                Timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        if (!context.AuditLogs.Any(record => record.TableAffected == "Subdivisions" &&
                                             record.RecordId == palmeraSouth.SubdivisionId &&
                                             record.Details == "[Seed] Added subdivision Palmera South Residences."))
        {
            context.AuditLogs.Add(new AuditLog
            {
                UserId = superAdmin.UserId,
                Action = "Create",
                TableAffected = "Subdivisions",
                RecordId = palmeraSouth.SubdivisionId,
                Details = "[Seed] Added subdivision Palmera South Residences.",
                Timestamp = DateTime.UtcNow.ToString("o")
            });
        }

        context.SaveChanges();
    }

    private static void EnsureRoles(AppDbContext context)
    {
        var roleNames = new[]
        {
            "Super Admin",
            "HOA President",
            "Board Member",
            "HOA Staff",
            "Homeowner"
        };

        foreach (var roleName in roleNames)
        {
            if (!context.Roles.Any(role => role.RoleName == roleName))
            {
                context.Roles.Add(new Role { RoleName = roleName });
            }
        }

        context.SaveChanges();
    }

    private static void EnsureUser(AppDbContext context, string username, string password, int roleId, int? subdivisionId, int? homeownerId)
    {
        if (context.Users.Any(user => user.Username == username))
        {
            return;
        }

        context.Users.Add(new User
        {
            Username = username,
            PasswordHash = HashHelper.Hash(password),
            RoleId = roleId,
            SubdivisionId = subdivisionId,
            HomeownerId = homeownerId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("o")
        });
    }

    private static void EnsureDuesRecord(
        AppDbContext context,
        int homeownerId,
        int subdivisionId,
        int createdBy,
        int month,
        int year,
        decimal amount,
        DateTime dueDate,
        string status,
        DateTime? paidDate)
    {
        if (context.DuesRecords.Any(record => record.HomeownerId == homeownerId && record.Month == month && record.Year == year))
        {
            return;
        }

        context.DuesRecords.Add(new DuesRecord
        {
            HomeownerId = homeownerId,
            Month = month,
            Year = year,
            Amount = amount,
            DueDate = ToIsoDateString(dueDate),
            Status = status,
            PaidDate = paidDate.HasValue ? ToIsoDateString(paidDate.Value) : null,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = createdBy,
            SubdivisionId = subdivisionId
        });
    }

    private static string ToIsoDateString(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc).ToString("o");
}
