using HRMS.Data;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class ClearanceService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public ClearanceService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<ClearanceRequest>> GetAllAsync()
    {
        return await BaseQuery()
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync();
    }

    public async Task<ClearanceRequest?> GetByIdAsync(int id)
    {
        return await BaseQuery()
            .SingleOrDefaultAsync(request => request.ClearanceId == id);
    }

    public async Task<List<ClearanceRequest>> GetByHomeownerAsync(int homeownerId)
    {
        return await BaseQuery()
            .Where(request => request.HomeownerId == homeownerId)
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync();
    }

    public Task<List<ClearanceRequest>> GetByHomeownerIdAsync(int homeownerId) =>
        GetByHomeownerAsync(homeownerId);

    public async Task<List<ClearanceRequest>> SearchAsync(string? homeownerName, string? status, string? clearanceType, int? homeownerId = null)
    {
        var query = BaseQuery();

        if (homeownerId.HasValue)
        {
            query = query.Where(request => request.HomeownerId == homeownerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(homeownerName))
        {
            query = query.Where(request =>
                request.Homeowner.FirstName.Contains(homeownerName) ||
                request.Homeowner.LastName.Contains(homeownerName) ||
                (request.Homeowner.MiddleName != null && request.Homeowner.MiddleName.Contains(homeownerName)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(request => request.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(clearanceType))
        {
            query = query.Where(request => request.ClearanceType == clearanceType);
        }

        return await query
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync();
    }

    public async Task<ClearanceRequest> AddAsync(ClearanceRequest request, int actorUserId)
    {
        request.ClearanceType = request.ClearanceType.Trim();
        request.Purpose = request.Purpose.Trim();
        request.Status = "Pending";
        request.RequestedAt = DateTime.UtcNow.ToString("o");
        request.ProcessedAt = null;
        request.ProcessedBy = null;
        request.Remarks = NormalizeOptional(request.Remarks);
        request.ValidUntil = null;

        _context.ClearanceRequests.Add(request);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "ClearanceRequests", request.ClearanceId, $"Created clearance request '{request.ClearanceType}'.");

        return request;
    }

    public async Task<ClearanceRequest?> ApproveAsync(int id, int actorUserId, string? remarks, DateTime? validUntil)
    {
        EnsurePresidentOrAbove(await GetActorRoleAsync(actorUserId));

        var existing = await _context.ClearanceRequests
            .SingleOrDefaultAsync(request => request.ClearanceId == id);

        if (existing is null)
        {
            return null;
        }

        existing.Status = "Approved";
        existing.ProcessedAt = DateTime.UtcNow.ToString("o");
        existing.ProcessedBy = actorUserId;
        existing.Remarks = NormalizeOptional(remarks);
        existing.ValidUntil = validUntil.HasValue
            ? DateTime.SpecifyKind(validUntil.Value.Date, DateTimeKind.Utc).ToString("o")
            : null;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "ClearanceRequests", existing.ClearanceId, $"Approved clearance request '{existing.ClearanceType}'.");

        return existing;
    }

    public async Task<ClearanceRequest?> RejectAsync(int id, int actorUserId, string remarks)
    {
        EnsurePresidentOrAbove(await GetActorRoleAsync(actorUserId));

        var existing = await _context.ClearanceRequests
            .SingleOrDefaultAsync(request => request.ClearanceId == id);

        if (existing is null)
        {
            return null;
        }

        existing.Status = "Rejected";
        existing.ProcessedAt = DateTime.UtcNow.ToString("o");
        existing.ProcessedBy = actorUserId;
        existing.Remarks = NormalizeOptional(remarks);
        existing.ValidUntil = null;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "ClearanceRequests", existing.ClearanceId, $"Rejected clearance request '{existing.ClearanceType}'.");

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
        var existing = await _context.ClearanceRequests
            .SingleOrDefaultAsync(request => request.ClearanceId == id);

        if (existing is null)
        {
            return false;
        }

        _context.ClearanceRequests.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "ClearanceRequests", existing.ClearanceId, $"Deleted clearance request '{existing.ClearanceType}'.");

        return true;
    }

    private IQueryable<ClearanceRequest> BaseQuery()
    {
        return _context.ClearanceRequests
            .AsNoTracking()
            .Include(request => request.Homeowner)
            .ThenInclude(homeowner => homeowner.Unit)
            .Include(request => request.ProcessedByUser);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<string?> GetActorRoleAsync(int actorUserId)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.Role.RoleName)
            .SingleOrDefaultAsync();
    }

    private static void EnsurePresidentOrAbove(string? role)
    {
        if (role is "HOA President" or "Super Admin")
        {
            return;
        }

        throw new UnauthorizedAccessException("Only the HOA President and Super Admin can approve or reject clearance requests.");
    }
}
