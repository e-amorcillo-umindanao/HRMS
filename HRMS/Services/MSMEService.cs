using HRMS.Data;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class MSMEService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public MSMEService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<MSME>> GetAllAsync()
    {
        return await BaseQuery()
            .OrderBy(msme => msme.BusinessName)
            .ToListAsync();
    }

    public async Task<MSME?> GetByIdAsync(int id)
    {
        return await BaseQuery(includeCreator: true)
            .SingleOrDefaultAsync(msme => msme.MSMEId == id);
    }

    public async Task<List<MSME>> SearchAsync(string? name, string? status, string? businessType)
    {
        var query = BaseQuery();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(msme => msme.BusinessName.Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(msme => msme.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(businessType))
        {
            query = query.Where(msme => msme.BusinessType == businessType);
        }

        return await query
            .OrderBy(msme => msme.BusinessName)
            .ToListAsync();
    }

    public async Task<MSME> AddAsync(MSME msme, int actorUserId)
    {
        msme.BusinessName = msme.BusinessName.Trim();
        msme.BusinessType = msme.BusinessType.Trim();
        msme.ContactNumber = NormalizeOptional(msme.ContactNumber);
        msme.Description = NormalizeOptional(msme.Description);
        msme.Status = string.IsNullOrWhiteSpace(msme.Status) ? "Active" : msme.Status.Trim();
        msme.RegistrationDate = NormalizeDate(msme.RegistrationDate);
        msme.ExpiryDate = NormalizeOptionalDate(msme.ExpiryDate);
        msme.CreatedAt = DateTime.UtcNow.ToString("o");
        msme.CreatedBy = actorUserId;

        _context.MSMEs.Add(msme);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "MSMEs", msme.MSMEId, $"Created MSME '{msme.BusinessName}'.");

        return msme;
    }

    public async Task<MSME?> UpdateAsync(MSME msme, int actorUserId)
    {
        var existing = await _context.MSMEs
            .SingleOrDefaultAsync(record => record.MSMEId == msme.MSMEId);

        if (existing is null)
        {
            return null;
        }

        existing.BusinessName = msme.BusinessName.Trim();
        existing.BusinessType = msme.BusinessType.Trim();
        existing.HomeownerId = msme.HomeownerId;
        existing.UnitId = msme.UnitId;
        existing.ContactNumber = NormalizeOptional(msme.ContactNumber);
        existing.Description = NormalizeOptional(msme.Description);
        existing.Status = string.IsNullOrWhiteSpace(msme.Status) ? "Active" : msme.Status.Trim();
        existing.RegistrationDate = NormalizeDate(msme.RegistrationDate);
        existing.ExpiryDate = NormalizeOptionalDate(msme.ExpiryDate);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "MSMEs", existing.MSMEId, $"Updated MSME '{existing.BusinessName}'.");

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
        var existing = await _context.MSMEs
            .SingleOrDefaultAsync(record => record.MSMEId == id);

        if (existing is null)
        {
            return false;
        }

        _context.MSMEs.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "MSMEs", existing.MSMEId, $"Deleted MSME '{existing.BusinessName}'.");

        return true;
    }

    public async Task<List<MSME>> GetByHomeownerAsync(int homeownerId)
    {
        return await BaseQuery()
            .Where(msme => msme.HomeownerId == homeownerId)
            .OrderBy(msme => msme.BusinessName)
            .ToListAsync();
    }

    private IQueryable<MSME> BaseQuery(bool includeCreator = false)
    {
        IQueryable<MSME> query = _context.MSMEs
            .AsNoTracking()
            .Include(msme => msme.Homeowner)
            .Include(msme => msme.Unit)
            .Include(msme => msme.InteractionLogs);

        if (includeCreator)
        {
            query = query.Include(msme => msme.CreatedByUser);
        }

        return query;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeDate(string? value)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc).ToString("o");
        }

        return DateTime.UtcNow.ToString("o");
    }

    private static string? NormalizeOptionalDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeDate(value);
    }
}
