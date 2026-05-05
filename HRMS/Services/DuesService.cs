using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class DuesService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public DuesService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<DuesRecord>> GetAllAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .ThenBy(record => record.Homeowner.LastName)
            .ThenBy(record => record.Homeowner.FirstName)
            .ToListAsync();
    }

    public async Task<DuesRecord?> GetByIdAsync(int id, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId, includeCreator: true)
            .SingleOrDefaultAsync(record => record.DuesId == id);
    }

    public async Task<List<DuesRecord>> GetByHomeownerAsync(int homeownerId, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(record => record.HomeownerId == homeownerId)
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .ToListAsync();
    }

    public Task<List<DuesRecord>> GetByHomeownerIdAsync(int homeownerId) =>
        GetByHomeownerAsync(homeownerId);

    public async Task<List<DuesRecord>> GetByMonthYearAsync(int month, int year, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(record => record.Month == month && record.Year == year)
            .OrderBy(record => record.Homeowner.LastName)
            .ThenBy(record => record.Homeowner.FirstName)
            .ToListAsync();
    }

    public async Task<DuesRecord> AddAsync(DuesRecord dues, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "dues", "You do not have write access to the Dues module.");
        dues.SubdivisionId = await ResolveSubdivisionIdAsync(dues.SubdivisionId, dues.HomeownerId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(dues.SubdivisionId, actorUserId);
        await EnsureUniqueAsync(dues.HomeownerId, dues.Month, dues.Year, null);

        dues.Status = NormalizeStatus(dues.Status);
        dues.DueDate = NormalizeDate(dues.DueDate);
        dues.PaidDate = dues.Status == "Paid" ? NormalizeOptionalDate(dues.PaidDate) : null;
        dues.CreatedAt = DateTime.UtcNow.ToString("o");
        dues.CreatedBy = actorUserId;

        _context.DuesRecords.Add(dues);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "DuesRecords", dues.DuesId, $"Created dues record for homeowner {dues.HomeownerId} ({dues.Month}/{dues.Year}).");

        return dues;
    }

    public async Task<DuesRecord?> UpdateAsync(DuesRecord dues, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "dues", "You do not have write access to the Dues module.");
        var existing = await _context.DuesRecords
            .SingleOrDefaultAsync(record => record.DuesId == dues.DuesId);

        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);
        var targetSubdivisionId = await ResolveSubdivisionIdAsync(dues.SubdivisionId == 0 ? existing.SubdivisionId : dues.SubdivisionId, dues.HomeownerId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(targetSubdivisionId, actorUserId);

        await EnsureUniqueAsync(dues.HomeownerId, dues.Month, dues.Year, dues.DuesId);

        existing.HomeownerId = dues.HomeownerId;
        existing.SubdivisionId = targetSubdivisionId;
        existing.Month = dues.Month;
        existing.Year = dues.Year;
        existing.Amount = dues.Amount;
        existing.DueDate = NormalizeDate(dues.DueDate);
        existing.Status = NormalizeStatus(dues.Status);
        existing.PaidDate = existing.Status == "Paid" ? NormalizeOptionalDate(dues.PaidDate) : null;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "DuesRecords", existing.DuesId, $"Updated dues record for homeowner {existing.HomeownerId} ({existing.Month}/{existing.Year}).");

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "dues", "You do not have write access to the Dues module.");
        var existing = await _context.DuesRecords
            .SingleOrDefaultAsync(record => record.DuesId == id);

        if (existing is null)
        {
            return false;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

        _context.DuesRecords.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "DuesRecords", existing.DuesId, $"Deleted dues record for homeowner {existing.HomeownerId} ({existing.Month}/{existing.Year}).");

        return true;
    }

    public async Task<DuesRecord?> MarkAsPaidAsync(int duesId, DateTime paidDate, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "dues", "You do not have write access to the Dues module.");
        var existing = await _context.DuesRecords
            .SingleOrDefaultAsync(record => record.DuesId == duesId);

        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

        existing.Status = "Paid";
        existing.PaidDate = DateTime.SpecifyKind(paidDate.Date, DateTimeKind.Utc).ToString("o");

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "DuesRecords", existing.DuesId, $"Marked dues record {existing.DuesId} as paid.");

        return existing;
    }

    private IQueryable<DuesRecord> BaseQuery(int? subdivisionId, bool includeCreator = false)
    {
        IQueryable<DuesRecord> query = _context.DuesRecords
            .AsNoTracking()
            .Include(record => record.Subdivision)
            .Include(record => record.Homeowner)
            .ThenInclude(homeowner => homeowner.Unit);

        if (subdivisionId.HasValue)
        {
            query = query.Where(record => record.SubdivisionId == subdivisionId.Value);
        }

        if (includeCreator)
        {
            query = query.Include(record => record.CreatedByUser);
        }

        return query;
    }

    private async Task<int> ResolveSubdivisionIdAsync(int subdivisionId, int homeownerId, int actorUserId)
    {
        var homeownerSubdivisionId = await _context.Homeowners
            .AsNoTracking()
            .Where(homeowner => homeowner.HomeownerId == homeownerId && !homeowner.IsDeleted)
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

        if (subdivisionId > 0)
        {
            return subdivisionId;
        }

        return homeownerSubdivisionId.Value;
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
            throw new UnauthorizedAccessException("You cannot manage dues records outside your assigned subdivision.");
        }
    }

    private async Task EnsureUniqueAsync(int homeownerId, int month, int year, int? existingId)
    {
        var duplicateExists = await _context.DuesRecords.AnyAsync(record =>
            record.HomeownerId == homeownerId &&
            record.Month == month &&
            record.Year == year &&
            (!existingId.HasValue || record.DuesId != existingId.Value));

        if (duplicateExists)
        {
            throw new InvalidOperationException("A dues record already exists for this homeowner, month, and year.");
        }
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

    private static string NormalizeStatus(string? status) =>
        string.IsNullOrWhiteSpace(status) ? "Unpaid" : status.Trim();

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
