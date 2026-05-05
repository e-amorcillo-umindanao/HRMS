using HRMS.Data;
using HRMS.Helpers;
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

    public async Task<List<ClearanceRequest>> GetAllAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync();
    }

    public async Task<ClearanceRequest?> GetByIdAsync(int id, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .SingleOrDefaultAsync(request => request.ClearanceId == id);
    }

    public async Task<List<ClearanceRequest>> GetByHomeownerAsync(int homeownerId, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(request => request.HomeownerId == homeownerId)
            .OrderByDescending(request => request.RequestedAt)
            .ToListAsync();
    }

    public Task<List<ClearanceRequest>> GetByHomeownerIdAsync(int homeownerId) =>
        GetByHomeownerAsync(homeownerId);

    public async Task<List<ClearanceRequest>> SearchAsync(int? subdivisionId, string? homeownerName, string? status, string? clearanceType, int? homeownerId = null)
    {
        var query = BaseQuery(subdivisionId);

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

    public Task<List<ClearanceRequest>> SearchAsync(string? homeownerName, string? status, string? clearanceType, int? homeownerId = null) =>
        SearchAsync(null, homeownerName, status, clearanceType, homeownerId);

    public async Task<ClearanceRequest> AddAsync(ClearanceRequest request, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "clearance", "You do not have write access to the Clearance module.");
        request.SubdivisionId = await ResolveSubdivisionIdAsync(request.SubdivisionId, request.HomeownerId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(request.SubdivisionId, actorUserId);
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
        EnsurePresident(await GetActorRoleAsync(actorUserId));

        var existing = await _context.ClearanceRequests
            .SingleOrDefaultAsync(request => request.ClearanceId == id);

        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

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
        EnsurePresident(await GetActorRoleAsync(actorUserId));

        var existing = await _context.ClearanceRequests
            .SingleOrDefaultAsync(request => request.ClearanceId == id);

        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

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
        await EnsureCanWriteAsync(actorUserId, "clearance", "You do not have write access to the Clearance module.");
        var existing = await _context.ClearanceRequests
            .SingleOrDefaultAsync(request => request.ClearanceId == id);

        if (existing is null)
        {
            return false;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

        _context.ClearanceRequests.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "ClearanceRequests", existing.ClearanceId, $"Deleted clearance request '{existing.ClearanceType}'.");

        return true;
    }

    private IQueryable<ClearanceRequest> BaseQuery(int? subdivisionId)
    {
        IQueryable<ClearanceRequest> query = _context.ClearanceRequests
            .AsNoTracking()
            .Include(request => request.Subdivision)
            .Include(request => request.Homeowner)
            .ThenInclude(homeowner => homeowner.Unit)
            .Include(request => request.ProcessedByUser);

        if (subdivisionId.HasValue)
        {
            query = query.Where(request => request.SubdivisionId == subdivisionId.Value);
        }

        return query;
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

    private async Task EnsureCanWriteAsync(int actorUserId, string module, string message)
    {
        var role = await GetActorRoleAsync(actorUserId);
        if (!AccessHelper.CanWrite(role ?? string.Empty, module))
        {
            throw new UnauthorizedAccessException(message);
        }
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

    private static void EnsurePresident(string? role)
    {
        if (role is "HOA President")
        {
            return;
        }

        throw new UnauthorizedAccessException("Only the HOA President can approve or reject clearance requests.");
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
            throw new UnauthorizedAccessException("You cannot manage clearance requests outside your assigned subdivision.");
        }
    }
}
