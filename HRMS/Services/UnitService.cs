using HRMS.Data;
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

    public async Task<List<Unit>> GetAllAsync()
    {
        return await BaseQuery()
            .OrderBy(u => u.UnitNumber)
            .ThenBy(u => u.Address)
            .ToListAsync();
    }

    public async Task<Unit?> GetByIdAsync(int id)
    {
        return await BaseQuery(includeCreator: true)
            .SingleOrDefaultAsync(u => u.UnitId == id);
    }

    public async Task<List<Unit>> SearchAsync(string? address, int? phaseId)
    {
        var query = BaseQuery();

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

    public async Task<Unit> AddAsync(Unit unit, int actorUserId)
    {
        unit.CreatedAt = DateTime.UtcNow.ToString("o");
        unit.CreatedBy = actorUserId;

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "Units", unit.UnitId, $"Created unit '{unit.UnitNumber}'.");

        return unit;
    }

    public async Task<Unit?> UpdateAsync(Unit unit, int actorUserId)
    {
        var existing = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == unit.UnitId);
        if (existing is null)
        {
            return null;
        }

        existing.UnitNumber = unit.UnitNumber;
        existing.Address = unit.Address;
        existing.PhaseId = unit.PhaseId;
        existing.HeadHomeownerId = unit.HeadHomeownerId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Units", existing.UnitId, $"Updated unit '{existing.UnitNumber}'.");

        return existing;
    }

    public async Task<UnitOperationResult> DeleteAsync(int id, int actorUserId)
    {
        var unit = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == id);
        if (unit is null)
        {
            return UnitOperationResult.Fail("Unit not found.");
        }

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

    public async Task<List<Homeowner>> GetOccupantsAsync(int unitId)
    {
        return await _context.Homeowners
            .AsNoTracking()
            .Where(h => h.UnitId == unitId && !h.IsDeleted)
            .OrderBy(h => h.LastName)
            .ThenBy(h => h.FirstName)
            .ToListAsync();
    }

    public async Task<UnitOperationResult> AssignHomeownerAsync(int unitId, int homeownerId, int actorUserId)
    {
        var unit = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == unitId);
        if (unit is null)
        {
            return UnitOperationResult.Fail("Unit not found.");
        }

        var homeowner = await _context.Homeowners.SingleOrDefaultAsync(h => h.HomeownerId == homeownerId && !h.IsDeleted);
        if (homeowner is null)
        {
            return UnitOperationResult.Fail("Homeowner not found.");
        }

        homeowner.UnitId = unitId;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Units", unitId, $"Assigned homeowner '{GetFullName(homeowner)}' to unit '{unit.UnitNumber}'.");

        return UnitOperationResult.Successful();
    }

    public async Task<UnitOperationResult> SetHeadHomeownerAsync(int unitId, int headHomeownerId, int actorUserId)
    {
        var unit = await _context.Units.SingleOrDefaultAsync(u => u.UnitId == unitId);
        if (unit is null)
        {
            return UnitOperationResult.Fail("Unit not found.");
        }

        var homeowner = await _context.Homeowners.SingleOrDefaultAsync(h => h.HomeownerId == headHomeownerId && !h.IsDeleted);
        if (homeowner is null)
        {
            return UnitOperationResult.Fail("Homeowner not found.");
        }

        unit.HeadHomeownerId = headHomeownerId;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Units", unitId, $"Set homeowner '{GetFullName(homeowner)}' as head homeowner for unit '{unit.UnitNumber}'.");

        return UnitOperationResult.Successful();
    }

    private IQueryable<Unit> BaseQuery(bool includeCreator = false)
    {
        IQueryable<Unit> query = _context.Units
            .AsNoTracking()
            .Include(u => u.Phase)
            .Include(u => u.HeadHomeowner)
            .Include(u => u.Homeowners);

        if (includeCreator)
        {
            query = query.Include(u => u.CreatedByUser);
        }

        return query;
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
