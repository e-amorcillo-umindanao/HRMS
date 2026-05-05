using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;
using PhaseModel = global::HRMS.Models.Phase;

namespace HRMS.Services;

public class SettingsService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public SettingsService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<HOASettings> SaveHoaSettingsAsync(HOASettings input, int subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        if (subdivisionId <= 0)
        {
            throw new InvalidOperationException("Select a subdivision first.");
        }

        if (string.IsNullOrWhiteSpace(input.HOAName) ||
            string.IsNullOrWhiteSpace(input.Subdivision) ||
            string.IsNullOrWhiteSpace(input.City) ||
            string.IsNullOrWhiteSpace(input.Province))
        {
            throw new InvalidOperationException("HOA name and address fields are required.");
        }

        var settings = await _context.HOASettings
            .FirstOrDefaultAsync(item => item.SubdivisionId == subdivisionId);

        if (settings is null)
        {
            settings = new HOASettings { SubdivisionId = subdivisionId };
            _context.HOASettings.Add(settings);
        }

        settings.SubdivisionId = subdivisionId;
        settings.HOAName = input.HOAName;
        settings.Subdivision = input.Subdivision;
        settings.City = input.City;
        settings.Province = input.Province;
        settings.PresidentName = NormalizeOptional(input.PresidentName);
        settings.SecretaryName = NormalizeOptional(input.SecretaryName);
        settings.TreasurerName = NormalizeOptional(input.TreasurerName);
        settings.ContactNumber = NormalizeOptional(input.ContactNumber);
        settings.LogoPath = NormalizeOptional(input.LogoPath);
        settings.UpdatedAt = DateTime.UtcNow.ToString("o");
        settings.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Update", "HOASettings", settings.SettingId, "Updated HOA settings.");

        return settings;
    }

    public async Task<User> AddUserAsync(string username, string password, int roleId, int? homeownerId, int? activeSubdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        if (roleId == 0 || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Username, password, and role are required.");
        }

        var normalizedUsername = username.Trim();

        if (await _context.Users.AnyAsync(user => user.Username == normalizedUsername))
        {
            throw new InvalidOperationException("That username is already in use.");
        }

        if (homeownerId.HasValue && await _context.Users.AnyAsync(user => user.HomeownerId == homeownerId.Value))
        {
            throw new InvalidOperationException("That homeowner is already linked to another user.");
        }

        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoleId == roleId);

        if (role is null)
        {
            throw new InvalidOperationException("The selected role could not be found.");
        }

        var linkedHomeownerSubdivisionId = homeownerId.HasValue
            ? await _context.Homeowners
                .AsNoTracking()
                .Where(homeowner => homeowner.HomeownerId == homeownerId.Value)
                .Select(homeowner => (int?)homeowner.SubdivisionId)
                .FirstOrDefaultAsync()
            : null;

        var resolvedSubdivisionId = role.RoleName == "Super Admin"
            ? null
            : linkedHomeownerSubdivisionId ?? activeSubdivisionId;

        if (role.RoleName != "Super Admin" && !resolvedSubdivisionId.HasValue)
        {
            throw new InvalidOperationException("Select a subdivision first.");
        }

        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = HashHelper.Hash(password),
            RoleId = roleId,
            HomeownerId = homeownerId,
            SubdivisionId = resolvedSubdivisionId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Create", "Users", user.UserId, $"Created user '{user.Username}'.");

        return user;
    }

    public async Task<User> ResetPasswordAsync(int userId, string newPassword, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new InvalidOperationException("Enter a new password first.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (user is null)
        {
            throw new InvalidOperationException("The selected user could not be found.");
        }

        user.PasswordHash = HashHelper.Hash(newPassword);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Update", "Users", user.UserId, $"Reset password for '{user.Username}'.");

        return user;
    }

    public async Task<User> DeactivateUserAsync(int userId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        if (actorUserId == userId)
        {
            throw new InvalidOperationException("You cannot deactivate your own account.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (user is null)
        {
            throw new InvalidOperationException("The selected user could not be found.");
        }

        user.IsActive = false;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Update", "Users", user.UserId, $"Deactivated user '{user.Username}'.");

        return user;
    }

    public async Task<User> UpdateUserAsync(int userId, int roleId, int? homeownerId, int? activeSubdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (user is null)
        {
            throw new InvalidOperationException("The selected user could not be found.");
        }

        var role = await _context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.RoleId == roleId);

        if (role is null)
        {
            throw new InvalidOperationException("The selected role could not be found.");
        }

        if (homeownerId.HasValue && await _context.Users.AnyAsync(item => item.UserId != userId && item.HomeownerId == homeownerId.Value))
        {
            throw new InvalidOperationException("That homeowner is already linked to another user.");
        }

        var linkedHomeownerSubdivisionId = homeownerId.HasValue
            ? await _context.Homeowners
                .AsNoTracking()
                .Where(homeowner => homeowner.HomeownerId == homeownerId.Value && !homeowner.IsDeleted)
                .Select(homeowner => (int?)homeowner.SubdivisionId)
                .FirstOrDefaultAsync()
            : null;

        if (homeownerId.HasValue && !linkedHomeownerSubdivisionId.HasValue)
        {
            throw new InvalidOperationException("The selected homeowner could not be found.");
        }

        var resolvedSubdivisionId = role.RoleName == "Super Admin"
            ? null
            : linkedHomeownerSubdivisionId ?? activeSubdivisionId ?? user.SubdivisionId;

        if (role.RoleName != "Super Admin" && !resolvedSubdivisionId.HasValue)
        {
            throw new InvalidOperationException("Select a subdivision first.");
        }

        user.RoleId = roleId;
        user.HomeownerId = homeownerId;
        user.SubdivisionId = resolvedSubdivisionId;

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Update", "Users", user.UserId, $"Updated user '{user.Username}' role and assignment.");

        return user;
    }

    public async Task<User> ReactivateUserAsync(int userId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.UserId == userId);

        if (user is null)
        {
            throw new InvalidOperationException("The selected user could not be found.");
        }

        user.IsActive = true;
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Update", "Users", user.UserId, $"Reactivated user '{user.Username}'.");

        return user;
    }

    public async Task<PhaseModel> AddPhaseAsync(string name, string? description, int subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        if (subdivisionId <= 0)
        {
            throw new InvalidOperationException("Select a subdivision first.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Phase name is required.");
        }

        var phase = new PhaseModel
        {
            Name = name.Trim(),
            Description = NormalizeOptional(description),
            CreatedAt = DateTime.UtcNow.ToString("o"),
            SubdivisionId = subdivisionId
        };

        _context.Phases.Add(phase);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Create", "Phases", phase.PhaseId, $"Created phase '{phase.Name}'.");

        return phase;
    }

    public async Task<PhaseModel?> UpdatePhaseAsync(int phaseId, string name, string? description, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Phase name is required.");
        }

        var phase = await _context.Phases
            .FirstOrDefaultAsync(item => item.PhaseId == phaseId);

        if (phase is null)
        {
            throw new InvalidOperationException("The selected phase could not be found.");
        }

        phase.Name = name.Trim();
        phase.Description = NormalizeOptional(description);

        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Update", "Phases", phase.PhaseId, $"Updated phase '{phase.Name}'.");

        return phase;
    }

    public async Task<bool> DeletePhaseAsync(int phaseId, int subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "settings", "Only Super Admin can manage settings.");

        if (subdivisionId <= 0)
        {
            throw new InvalidOperationException("Select a subdivision first.");
        }

        var assignedHomeowners = await _context.Homeowners.CountAsync(homeowner =>
            !homeowner.IsDeleted &&
            homeowner.PhaseId == phaseId &&
            homeowner.SubdivisionId == subdivisionId);

        var assignedUnits = await _context.Units.CountAsync(unit =>
            unit.PhaseId == phaseId &&
            unit.SubdivisionId == subdivisionId);

        if (assignedHomeowners > 0 || assignedUnits > 0)
        {
            throw new InvalidOperationException("This phase cannot be deleted while homeowners or units are still assigned to it.");
        }

        var phase = await _context.Phases
            .FirstOrDefaultAsync(item => item.PhaseId == phaseId && item.SubdivisionId == subdivisionId);

        if (phase is null)
        {
            throw new InvalidOperationException("The selected phase could not be found.");
        }

        _context.Phases.Remove(phase);
        await _context.SaveChangesAsync();
        await _auditService.LogAsync(actorUserId, "Delete", "Phases", phase.PhaseId, $"Deleted phase '{phase.Name}'.");

        return true;
    }

    private async Task<string?> GetActorRoleAsync(int actorUserId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.Role.RoleName)
            .SingleOrDefaultAsync();
    }

    private async Task EnsureCanWriteAsync(int actorUserId, string module, string message)
    {
        var role = await GetActorRoleAsync(actorUserId);
        if (!AccessHelper.CanWrite(role ?? string.Empty, module))
        {
            throw new UnauthorizedAccessException(message);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
