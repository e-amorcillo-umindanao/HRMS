using HRMS.Data;
using HRMS.Helpers;
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

    public async Task<List<Event>> GetAllAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .OrderByDescending(evt => evt.EventDate)
            .ToListAsync();
    }

    public async Task<Event?> GetByIdAsync(int id, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId, includeCreator: true)
            .SingleOrDefaultAsync(evt => evt.EventId == id);
    }

    public async Task<Event> AddAsync(Event evt, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "events", "You do not have write access to the Events module.");
        evt.SubdivisionId = await ResolveSubdivisionIdAsync(evt.SubdivisionId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(evt.SubdivisionId, actorUserId);
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
        await EnsureCanWriteAsync(actorUserId, "events", "You do not have write access to the Events module.");
        var existing = await _context.Events
            .SingleOrDefaultAsync(record => record.EventId == evt.EventId);

        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);
        var targetSubdivisionId = evt.SubdivisionId == 0 ? existing.SubdivisionId : evt.SubdivisionId;
        await EnsureActorCanAccessSubdivisionAsync(targetSubdivisionId, actorUserId);

        existing.Title = evt.Title.Trim();
        existing.SubdivisionId = targetSubdivisionId;
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
        await EnsureCanWriteAsync(actorUserId, "events", "You do not have write access to the Events module.");
        var existing = await _context.Events
            .Include(evt => evt.Attendances)
            .SingleOrDefaultAsync(evt => evt.EventId == id);

        if (existing is null)
        {
            return false;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

        if (existing.Attendances.Count > 0)
        {
            _context.Attendances.RemoveRange(existing.Attendances);
        }

        _context.Events.Remove(existing);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "Events", existing.EventId, $"Deleted event '{existing.Title}'.");

        return true;
    }

    private IQueryable<Event> BaseQuery(int? subdivisionId, bool includeCreator = false)
    {
        IQueryable<Event> query = _context.Events
            .AsNoTracking()
            .Include(evt => evt.Subdivision)
            .Include(evt => evt.Attendances)
            .ThenInclude(attendance => attendance.Homeowner);

        if (subdivisionId.HasValue)
        {
            query = query.Where(evt => evt.SubdivisionId == subdivisionId.Value);
        }

        if (includeCreator)
        {
            query = query.Include(evt => evt.CreatedByUser);
        }

        return query;
    }

    private async Task<int> ResolveSubdivisionIdAsync(int subdivisionId, int actorUserId)
    {
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

        throw new InvalidOperationException("Subdivision is required for event records.");
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
            throw new UnauthorizedAccessException("You cannot manage events outside your assigned subdivision.");
        }
    }

    private static string NormalizeDate(string? value)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc).ToString("o");
        }

        return DateTime.UtcNow.ToString("o");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
