using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class AttendanceService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public AttendanceService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<Attendance>> GetByEventAsync(int eventId, int subdivisionId)
    {
        return await _context.Attendances
            .AsNoTracking()
            .Include(attendance => attendance.Subdivision)
            .Include(attendance => attendance.Homeowner)
            .Include(attendance => attendance.Event)
            .Where(attendance =>
                attendance.EventId == eventId &&
                attendance.SubdivisionId == subdivisionId &&
                !attendance.Homeowner.IsDeleted)
            .OrderBy(attendance => attendance.Homeowner.LastName)
            .ThenBy(attendance => attendance.Homeowner.FirstName)
            .ToListAsync();
    }

    public async Task<List<Attendance>> GetByHomeownerAsync(int homeownerId, int? subdivisionId = null)
    {
        var query = _context.Attendances
            .AsNoTracking()
            .Include(attendance => attendance.Subdivision)
            .Include(attendance => attendance.Event)
            .Where(attendance => attendance.HomeownerId == homeownerId);

        if (subdivisionId.HasValue)
        {
            query = query.Where(attendance => attendance.SubdivisionId == subdivisionId.Value);
        }

        return await query
            .OrderByDescending(attendance => attendance.Event.EventDate)
            .ToListAsync();
    }

    public async Task<Attendance> RecordAsync(Attendance attendance, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "events", "You do not have write access to the Events module.");
        var subdivisionId = await EnsureEventAndHomeownerShareSubdivisionAsync(attendance.EventId, [attendance.HomeownerId], actorUserId);

        var existing = await _context.Attendances
            .SingleOrDefaultAsync(record =>
                record.EventId == attendance.EventId &&
                record.HomeownerId == attendance.HomeownerId);

        if (existing is null)
        {
            existing = new Attendance
            {
                EventId = attendance.EventId,
                HomeownerId = attendance.HomeownerId
            };

            _context.Attendances.Add(existing);
        }

        existing.SubdivisionId = subdivisionId;
        existing.Status = attendance.Status.Trim();
        existing.RecordedAt = DateTime.UtcNow.ToString("o");
        existing.RecordedBy = actorUserId;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Attendances", existing.AttendanceId, $"Recorded attendance for homeowner {existing.HomeownerId} in event {existing.EventId}.");

        return await _context.Attendances
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Include(record => record.Event)
            .SingleAsync(record => record.AttendanceId == existing.AttendanceId);
    }

    public async Task BulkRecordAsync(List<Attendance> attendances, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "events", "You do not have write access to the Events module.");
        if (attendances.Count == 0)
        {
            return;
        }

        var eventId = attendances[0].EventId;
        if (attendances.Any(attendance => attendance.EventId != eventId))
        {
            throw new InvalidOperationException("All attendance rows must belong to the same event.");
        }

        var homeownerIds = attendances
            .Select(attendance => attendance.HomeownerId)
            .Distinct()
            .ToList();

        var subdivisionId = await EnsureEventAndHomeownerShareSubdivisionAsync(eventId, homeownerIds, actorUserId);

        var existingRecords = await _context.Attendances
            .Where(record => record.EventId == eventId && homeownerIds.Contains(record.HomeownerId))
            .ToListAsync();

        foreach (var attendance in attendances)
        {
            var existing = existingRecords
                .FirstOrDefault(record => record.EventId == attendance.EventId && record.HomeownerId == attendance.HomeownerId);

            if (existing is null)
            {
                existing = new Attendance
                {
                    EventId = attendance.EventId,
                    HomeownerId = attendance.HomeownerId
                };

                _context.Attendances.Add(existing);
                existingRecords.Add(existing);
            }

            existing.SubdivisionId = subdivisionId;
            existing.Status = attendance.Status.Trim();
            existing.RecordedAt = DateTime.UtcNow.ToString("o");
            existing.RecordedBy = actorUserId;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Attendances", eventId, $"Bulk-recorded attendance for event {eventId}.");
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

    private async Task<int> EnsureEventAndHomeownerShareSubdivisionAsync(int eventId, IReadOnlyCollection<int> homeownerIds, int actorUserId)
    {
        var evt = await _context.Events
            .AsNoTracking()
            .SingleOrDefaultAsync(record => record.EventId == eventId);

        if (evt is null)
        {
            throw new InvalidOperationException("Event not found.");
        }

        var distinctHomeownerIds = homeownerIds
            .Distinct()
            .ToList();

        var homeowners = await _context.Homeowners
            .AsNoTracking()
            .Where(homeowner => distinctHomeownerIds.Contains(homeowner.HomeownerId) && !homeowner.IsDeleted)
            .Select(homeowner => new { homeowner.HomeownerId, homeowner.SubdivisionId })
            .ToListAsync();

        if (homeowners.Count != distinctHomeownerIds.Count)
        {
            throw new InvalidOperationException("One or more homeowners were not found.");
        }

        if (homeowners.Any(homeowner => homeowner.SubdivisionId != evt.SubdivisionId))
        {
            throw new InvalidOperationException("Cannot record attendance: homeowner does not belong to this subdivision.");
        }

        var actorSubdivisionId = await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.SubdivisionId)
            .SingleOrDefaultAsync();

        if (actorSubdivisionId.HasValue && actorSubdivisionId.Value != evt.SubdivisionId)
        {
            throw new UnauthorizedAccessException("You cannot manage attendance for another subdivision.");
        }

        return evt.SubdivisionId;
    }
}
