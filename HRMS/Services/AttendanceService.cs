using HRMS.Data;
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

    public async Task<List<Attendance>> GetByEventAsync(int eventId)
    {
        return await _context.Attendances
            .AsNoTracking()
            .Include(attendance => attendance.Homeowner)
            .Include(attendance => attendance.Event)
            .Where(attendance => attendance.EventId == eventId)
            .OrderBy(attendance => attendance.Homeowner.LastName)
            .ThenBy(attendance => attendance.Homeowner.FirstName)
            .ToListAsync();
    }

    public async Task<List<Attendance>> GetByHomeownerAsync(int homeownerId)
    {
        return await _context.Attendances
            .AsNoTracking()
            .Include(attendance => attendance.Event)
            .Where(attendance => attendance.HomeownerId == homeownerId)
            .OrderByDescending(attendance => attendance.Event.EventDate)
            .ToListAsync();
    }

    public async Task<Attendance> RecordAsync(Attendance attendance, int actorUserId)
    {
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
        if (attendances.Count == 0)
        {
            return;
        }

        var eventId = attendances[0].EventId;
        var homeownerIds = attendances
            .Select(attendance => attendance.HomeownerId)
            .Distinct()
            .ToList();

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

            existing.Status = attendance.Status.Trim();
            existing.RecordedAt = DateTime.UtcNow.ToString("o");
            existing.RecordedBy = actorUserId;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Attendances", eventId, $"Bulk-recorded attendance for event {eventId}.");
    }
}
