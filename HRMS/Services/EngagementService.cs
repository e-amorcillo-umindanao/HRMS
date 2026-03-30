using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class EngagementService
{
    private readonly AppDbContext _context;

    public EngagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<EngagementSummary?> GetEngagementSummaryAsync(int homeownerId)
    {
        var homeowner = await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Phase)
            .Include(record => record.Unit)
            .SingleOrDefaultAsync(record => record.HomeownerId == homeownerId && !record.IsDeleted);

        if (homeowner is null)
        {
            return null;
        }

        var totalEvents = await GetPastEventCountAsync();

        var attendanceRecords = await _context.Attendances
            .AsNoTracking()
            .Include(record => record.Event)
            .Where(record => record.HomeownerId == homeownerId && record.Status == "Present")
            .ToListAsync();

        var interactionDates = await _context.InteractionLogs
            .AsNoTracking()
            .Where(record => record.HomeownerId == homeownerId)
            .Select(record => record.InteractionDate)
            .ToListAsync();

        return BuildSummary(homeowner, totalEvents, attendanceRecords, interactionDates);
    }

    public async Task<List<EngagementSummary>> GetAllEngagementSummariesAsync()
    {
        var homeowners = await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Phase)
            .Include(record => record.Unit)
            .Where(record => !record.IsDeleted && record.Status == "Active")
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .ToListAsync();

        if (homeowners.Count == 0)
        {
            return [];
        }

        var homeownerIds = homeowners
            .Select(record => record.HomeownerId)
            .ToList();

        var totalEvents = await GetPastEventCountAsync();

        var attendanceRecords = await _context.Attendances
            .AsNoTracking()
            .Include(record => record.Event)
            .Where(record => homeownerIds.Contains(record.HomeownerId) && record.Status == "Present")
            .ToListAsync();

        var attendanceByHomeowner = attendanceRecords
            .Where(record => IsPastDate(record.Event.EventDate))
            .GroupBy(record => record.HomeownerId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var interactionDates = await _context.InteractionLogs
            .AsNoTracking()
            .Where(record => record.HomeownerId.HasValue && homeownerIds.Contains(record.HomeownerId.Value))
            .Select(record => new InteractionDateMetric
            {
                HomeownerId = record.HomeownerId!.Value,
                InteractionDate = record.InteractionDate
            })
            .ToListAsync();

        var interactionDatesByHomeowner = interactionDates
            .GroupBy(record => record.HomeownerId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(record => record.InteractionDate).ToList());

        return homeowners
            .Select(homeowner =>
            {
                attendanceByHomeowner.TryGetValue(homeowner.HomeownerId, out var homeownerAttendances);
                interactionDatesByHomeowner.TryGetValue(homeowner.HomeownerId, out var homeownerInteractions);

                return BuildSummary(
                    homeowner,
                    totalEvents,
                    homeownerAttendances ?? [],
                    homeownerInteractions ?? []);
            })
            .OrderByDescending(summary => summary.Score)
            .ThenBy(summary => summary.HomeownerName)
            .ToList();
    }

    private async Task<int> GetPastEventCountAsync()
    {
        var eventDates = await _context.Events
            .AsNoTracking()
            .Select(record => record.EventDate)
            .ToListAsync();

        return eventDates.Count(IsPastDate);
    }

    private static EngagementSummary BuildSummary(
        Homeowner homeowner,
        int totalEvents,
        IReadOnlyCollection<Attendance> attendanceRecords,
        IReadOnlyCollection<string> interactionDates)
    {
        var presentCount = attendanceRecords.Count(record => IsPastDate(record.Event.EventDate));
        var parsedInteractionDates = interactionDates
            .Select(ParseDate)
            .Where(date => date.HasValue)
            .Select(date => date!.Value)
            .ToList();

        var semesterStart = GetCurrentSemesterStart();
        var semesterEnd = semesterStart.AddMonths(6);
        var interactionsInSemester = parsedInteractionDates.Count(date =>
            date.Date >= semesterStart && date.Date < semesterEnd);
        var lastInteractionDate = parsedInteractionDates.Count == 0
            ? (DateTime?)null
            : parsedInteractionDates.Max();

        var (score, label, color) = EngagementCalculator.ComputeScore(
            presentCount,
            totalEvents,
            interactionsInSemester,
            lastInteractionDate);

        return new EngagementSummary
        {
            HomeownerId = homeowner.HomeownerId,
            Homeowner = homeowner,
            HomeownerName = GetFullName(homeowner),
            Score = score,
            Label = label,
            Color = color,
            LastInteractionDate = lastInteractionDate,
            EventsAttended = presentCount,
            TotalEvents = totalEvents,
            InteractionsInSemester = interactionsInSemester
        };
    }

    private static DateTime GetCurrentSemesterStart()
    {
        var today = DateTime.UtcNow.Date;
        return today.Month <= 6
            ? new DateTime(today.Year, 1, 1)
            : new DateTime(today.Year, 7, 1);
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, out var parsed) ? parsed : null;

    private static bool IsPastDate(string? value)
    {
        var parsed = ParseDate(value);
        return parsed.HasValue && parsed.Value.Date <= DateTime.UtcNow.Date;
    }

    private static string GetFullName(Homeowner homeowner)
    {
        var parts = new[] { homeowner.FirstName, homeowner.MiddleName, homeowner.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(" ", parts);
    }

    private sealed class InteractionDateMetric
    {
        public int HomeownerId { get; set; }
        public string InteractionDate { get; set; } = string.Empty;
    }
}

public sealed class EngagementSummary
{
    public int HomeownerId { get; init; }
    public Homeowner Homeowner { get; init; } = null!;
    public string HomeownerName { get; init; } = string.Empty;
    public double Score { get; init; }
    public string Label { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public DateTime? LastInteractionDate { get; init; }
    public int EventsAttended { get; init; }
    public int TotalEvents { get; init; }
    public int InteractionsInSemester { get; init; }
}
