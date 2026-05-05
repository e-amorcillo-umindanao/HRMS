using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class UnitService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public UnitService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<Unit>> GetAllAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .OrderBy(u => u.UnitNumber)
            .ThenBy(u => u.Address)
            .ToListAsync();
    }

    public async Task<Unit?> GetByIdAsync(int id, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId, includeCreator: true)
            .SingleOrDefaultAsync(u => u.UnitId == id);
    }

    public async Task<List<Unit>> SearchAsync(int? subdivisionId, string? address, int? phaseId)
    {
        var query = BaseQuery(subdivisionId);

        if (!string.IsNullOrWhiteSpace(address))
        {
            query = query.Where(u => u.Address.Contains(address) || u.UnitNumber.Contains(address));
        }

        if (phaseId.HasValue)
        {
            query = query.Where(u => u.PhaseId == phaseId);
        }

        return await query
            .OrderBy(u => u.UnitNumber)
            .ThenBy(u => u.Address)
            .ToListAsync();
    }

    public Task<List<Unit>> SearchAsync(string? address, int? phaseId) =>
        SearchAsync(null, address, phaseId);

    public async Task<Unit> AddAsync(Unit unit, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "units", "You do not have write access to the Units module.");
        unit.SubdivisionId = await ResolveSubdivisionIdAsync(unit.SubdivisionId, unit.PhaseId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(unit.SubdivisionId, actorUserId);
        await EnsureAssignmentsBelongToSubdivisionAsync(unit.PhaseId, unit.HeadHomeownerId, unit.SubdivisionId);
        unit.CreatedAt = DateTime.UtcNow.ToString("o");
        unit.CreatedBy = actorUserId;

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "Units", unit.UnitId, $"Created unit '{unit.UnitNumber}'.");

        return unit;
    }

    public async Task<Unit?> UpdateAsync(Unit unit, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "units", "You do not have write access to the Units module.");
        var existing = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == unit.UnitId);
        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);
        var targetSubdivisionId = unit.SubdivisionId == 0
            ? await ResolveSubdivisionIdAsync(existing.SubdivisionId, unit.PhaseId ?? existing.PhaseId, actorUserId)
            : await ResolveSubdivisionIdAsync(unit.SubdivisionId, unit.PhaseId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(targetSubdivisionId, actorUserId);
        await EnsureAssignmentsBelongToSubdivisionAsync(unit.PhaseId, unit.HeadHomeownerId, targetSubdivisionId);

        existing.UnitNumber = unit.UnitNumber;
        existing.Address = unit.Address;
        existing.SubdivisionId = targetSubdivisionId;
        existing.PhaseId = unit.PhaseId;
        existing.HeadHomeownerId = unit.HeadHomeownerId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Units", existing.UnitId, $"Updated unit '{existing.UnitNumber}'.");

        return existing;
    }

    public async Task<UnitOperationResult> DeleteAsync(int id, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "units", "You do not have write access to the Units module.");
        var unit = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == id);
        if (unit is null)
        {
            return UnitOperationResult.Fail("Unit not found.");
        }

        await EnsureActorCanAccessSubdivisionAsync(unit.SubdivisionId, actorUserId);

        var assignedHomeowners = await _context.Homeowners
            .AnyAsync(h => h.UnitId == id && !h.IsDeleted);

        if (assignedHomeowners)
        {
            return UnitOperationResult.Fail("This unit still has assigned homeowners.");
        }

        _context.Units.Remove(unit);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "Units", unit.UnitId, $"Deleted unit '{unit.UnitNumber}'.");

        return UnitOperationResult.Successful();
    }

    public async Task<List<Homeowner>> GetOccupantsAsync(int unitId, int? subdivisionId = null)
    {
        var query = _context.Homeowners
            .AsNoTracking()
            .Where(h => h.UnitId == unitId && !h.IsDeleted);

        if (subdivisionId.HasValue)
        {
            query = query.Where(h => h.SubdivisionId == subdivisionId.Value);
        }

        return await query
            .OrderBy(h => h.LastName)
            .ThenBy(h => h.FirstName)
            .ToListAsync();
    }

    public async Task<UnitOperationResult> AssignHomeownerAsync(int unitId, int homeownerId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "units", "You do not have write access to the Units module.");
        var unit = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == unitId);
        if (unit is null)
        {
            return UnitOperationResult.Fail("Unit not found.");
        }

        await EnsureActorCanAccessSubdivisionAsync(unit.SubdivisionId, actorUserId);

        var homeowner = await _context.Homeowners.SingleOrDefaultAsync(h => h.HomeownerId == homeownerId && !h.IsDeleted);
        if (homeowner is null)
        {
            return UnitOperationResult.Fail("Homeowner not found.");
        }

        if (homeowner.SubdivisionId != unit.SubdivisionId)
        {
            return UnitOperationResult.Fail("The selected homeowner belongs to a different subdivision.");
        }

        homeowner.UnitId = unitId;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Units", unitId, $"Assigned homeowner '{GetFullName(homeowner)}' to unit '{unit.UnitNumber}'.");

        return UnitOperationResult.Successful();
    }

    public async Task<UnitOperationResult> SetHeadHomeownerAsync(int unitId, int headHomeownerId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "units", "You do not have write access to the Units module.");
        var unit = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == unitId);
        if (unit is null)
        {
            return UnitOperationResult.Fail("Unit not found.");
        }

        await EnsureActorCanAccessSubdivisionAsync(unit.SubdivisionId, actorUserId);

        var homeowner = await _context.Homeowners.SingleOrDefaultAsync(h => h.HomeownerId == headHomeownerId && !h.IsDeleted);
        if (homeowner is null)
        {
            return UnitOperationResult.Fail("Homeowner not found.");
        }

        if (homeowner.SubdivisionId != unit.SubdivisionId)
        {
            return UnitOperationResult.Fail("The selected homeowner belongs to a different subdivision.");
        }

        unit.HeadHomeownerId = headHomeownerId;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Units", unitId, $"Set homeowner '{GetFullName(homeowner)}' as head homeowner for unit '{unit.UnitNumber}'.");

        return UnitOperationResult.Successful();
    }

    private IQueryable<Unit> BaseQuery(int? subdivisionId, bool includeCreator = false)
    {
        IQueryable<Unit> query = _context.Units
            .AsNoTracking()
            .Include(u => u.Subdivision)
            .Include(u => u.Phase)
            .Include(u => u.HeadHomeowner)
            .Include(u => u.Homeowners);

        if (subdivisionId.HasValue)
        {
            query = query.Where(u => u.SubdivisionId == subdivisionId.Value);
        }

        if (includeCreator)
        {
            query = query.Include(u => u.CreatedByUser);
        }

        return query;
    }

    private async Task<int> ResolveSubdivisionIdAsync(int subdivisionId, int? phaseId, int actorUserId)
    {
        if (subdivisionId > 0)
        {
            return subdivisionId;
        }

        if (phaseId.HasValue)
        {
            var phaseSubdivisionId = await _context.Phases
                .AsNoTracking()
                .Where(phase => phase.PhaseId == phaseId.Value)
                .Select(phase => phase.SubdivisionId)
                .SingleOrDefaultAsync();

            if (phaseSubdivisionId > 0)
            {
                return phaseSubdivisionId;
            }
        }

        var actorSubdivisionId = await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.SubdivisionId)
            .SingleOrDefaultAsync();

        if (actorSubdivisionId.HasValue)
        {
            return actorSubdivisionId.Value;
        }

        throw new InvalidOperationException("Subdivision is required for unit records.");
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

    private async Task EnsureAssignmentsBelongToSubdivisionAsync(int? phaseId, int? headHomeownerId, int subdivisionId)
    {
        if (phaseId.HasValue)
        {
            var phaseSubdivisionId = await _context.Phases
                .AsNoTracking()
                .Where(phase => phase.PhaseId == phaseId.Value)
                .Select(phase => (int?)phase.SubdivisionId)
                .SingleOrDefaultAsync();

            if (!phaseSubdivisionId.HasValue)
            {
                throw new InvalidOperationException("The selected phase could not be found.");
            }

            if (phaseSubdivisionId.Value != subdivisionId)
            {
                throw new InvalidOperationException("The selected phase does not belong to this subdivision.");
            }
        }

        if (headHomeownerId.HasValue)
        {
            var homeownerSubdivisionId = await _context.Homeowners
                .AsNoTracking()
                .Where(homeowner => homeowner.HomeownerId == headHomeownerId.Value && !homeowner.IsDeleted)
                .Select(homeowner => (int?)homeowner.SubdivisionId)
                .SingleOrDefaultAsync();

            if (!homeownerSubdivisionId.HasValue)
            {
                throw new InvalidOperationException("The selected head homeowner could not be found.");
            }

            if (homeownerSubdivisionId.Value != subdivisionId)
            {
                throw new InvalidOperationException("The selected head homeowner does not belong to this subdivision.");
            }
        }
    }

    private async Task EnsureActorCanAccessSubdivisionAsync(int subdivisionId, int actorUserId)
    {
        var actorSubdivisionId = await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.SubdivisionId)
            .SingleOrDefaultAsync();

        if (actorSubdivisionId.HasValue && actorSubdivisionId.Value != subdivisionId)
        {
            throw new UnauthorizedAccessException("You cannot manage units outside your assigned subdivision.");
        }
    }

    private static string GetFullName(Homeowner homeowner)
    {
        var parts = new[] { homeowner.FirstName, homeowner.MiddleName, homeowner.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(" ", parts);
    }
}

public sealed class UnitOperationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    public static UnitOperationResult Successful() => new() { Success = true };

    public static UnitOperationResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
