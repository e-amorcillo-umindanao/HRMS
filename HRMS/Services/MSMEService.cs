using HRMS.Data;
using HRMS.Helpers;
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

    public async Task<List<MSME>> GetAllAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .OrderBy(msme => msme.BusinessName)
            .ToListAsync();
    }

    public async Task<MSME?> GetByIdAsync(int id, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId, includeCreator: true)
            .SingleOrDefaultAsync(msme => msme.MSMEId == id);
    }

    public async Task<List<MSME>> SearchAsync(int? subdivisionId, string? name, string? status, string? businessType)
    {
        var query = BaseQuery(subdivisionId);

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

    public Task<List<MSME>> SearchAsync(string? name, string? status, string? businessType) =>
        SearchAsync(null, name, status, businessType);

    public async Task<MSME> AddAsync(MSME msme, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "msme", "Only Super Admin can manage MSME records.");
        msme.SubdivisionId = await ResolveSubdivisionIdAsync(msme.SubdivisionId, msme.HomeownerId, actorUserId);
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
        await EnsureCanWriteAsync(actorUserId, "msme", "Only Super Admin can manage MSME records.");
        var existing = await _context.MSMEs
            .SingleOrDefaultAsync(record => record.MSMEId == msme.MSMEId);

        if (existing is null)
        {
            return null;
        }

        existing.BusinessName = msme.BusinessName.Trim();
        existing.BusinessType = msme.BusinessType.Trim();
        existing.SubdivisionId = msme.SubdivisionId == 0 ? existing.SubdivisionId : msme.SubdivisionId;
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
        await EnsureCanWriteAsync(actorUserId, "msme", "Only Super Admin can manage MSME records.");
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

    public async Task<List<MSME>> GetByHomeownerAsync(int homeownerId, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(msme => msme.HomeownerId == homeownerId)
            .OrderBy(msme => msme.BusinessName)
            .ToListAsync();
    }

    private IQueryable<MSME> BaseQuery(int? subdivisionId, bool includeCreator = false)
    {
        IQueryable<MSME> query = _context.MSMEs
            .AsNoTracking()
            .Include(msme => msme.Subdivision)
            .Include(msme => msme.Homeowner)
            .Include(msme => msme.Unit)
            .Include(msme => msme.InteractionLogs);

        if (subdivisionId.HasValue)
        {
            query = query.Where(msme => msme.SubdivisionId == subdivisionId.Value);
        }

        if (includeCreator)
        {
            query = query.Include(msme => msme.CreatedByUser);
        }

        return query;
    }

    private async Task<int> ResolveSubdivisionIdAsync(int subdivisionId, int homeownerId, int actorUserId)
    {
        if (subdivisionId > 0)
        {
            return subdivisionId;
        }

        var homeownerSubdivisionId = await _context.Homeowners
            .AsNoTracking()
            .Where(homeowner => homeowner.HomeownerId == homeownerId && !homeowner.IsDeleted)
            .Select(homeowner => homeowner.SubdivisionId)
            .SingleOrDefaultAsync();

        if (homeownerSubdivisionId > 0)
        {
            return homeownerSubdivisionId;
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

        throw new InvalidOperationException("Subdivision is required for MSME records.");
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
