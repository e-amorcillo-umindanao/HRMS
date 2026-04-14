using HRMS.Data;
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

    public async Task<List<ViolationRecord>> GetAllAsync()
    {
        return await BaseQuery()
            .OrderByDescending(record => record.FiledAt)
            .ToListAsync();
    }

    public async Task<ViolationRecord?> GetByIdAsync(int id)
    {
        return await BaseQuery()
            .SingleOrDefaultAsync(record => record.ViolationId == id);
    }

    public async Task<List<ViolationRecord>> GetByHomeownerAsync(int homeownerId)
    {
        return await BaseQuery()
            .Where(record => record.HomeownerId == homeownerId)
            .OrderByDescending(record => record.ViolationDate)
            .ToListAsync();
    }

    public Task<List<ViolationRecord>> GetByHomeownerIdAsync(int homeownerId) =>
        GetByHomeownerAsync(homeownerId);

    public async Task<List<ViolationRecord>> SearchAsync(string? homeownerName, string? status, string? type)
    {
        var query = BaseQuery();

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

    public async Task<ViolationRecord> AddAsync(ViolationRecord record, int actorUserId)
    {
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
        record.ViolationNumber = await GenerateViolationNumberAsync(DateTime.UtcNow.Year);

        _context.ViolationRecords.Add(record);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "ViolationRecords", record.ViolationId, $"Filed violation '{record.ViolationNumber}'.");

        return record;
    }

    public async Task<ViolationRecord?> UpdateAsync(ViolationRecord record, int actorUserId)
    {
        var existing = await _context.ViolationRecords
            .SingleOrDefaultAsync(item => item.ViolationId == record.ViolationId);

        if (existing is null)
        {
            return null;
        }

        existing.HomeownerId = record.HomeownerId;
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
        var existing = await _context.ViolationRecords
            .SingleOrDefaultAsync(record => record.ViolationId == id);

        if (existing is null)
        {
            return false;
        }

        _context.ViolationRecords.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "ViolationRecords", existing.ViolationId, $"Deleted violation '{existing.ViolationNumber}'.");

        return true;
    }

    private IQueryable<ViolationRecord> BaseQuery()
    {
        return _context.ViolationRecords
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Include(record => record.FiledByUser)
            .Include(record => record.UpdatedByUser);
    }

    private async Task<string> GenerateViolationNumberAsync(int year)
    {
        var prefix = $"VIO-{year}-";
        var count = await _context.ViolationRecords.CountAsync(record => record.ViolationNumber.StartsWith(prefix));
        return $"{prefix}{(count + 1):D4}";
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
