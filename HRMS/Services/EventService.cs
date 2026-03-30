using HRMS.Data;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class EventService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public EventService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<Event>> GetAllAsync()
    {
        return await BaseQuery()
            .OrderByDescending(evt => evt.EventDate)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(int id)
    {
        return await BaseQuery(includeCreator: true)
            .SingleOrDefaultAsync(evt => evt.EventId == id);
    }

    public async Task<Event> AddAsync(Event evt, int actorUserId)
    {
        evt.CreatedAt = DateTime.UtcNow.ToString("o");
        evt.CreatedBy = actorUserId;
        evt.EventDate = NormalizeDate(evt.EventDate);
        evt.Title = evt.Title.Trim();
        evt.EventType = evt.EventType.Trim();
        evt.Venue = NormalizeOptional(evt.Venue);
        evt.Description = NormalizeOptional(evt.Description);

        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "Events", evt.EventId, $"Created event '{evt.Title}'.");

        return evt;
    }

    public async Task<Event?> UpdateAsync(Event evt, int actorUserId)
    {
        var existing = await _context.Events
            .SingleOrDefaultAsync(record => record.EventId == evt.EventId);

        if (existing is null)
        {
            return null;
        }

        existing.Title = evt.Title.Trim();
        existing.EventDate = NormalizeDate(evt.EventDate);
        existing.EventType = evt.EventType.Trim();
        existing.Venue = NormalizeOptional(evt.Venue);
        existing.Description = NormalizeOptional(evt.Description);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Events", existing.EventId, $"Updated event '{existing.Title}'.");

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, int actorUserId)
    {
        var existing = await _context.Events
            .Include(evt => evt.Attendances)
            .SingleOrDefaultAsync(evt => evt.EventId == id);

        if (existing is null)
        {
            return false;
        }

        if (existing.Attendances.Count > 0)
        {
            _context.Attendances.RemoveRange(existing.Attendances);
        }

        _context.Events.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "Events", existing.EventId, $"Deleted event '{existing.Title}'.");

        return true;
    }

    private IQueryable<Event> BaseQuery(bool includeCreator = false)
    {
        IQueryable<Event> query = _context.Events
            .AsNoTracking()
            .Include(evt => evt.Attendances)
            .ThenInclude(attendance => attendance.Homeowner);

        if (includeCreator)
        {
            query = query.Include(evt => evt.CreatedByUser);
        }

        return query;
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
