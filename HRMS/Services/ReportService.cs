using System.Globalization;
using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class ReportService
{
    private static readonly string[] MsmStatusOrder = ["Active", "Suspended", "Expired", "Closed"];
    private static readonly string[] HomeownerStatusOrder = ["Active", "Inactive", "Transferred", "Deceased"];

    private readonly AppDbContext _context;
    private readonly EngagementService _engagementService;
    private readonly AuditService _auditService;

    public ReportService(AppDbContext context, EngagementService engagementService, AuditService auditService)
    {
        _context = context;
        _engagementService = engagementService;
        _auditService = auditService;
    }

    public async Task<DashboardKpiSummary> GetDashboardKPIsAsync(int? subdivisionId = null)
    {
        return new DashboardKpiSummary
        {
            TotalHomeowners = await _context.Homeowners.AsNoTracking().CountAsync(record => !record.IsDeleted && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value)),
            ActiveHomeowners = await _context.Homeowners.AsNoTracking().CountAsync(record => !record.IsDeleted && record.Status == "Active" && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value)),
            TotalUnits = await _context.Units.AsNoTracking().CountAsync(record => !subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value),
            TotalMSMEs = await _context.MSMEs.AsNoTracking().CountAsync(record => !subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value),
            ActiveMSMEs = await _context.MSMEs.AsNoTracking().CountAsync(record => record.Status == "Active" && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value)),
            OpenViolations = await _context.ViolationRecords.AsNoTracking().CountAsync(record => (record.Status == "Open" || record.Status == "Under Review") && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value)),
            UnpaidOrOverdueDues = await _context.DuesRecords.AsNoTracking().CountAsync(record => (record.Status == "Unpaid" || record.Status == "Overdue") && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value)),
            PendingClearances = await _context.ClearanceRequests.AsNoTracking().CountAsync(record => record.Status == "Pending" && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value))
        };
    }

    public async Task<HomeownerDashboardSummary?> GetHomeownerDashboardAsync(int homeownerId, int? subdivisionId = null)
    {
        var engagement = await _engagementService.GetEngagementSummaryAsync(homeownerId, subdivisionId);
        if (engagement is null)
        {
            return null;
        }

        var unpaidDuesCount = await _context.DuesRecords
            .AsNoTracking()
            .CountAsync(record => record.HomeownerId == homeownerId && (record.Status == "Unpaid" || record.Status == "Overdue"));

        var openViolationCount = await _context.ViolationRecords
            .AsNoTracking()
            .CountAsync(record => record.HomeownerId == homeownerId && (record.Status == "Open" || record.Status == "Under Review"));

        var pendingClearanceCount = await _context.ClearanceRequests
            .AsNoTracking()
            .CountAsync(record => record.HomeownerId == homeownerId && record.Status == "Pending");

        return new HomeownerDashboardSummary
        {
            HomeownerId = homeownerId,
            HomeownerName = engagement.HomeownerName,
            EngagementScore = engagement.Score,
            EngagementLabel = engagement.Label,
            EngagementColor = engagement.Color,
            UnpaidDuesCount = unpaidDuesCount,
            OpenViolationCount = openViolationCount,
            PendingClearanceCount = pendingClearanceCount
        };
    }

    public async Task<List<TrendPoint>> GetEngagementTrendAsync(int? subdivisionId = null)
    {
        var referenceMonths = GetReferenceMonths(6);
        var activeHomeowners = await _context.Homeowners
            .AsNoTracking()
            .Where(record => !record.IsDeleted && record.Status == "Active" && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value))
            .Select(record => record.HomeownerId)
            .ToListAsync();

        if (activeHomeowners.Count == 0)
        {
            return referenceMonths
                .Select(month => new TrendPoint
                {
                    Label = month.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Value = 0
                })
                .ToList();
        }

        var eventDates = (await _context.Events
                .AsNoTracking()
                .Select(record => record.EventDate)
                .ToListAsync())
            .Select(ParseDate)
            .Where(date => date.HasValue)
            .Select(date => date!.Value.Date)
            .ToList();

        var attendanceMetrics = (await _context.Attendances
                .AsNoTracking()
                .Include(record => record.Event)
                .Where(record => activeHomeowners.Contains(record.HomeownerId) && record.Status == "Present")
                .Select(record => new AttendanceMetric
                {
                    HomeownerId = record.HomeownerId,
                    EventDate = record.Event.EventDate
                })
                .ToListAsync())
            .GroupBy(record => record.HomeownerId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(record => ParseDate(record.EventDate))
                    .Where(date => date.HasValue)
                    .Select(date => date!.Value.Date)
                    .ToList());

        var interactionMetrics = (await _context.InteractionLogs
                .AsNoTracking()
                .Where(record => record.HomeownerId.HasValue && activeHomeowners.Contains(record.HomeownerId.Value))
                .Select(record => new InteractionMetric
                {
                    HomeownerId = record.HomeownerId!.Value,
                    InteractionDate = record.InteractionDate
                })
                .ToListAsync())
            .GroupBy(record => record.HomeownerId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(record => ParseDate(record.InteractionDate))
                    .Where(date => date.HasValue)
                    .Select(date => date!.Value.Date)
                    .ToList());

        return referenceMonths
            .Select(month =>
            {
                var monthEnd = month.AddMonths(1).AddDays(-1).Date;
                var referenceDate = monthEnd > DateTime.UtcNow.Date ? DateTime.UtcNow.Date : monthEnd;
                var totalEvents = eventDates.Count(date => date <= referenceDate);
                var semesterStart = GetSemesterStart(referenceDate);

                var scores = activeHomeowners
                    .Select(homeownerId =>
                    {
                        attendanceMetrics.TryGetValue(homeownerId, out var homeownerAttendances);
                        interactionMetrics.TryGetValue(homeownerId, out var homeownerInteractions);

                        var eventsAttended = (homeownerAttendances ?? []).Count(date => date <= referenceDate);
                        var filteredInteractions = (homeownerInteractions ?? [])
                            .Where(date => date <= referenceDate)
                            .ToList();

                        var interactionsInSemester = filteredInteractions.Count(date => date >= semesterStart && date <= referenceDate);
                        var lastInteractionDate = filteredInteractions.Count == 0
                            ? (DateTime?)null
                            : filteredInteractions.Max();

                        var (score, _, _) = EngagementCalculator.ComputeScore(
                            eventsAttended,
                            totalEvents,
                            interactionsInSemester,
                            lastInteractionDate);

                        return score;
                    })
                    .ToList();

                return new TrendPoint
                {
                    Label = month.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    Value = scores.Count == 0 ? 0 : Math.Round(scores.Average(), 2)
                };
            })
            .ToList();
    }

    public async Task<List<TrendPoint>> GetAttendanceTrendAsync(int? subdivisionId = null)
    {
        var events = await _context.Events
            .AsNoTracking()
            .Include(record => record.Attendances)
            .Where(record => !subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value)
            .ToListAsync();

        return events
            .Select(record => new
            {
                Event = record,
                EventDate = ParseDate(record.EventDate)
            })
            .Where(record => record.EventDate.HasValue)
            .OrderByDescending(record => record.EventDate!.Value)
            .Take(10)
            .OrderBy(record => record.EventDate!.Value)
            .Select(record => new TrendPoint
            {
                Label = BuildEventLabel(record.Event.Title, record.EventDate!.Value),
                Value = record.Event.Attendances.Count(attendance => attendance.Status == "Present")
            })
            .ToList();
    }

    public async Task<List<StatusBreakdownItem>> GetMSMEStatusBreakdownAsync(int? subdivisionId = null)
    {
        var counts = await _context.MSMEs
            .AsNoTracking()
            .Where(record => !subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value)
            .GroupBy(record => record.Status)
            .Select(group => new StatusBreakdownItem
            {
                Label = group.Key,
                Value = group.Count()
            })
            .ToListAsync();

        return MsmStatusOrder
            .Select(status => counts.FirstOrDefault(item => item.Label == status) ?? new StatusBreakdownItem
            {
                Label = status,
                Value = 0
            })
            .ToList();
    }

    public async Task<List<StatusBreakdownItem>> GetHomeownerStatusBreakdownAsync(int? subdivisionId = null)
    {
        var counts = await _context.Homeowners
            .AsNoTracking()
            .Where(record => !record.IsDeleted && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value))
            .GroupBy(record => record.Status)
            .Select(group => new StatusBreakdownItem
            {
                Label = group.Key,
                Value = group.Count()
            })
            .ToListAsync();

        return HomeownerStatusOrder
            .Select(status => counts.FirstOrDefault(item => item.Label == status) ?? new StatusBreakdownItem
            {
                Label = status,
                Value = 0
            })
            .ToList();
    }

    public async Task<DuesCollectionSummary> GetDuesCollectionSummaryAsync(int month, int year, int? subdivisionId = null)
    {
        var records = await _context.DuesRecords
            .AsNoTracking()
            .Where(record => record.Month == month && record.Year == year && (!subdivisionId.HasValue || record.SubdivisionId == subdivisionId.Value))
            .ToListAsync();

        return new DuesCollectionSummary
        {
            Month = month,
            Year = year,
            PaidCount = records.Count(record => record.Status == "Paid"),
            UnpaidCount = records.Count(record => record.Status == "Unpaid"),
            OverdueCount = records.Count(record => record.Status == "Overdue"),
            PaidAmount = records.Where(record => record.Status == "Paid").Sum(record => record.Amount),
            UnpaidAmount = records.Where(record => record.Status == "Unpaid").Sum(record => record.Amount),
            OverdueAmount = records.Where(record => record.Status == "Overdue").Sum(record => record.Amount)
        };
    }

    public async Task<byte[]> ExportHomeownersToPdfAsync(int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var homeowners = await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Phase)
            .Include(record => record.Unit)
            .Where(record => !record.IsDeleted && record.SubdivisionId == effectiveSubdivisionId)
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .ToListAsync();

        var rows = homeowners
            .Select(record => (IReadOnlyList<string>)new[]
            {
                GetFullName(record),
                DisplayValue(record.Status),
                DisplayValue(record.Phase?.Name),
                DisplayValue(record.Unit?.UnitNumber),
                DisplayValue(record.Categories),
                FormatDate(record.ResidencySince),
                DisplayValue(record.ContactNumber)
            })
            .ToList();

        var bytes = PdfExportHelper.GenerateTableReport(
            await GetSettingsAsync(effectiveSubdivisionId),
            "Homeowners Report",
            ["Full Name", "Status", "Phase", "Unit", "Categories", "Residency Since", "Contact Number"],
            rows);

        await LogExportAsync(actorUserId, "Homeowners PDF", effectiveSubdivisionId);
        return bytes;
    }

    public async Task<byte[]> ExportHomeownersToExcelAsync(int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var homeowners = await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Phase)
            .Include(record => record.Unit)
            .Where(record => !record.IsDeleted && record.SubdivisionId == effectiveSubdivisionId)
            .OrderBy(record => record.LastName)
            .ThenBy(record => record.FirstName)
            .ToListAsync();

        var rows = homeowners
            .Select(record => (IReadOnlyList<string>)new[]
            {
                GetFullName(record),
                DisplayValue(record.Status),
                DisplayValue(record.Phase?.Name),
                DisplayValue(record.Unit?.UnitNumber),
                DisplayValue(record.Categories),
                FormatDate(record.ResidencySince),
                DisplayValue(record.ContactNumber)
            })
            .ToList();

        var bytes = ExcelExportHelper.GenerateWorksheet(
            "Homeowners",
            ["Full Name", "Status", "Phase", "Unit", "Categories", "Residency Since", "Contact Number"],
            rows);

        await LogExportAsync(actorUserId, "Homeowners Excel", effectiveSubdivisionId);
        return bytes;
    }

    public async Task<byte[]> ExportDuesToPdfAsync(int? month, int? year, int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var query = _context.DuesRecords
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Where(record => !record.Homeowner.IsDeleted && record.SubdivisionId == effectiveSubdivisionId);

        if (month.HasValue)
        {
            query = query.Where(record => record.Month == month.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(record => record.Year == year.Value);
        }

        var duesRecords = await query
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .ThenBy(record => record.Homeowner.LastName)
            .ThenBy(record => record.Homeowner.FirstName)
            .ToListAsync();

        var rows = duesRecords
            .Select(record => (IReadOnlyList<string>)new[]
            {
                GetFullName(record.Homeowner),
                FormatMonthYear(record.Month, record.Year),
                FormatHelper.Peso(record.Amount),
                FormatDate(record.DueDate),
                FormatDate(record.PaidDate),
                DisplayValue(record.Status)
            })
            .ToList();

        var bytes = PdfExportHelper.GenerateTableReport(
            await GetSettingsAsync(effectiveSubdivisionId),
            BuildDuesReportTitle(month, year),
            ["Homeowner", "Month/Year", "Amount", "Due Date", "Paid Date", "Status"],
            rows);

        await LogExportAsync(actorUserId, "Dues PDF", effectiveSubdivisionId);
        return bytes;
    }

    public async Task<byte[]> ExportDuesToExcelAsync(int? month, int? year, int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var query = _context.DuesRecords
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Where(record => !record.Homeowner.IsDeleted && record.SubdivisionId == effectiveSubdivisionId);

        if (month.HasValue)
        {
            query = query.Where(record => record.Month == month.Value);
        }

        if (year.HasValue)
        {
            query = query.Where(record => record.Year == year.Value);
        }

        var duesRecords = await query
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .ThenBy(record => record.Homeowner.LastName)
            .ThenBy(record => record.Homeowner.FirstName)
            .ToListAsync();

        var rows = duesRecords
            .Select(record => (IReadOnlyList<string>)new[]
            {
                GetFullName(record.Homeowner),
                FormatMonthYear(record.Month, record.Year),
                FormatHelper.Peso(record.Amount),
                FormatDate(record.DueDate),
                FormatDate(record.PaidDate),
                DisplayValue(record.Status)
            })
            .ToList();

        var bytes = ExcelExportHelper.GenerateWorksheet(
            "Dues",
            ["Homeowner", "Month/Year", "Amount", "Due Date", "Paid Date", "Status"],
            rows);

        await LogExportAsync(actorUserId, "Dues Excel", effectiveSubdivisionId);
        return bytes;
    }

    public async Task<byte[]> ExportViolationsToPdfAsync(int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var violations = await _context.ViolationRecords
            .AsNoTracking()
            .Include(record => record.FiledByUser)
            .Where(record => record.SubdivisionId == effectiveSubdivisionId)
            .OrderByDescending(record => record.FiledAt)
            .ToListAsync();

        var rows = violations
            .Select(record => (IReadOnlyList<string>)new[]
            {
                DisplayValue(record.ViolationNumber),
                DisplayValue(record.HomeownerName),
                DisplayValue(record.ViolationType),
                FormatDate(record.ViolationDate),
                DisplayValue(record.Status),
                DisplayValue(record.FiledByUser?.Username)
            })
            .ToList();

        var bytes = PdfExportHelper.GenerateTableReport(
            await GetSettingsAsync(effectiveSubdivisionId),
            "Violations Report",
            ["Violation Number", "Homeowner", "Type", "Violation Date", "Status", "Filed By"],
            rows);

        await LogExportAsync(actorUserId, "Violations PDF", effectiveSubdivisionId);
        return bytes;
    }

    public async Task<byte[]> ExportViolationsToExcelAsync(int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var violations = await _context.ViolationRecords
            .AsNoTracking()
            .Include(record => record.FiledByUser)
            .Where(record => record.SubdivisionId == effectiveSubdivisionId)
            .OrderByDescending(record => record.FiledAt)
            .ToListAsync();

        var rows = violations
            .Select(record => (IReadOnlyList<string>)new[]
            {
                DisplayValue(record.ViolationNumber),
                DisplayValue(record.HomeownerName),
                DisplayValue(record.ViolationType),
                FormatDate(record.ViolationDate),
                DisplayValue(record.Status),
                DisplayValue(record.FiledByUser?.Username)
            })
            .ToList();

        var bytes = ExcelExportHelper.GenerateWorksheet(
            "Violations",
            ["Violation Number", "Homeowner", "Type", "Violation Date", "Status", "Filed By"],
            rows);

        await LogExportAsync(actorUserId, "Violations Excel", effectiveSubdivisionId);
        return bytes;
    }

    public async Task<byte[]> ExportClearancesToPdfAsync(int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var clearances = await _context.ClearanceRequests
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Include(record => record.ProcessedByUser)
            .Where(record => record.SubdivisionId == effectiveSubdivisionId)
            .OrderByDescending(record => record.RequestedAt)
            .ToListAsync();

        var rows = clearances
            .Select(record => (IReadOnlyList<string>)new[]
            {
                GetFullName(record.Homeowner),
                DisplayValue(record.ClearanceType),
                DisplayValue(record.Purpose),
                DisplayValue(record.Status),
                FormatDate(record.RequestedAt),
                FormatDate(record.ProcessedAt)
            })
            .ToList();

        var bytes = PdfExportHelper.GenerateTableReport(
            await GetSettingsAsync(effectiveSubdivisionId),
            "Clearance Requests Report",
            ["Homeowner", "Clearance Type", "Purpose", "Status", "Requested At", "Processed At"],
            rows);

        await LogExportAsync(actorUserId, "Clearances PDF", effectiveSubdivisionId);
        return bytes;
    }

    public async Task<byte[]> ExportClearancesToExcelAsync(int? subdivisionId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "reports", "You do not have permission to export reports.");
        var effectiveSubdivisionId = await ResolveReportSubdivisionIdAsync(subdivisionId, actorUserId);
        var clearances = await _context.ClearanceRequests
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Include(record => record.ProcessedByUser)
            .Where(record => record.SubdivisionId == effectiveSubdivisionId)
            .OrderByDescending(record => record.RequestedAt)
            .ToListAsync();

        var rows = clearances
            .Select(record => (IReadOnlyList<string>)new[]
            {
                GetFullName(record.Homeowner),
                DisplayValue(record.ClearanceType),
                DisplayValue(record.Purpose),
                DisplayValue(record.Status),
                FormatDate(record.RequestedAt),
                FormatDate(record.ProcessedAt)
            })
            .ToList();

        var bytes = ExcelExportHelper.GenerateWorksheet(
            "Clearances",
            ["Homeowner", "Clearance Type", "Purpose", "Status", "Requested At", "Processed At"],
            rows);

        await LogExportAsync(actorUserId, "Clearances Excel", effectiveSubdivisionId);
        return bytes;
    }

    private async Task<HOASettings?> GetSettingsAsync(int subdivisionId)
    {
        return await _context.HOASettings
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.SubdivisionId == subdivisionId);
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

    private async Task<int> ResolveReportSubdivisionIdAsync(int? subdivisionId, int actorUserId)
    {
        var actor = await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => new { user.SubdivisionId, RoleName = user.Role.RoleName })
            .SingleOrDefaultAsync();

        if (actor is null)
        {
            throw new UnauthorizedAccessException("The current user could not be resolved.");
        }

        if (string.Equals(actor.RoleName, "Super Admin", StringComparison.Ordinal))
        {
            if (!subdivisionId.HasValue || subdivisionId.Value <= 0)
            {
                throw new InvalidOperationException("Select a subdivision first.");
            }

            return subdivisionId.Value;
        }

        if (!actor.SubdivisionId.HasValue)
        {
            throw new UnauthorizedAccessException("Your account is not assigned to a subdivision.");
        }

        if (subdivisionId.HasValue && subdivisionId.Value != actor.SubdivisionId.Value)
        {
            throw new UnauthorizedAccessException("You cannot export reports for another subdivision.");
        }

        return actor.SubdivisionId.Value;
    }

    private Task LogExportAsync(int actorUserId, string exportName, int subdivisionId) =>
        _auditService.LogAsync(actorUserId, "Export", "Reports", subdivisionId, $"Generated {exportName} export.");

    private static List<DateTime> GetReferenceMonths(int count)
    {
        var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return Enumerable.Range(0, count)
            .Select(offset => currentMonth.AddMonths(-(count - 1 - offset)))
            .ToList();
    }

    private static DateTime GetSemesterStart(DateTime referenceDate)
    {
        return referenceDate.Month <= 6
            ? new DateTime(referenceDate.Year, 1, 1)
            : new DateTime(referenceDate.Year, 7, 1);
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, out var parsed) ? parsed.Date : null;

    private static string BuildEventLabel(string? title, DateTime eventDate)
    {
        var text = string.IsNullOrWhiteSpace(title) ? eventDate.ToString("MMM dd", CultureInfo.InvariantCulture) : title.Trim();
        return text.Length <= 14 ? text : $"{text[..11]}...";
    }

    private static string BuildDuesReportTitle(int? month, int? year)
    {
        if (month.HasValue && year.HasValue)
        {
            return $"Dues Report - {GetMonthName(month.Value)} {year.Value}";
        }

        if (year.HasValue)
        {
            return $"Dues Report - {year.Value}";
        }

        if (month.HasValue)
        {
            return $"Dues Report - {GetMonthName(month.Value)}";
        }

        return "Dues Report";
    }

    private static string GetFullName(Homeowner homeowner)
    {
        var parts = new[] { homeowner.FirstName, homeowner.MiddleName, homeowner.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(" ", parts);
    }

    private static string DisplayValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string FormatDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !DateTime.TryParse(value, out var parsed))
        {
            return "-";
        }

        return parsed.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
    }

    private static string FormatMonthYear(int month, int year) =>
        $"{GetMonthName(month)} {year}";

    private static string GetMonthName(int month)
    {
        if (month < 1 || month > 12)
        {
            return "-";
        }

        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);
    }

    private sealed class AttendanceMetric
    {
        public int HomeownerId { get; set; }
        public string EventDate { get; set; } = string.Empty;
    }

    private sealed class InteractionMetric
    {
        public int HomeownerId { get; set; }
        public string InteractionDate { get; set; } = string.Empty;
    }
}

