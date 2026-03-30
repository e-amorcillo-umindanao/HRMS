using HRMS.Data;
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

    public async Task<List<InteractionLog>> GetByHomeownerAsync(int homeownerId)
    {
        return await BaseQuery()
            .Where(log => log.HomeownerId == homeownerId)
            .OrderByDescending(log => log.InteractionDate)
            .ThenByDescending(log => log.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<InteractionLog>> GetByMSMEAsync(int msmeId)
    {
        return await BaseQuery()
            .Where(log => log.MSMEId == msmeId)
            .OrderByDescending(log => log.InteractionDate)
            .ThenByDescending(log => log.CreatedAt)
            .ToListAsync();
    }

    public async Task<InteractionLog> AddAsync(InteractionLog log, int actorUserId)
    {
        ValidateSubject(log.HomeownerId, log.MSMEId);

        if (string.IsNullOrWhiteSpace(log.InteractionType))
        {
            throw new ArgumentException("Interaction type is required.", nameof(log));
        }

        var entity = new InteractionLog
        {
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

        return await BaseQuery()
            .SingleAsync(record => record.InteractionLogId == entity.InteractionLogId);
    }

    public async Task<InteractionLog?> UpdateAsync(InteractionLog log, int actorUserId)
    {
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
        existing.InteractionType = log.InteractionType.Trim();
        existing.Notes = string.IsNullOrWhiteSpace(log.Notes) ? string.Empty : log.Notes.Trim();
        existing.InteractionDate = NormalizeDate(log.InteractionDate);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "InteractionLogs", existing.InteractionLogId, "Updated interaction log.");

        return await BaseQuery()
            .SingleAsync(record => record.InteractionLogId == existing.InteractionLogId);
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
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

    private IQueryable<InteractionLog> BaseQuery()
    {
        return _context.InteractionLogs
            .AsNoTracking()
            .Include(log => log.Homeowner)
            .Include(log => log.MSME)
            .Include(log => log.CreatedByUser);
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
}
