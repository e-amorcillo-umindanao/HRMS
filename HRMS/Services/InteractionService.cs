using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class InteractionService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public InteractionService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<InteractionLog>> GetByHomeownerAsync(int homeownerId, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(log => log.HomeownerId == homeownerId)
            .OrderByDescending(log => log.InteractionDate)
            .ThenByDescending(log => log.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<InteractionLog>> GetByMSMEAsync(int msmeId, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(log => log.MSMEId == msmeId)
            .OrderByDescending(log => log.InteractionDate)
            .ThenByDescending(log => log.CreatedAt)
            .ToListAsync();
    }

    public async Task<InteractionLog> AddAsync(InteractionLog log, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "engagement", "You do not have write access to the Engagement module.");
        ValidateSubject(log.HomeownerId, log.MSMEId);

        if (string.IsNullOrWhiteSpace(log.InteractionType))
        {
            throw new ArgumentException("Interaction type is required.", nameof(log));
        }

        var entity = new InteractionLog
        {
            SubdivisionId = await ResolveSubdivisionIdAsync(log.SubdivisionId, log.HomeownerId, log.MSMEId, actorUserId),
            HomeownerId = log.HomeownerId,
            MSMEId = log.MSMEId,
            InteractionType = log.InteractionType.Trim(),
            Notes = string.IsNullOrWhiteSpace(log.Notes) ? string.Empty : log.Notes.Trim(),
            InteractionDate = NormalizeDate(log.InteractionDate),
            CreatedAt = DateTime.UtcNow.ToString("o"),
            CreatedBy = actorUserId
        };

        _context.InteractionLogs.Add(entity);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "InteractionLogs", entity.InteractionLogId, "Created interaction log.");

        return await BaseQuery(null)
            .SingleAsync(record => record.InteractionLogId == entity.InteractionLogId);
    }

    public async Task<InteractionLog?> UpdateAsync(InteractionLog log, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "engagement", "You do not have write access to the Engagement module.");
        ValidateSubject(log.HomeownerId, log.MSMEId);

        if (string.IsNullOrWhiteSpace(log.InteractionType))
        {
            throw new ArgumentException("Interaction type is required.", nameof(log));
        }

        var existing = await _context.InteractionLogs
            .SingleOrDefaultAsync(record => record.InteractionLogId == log.InteractionLogId);

        if (existing is null)
        {
            return null;
        }

        existing.HomeownerId = log.HomeownerId;
        existing.MSMEId = log.MSMEId;
        existing.SubdivisionId = log.SubdivisionId == 0
            ? existing.SubdivisionId
            : log.SubdivisionId;
        existing.InteractionType = log.InteractionType.Trim();
        existing.Notes = string.IsNullOrWhiteSpace(log.Notes) ? string.Empty : log.Notes.Trim();
        existing.InteractionDate = NormalizeDate(log.InteractionDate);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "InteractionLogs", existing.InteractionLogId, "Updated interaction log.");

        return await BaseQuery(null)
            .SingleAsync(record => record.InteractionLogId == existing.InteractionLogId);
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "engagement", "You do not have write access to the Engagement module.");
        var existing = await _context.InteractionLogs
            .SingleOrDefaultAsync(record => record.InteractionLogId == id);

        if (existing is null)
        {
            return false;
        }

        _context.InteractionLogs.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "InteractionLogs", existing.InteractionLogId, "Deleted interaction log.");

        return true;
    }

    private IQueryable<InteractionLog> BaseQuery(int? subdivisionId)
    {
        IQueryable<InteractionLog> query = _context.InteractionLogs
            .AsNoTracking()
            .Include(log => log.Subdivision)
            .Include(log => log.Homeowner)
            .Include(log => log.MSME)
            .Include(log => log.CreatedByUser);

        if (subdivisionId.HasValue)
        {
            query = query.Where(log => log.SubdivisionId == subdivisionId.Value);
        }

        return query;
    }

    private static void ValidateSubject(int? homeownerId, int? msmeId)
    {
        var hasHomeowner = homeownerId.HasValue;
        var hasMsme = msmeId.HasValue;

        if (hasHomeowner == hasMsme)
        {
            throw new ArgumentException("Exactly one of HomeownerId or MSMEId must be set.");
        }
    }

    private static string NormalizeDate(string? value)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc).ToString("o");
        }

        return DateTime.UtcNow.ToString("o");
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

    private async Task<int> ResolveSubdivisionIdAsync(int subdivisionId, int? homeownerId, int? msmeId, int actorUserId)
    {
        if (subdivisionId > 0)
        {
            return subdivisionId;
        }

        if (homeownerId.HasValue)
        {
            var homeownerSubdivisionId = await _context.Homeowners
                .AsNoTracking()
                .Where(homeowner => homeowner.HomeownerId == homeownerId.Value && !homeowner.IsDeleted)
                .Select(homeowner => homeowner.SubdivisionId)
                .SingleOrDefaultAsync();

            if (homeownerSubdivisionId > 0)
            {
                return homeownerSubdivisionId;
            }
        }

        if (msmeId.HasValue)
        {
            var msmeSubdivisionId = await _context.MSMEs
                .AsNoTracking()
                .Where(msme => msme.MSMEId == msmeId.Value)
                .Select(msme => msme.SubdivisionId)
                .SingleOrDefaultAsync();

            if (msmeSubdivisionId > 0)
            {
                return msmeSubdivisionId;
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

        throw new InvalidOperationException("Subdivision is required for interaction logs.");
    }
}