public sealed class DashboardKpiSummary
{
    public int TotalHomeowners { get; init; }
    public int ActiveHomeowners { get; init; }
    public int TotalUnits { get; init; }
    public int TotalMSMEs { get; init; }
    public int ActiveMSMEs { get; init; }
    public int OpenViolations { get; init; }
    public int UnpaidOrOverdueDues { get; init; }
    public int PendingClearances { get; init; }
}

public sealed class HomeownerDashboardSummary
{
    public int HomeownerId { get; init; }
    public string HomeownerName { get; init; } = string.Empty;
    public double EngagementScore { get; init; }
    public string EngagementLabel { get; init; } = string.Empty;
    public string EngagementColor { get; init; } = string.Empty;
    public int UnpaidDuesCount { get; init; }
    public int OpenViolationCount { get; init; }
    public int PendingClearanceCount { get; init; }
}

public sealed class TrendPoint
{
    public string Label { get; init; } = string.Empty;
    public double Value { get; init; }
}

public sealed class StatusBreakdownItem
{
    public string Label { get; init; } = string.Empty;
    public int Value { get; init; }
}

public sealed class DuesCollectionSummary
{
    public int Month { get; init; }
    public int Year { get; init; }
    public int PaidCount { get; init; }
    public int UnpaidCount { get; init; }
    public int OverdueCount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal UnpaidAmount { get; init; }
    public decimal OverdueAmount { get; init; }
}
