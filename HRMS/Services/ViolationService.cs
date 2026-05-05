using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class ViolationService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public ViolationService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<ViolationRecord>> GetAllAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .OrderByDescending(record => record.FiledAt)
            .ToListAsync();
    }

    public async Task<ViolationRecord?> GetByIdAsync(int id, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .SingleOrDefaultAsync(record => record.ViolationId == id);
    }

    public async Task<List<ViolationRecord>> GetByHomeownerAsync(int homeownerId, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(record => record.HomeownerId == homeownerId)
            .OrderByDescending(record => record.ViolationDate)
            .ToListAsync();
    }

    public Task<List<ViolationRecord>> GetByHomeownerIdAsync(int homeownerId) =>
        GetByHomeownerAsync(homeownerId);

    public async Task<List<ViolationRecord>> SearchAsync(int? subdivisionId, string? homeownerName, string? status, string? type)
    {
        var query = BaseQuery(subdivisionId);

        if (!string.IsNullOrWhiteSpace(homeownerName))
        {
            query = query.Where(record => record.HomeownerName.Contains(homeownerName));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(record => record.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(record => record.ViolationType == type);
        }

        return await query
            .OrderByDescending(record => record.FiledAt)
            .ToListAsync();
    }

    public Task<List<ViolationRecord>> SearchAsync(string? homeownerName, string? status, string? type) =>
        SearchAsync(null, homeownerName, status, type);

    public async Task<ViolationRecord> AddAsync(ViolationRecord record, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "violations", "Super Admin does not have write access to the Violations module.");
        record.SubdivisionId = await ResolveSubdivisionIdAsync(record.SubdivisionId, record.HomeownerId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(record.SubdivisionId, actorUserId);
        record.ViolationType = record.ViolationType.Trim();
        record.HomeownerName = record.HomeownerName.Trim();
        record.Details = record.Details.Trim();
        record.Status = string.IsNullOrWhiteSpace(record.Status) ? "Open" : record.Status.Trim();
        record.Resolution = NormalizeOptional(record.Resolution);
        record.ViolationDate = NormalizeDate(record.ViolationDate);
        record.FiledAt = DateTime.UtcNow.ToString("o");
        record.FiledBy = actorUserId;
        record.UpdatedAt = null;
        record.UpdatedBy = null;
        record.ViolationNumber = await GenerateViolationNumberAsync(DateTime.UtcNow.Year, record.SubdivisionId);

        _context.ViolationRecords.Add(record);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "ViolationRecords", record.ViolationId, $"Filed violation '{record.ViolationNumber}'.");

        return record;
    }

    public async Task<ViolationRecord?> UpdateAsync(ViolationRecord record, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "violations", "Super Admin does not have write access to the Violations module.");
        var existing = await _context.ViolationRecords
            .SingleOrDefaultAsync(item => item.ViolationId == record.ViolationId);

        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);
        var targetSubdivisionId = await ResolveSubdivisionIdAsync(record.SubdivisionId == 0 ? existing.SubdivisionId : record.SubdivisionId, record.HomeownerId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(targetSubdivisionId, actorUserId);

        existing.HomeownerId = record.HomeownerId;
        existing.SubdivisionId = targetSubdivisionId;
        existing.HomeownerName = record.HomeownerName.Trim();
        existing.ViolationType = record.ViolationType.Trim();
        existing.ViolationDate = NormalizeDate(record.ViolationDate);
        existing.Details = record.Details.Trim();
        existing.Status = string.IsNullOrWhiteSpace(record.Status) ? "Open" : record.Status.Trim();
        existing.Resolution = NormalizeOptional(record.Resolution);
        existing.UpdatedAt = DateTime.UtcNow.ToString("o");
        existing.UpdatedBy = actorUserId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "ViolationRecords", existing.ViolationId, $"Updated violation '{existing.ViolationNumber}'.");

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "violations", "Super Admin does not have write access to the Violations module.");
        var existing = await _context.ViolationRecords
            .SingleOrDefaultAsync(record => record.ViolationId == id);

        if (existing is null)
        {
            return false;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

        _context.ViolationRecords.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "ViolationRecords", existing.ViolationId, $"Deleted violation '{existing.ViolationNumber}'.");

        return true;
    }

    private IQueryable<ViolationRecord> BaseQuery(int? subdivisionId)
    {
        IQueryable<ViolationRecord> query = _context.ViolationRecords
            .AsNoTracking()
            .Include(record => record.Subdivision)
            .Include(record => record.Homeowner)
            .Include(record => record.FiledByUser)
            .Include(record => record.UpdatedByUser);

        if (subdivisionId.HasValue)
        {
            query = query.Where(record => record.SubdivisionId == subdivisionId.Value);
        }

        return query;
    }

    private async Task<string> GenerateViolationNumberAsync(int year, int subdivisionId)
    {
        var prefix = $"VIO-{year}-";
        var count = await _context.ViolationRecords.CountAsync(record =>
            record.SubdivisionId == subdivisionId &&
            record.ViolationNumber.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
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

    private async Task EnsureActorCanAccessSubdivisionAsync(int subdivisionId, int actorUserId)
    {
        var actorSubdivisionId = await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.SubdivisionId)
            .SingleOrDefaultAsync();

        if (actorSubdivisionId.HasValue && actorSubdivisionId.Value != subdivisionId)
        {
            throw new UnauthorizedAccessException("You cannot manage violations outside your assigned subdivision.");
        }
    }

    private async Task<int> ResolveSubdivisionIdAsync(int subdivisionId, int? homeownerId, int actorUserId)
    {
        if (homeownerId.HasValue)
        {
            var homeownerSubdivisionId = await _context.Homeowners
                .AsNoTracking()
                .Where(homeowner => homeowner.HomeownerId == homeownerId.Value && !homeowner.IsDeleted)
                .Select(homeowner => (int?)homeowner.SubdivisionId)
                .SingleOrDefaultAsync();

            if (!homeownerSubdivisionId.HasValue)
            {
                throw new InvalidOperationException("The selected homeowner could not be found.");
            }

            if (subdivisionId > 0 && homeownerSubdivisionId.Value != subdivisionId)
            {
                throw new InvalidOperationException("The selected homeowner does not belong to this subdivision.");
            }

            if (subdivisionId == 0)
            {
                return homeownerSubdivisionId.Value;
            }
        }

        if (subdivisionId > 0)
        {
            return subdivisionId;
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

        throw new InvalidOperationException("Subdivision is required for violation records.");
    }

    private static string NormalizeDate(string? value)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc).ToString("o");
        }

        return DateTime.UtcNow.ToString("o");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
