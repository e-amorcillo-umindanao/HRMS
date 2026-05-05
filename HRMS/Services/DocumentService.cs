using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class DocumentService
{
    private readonly AppDbContext _context;
    private readonly DuesService _duesService;
    private readonly AuditService _auditService;

    public DocumentService(AppDbContext context, DuesService duesService, AuditService auditService)
    {
        _context = context;
        _duesService = duesService;
        _auditService = auditService;
    }

    public async Task<GeneratedDocumentResult?> GenerateDuesStatementAsync(int homeownerId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "documents", "You do not have permission to generate documents.");
        var actorSubdivisionId = await ResolveActorSubdivisionIdAsync(actorUserId);
        var homeowner = await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Unit)
            .SingleOrDefaultAsync(record =>
                record.HomeownerId == homeownerId &&
                !record.IsDeleted &&
                record.SubdivisionId == actorSubdivisionId);

        if (homeowner is null)
        {
            return null;
        }

        var duesRecords = await _duesService.GetByHomeownerAsync(homeownerId, homeowner.SubdivisionId);
        var settings = await GetSettingsAsync(homeowner.SubdivisionId);

        var fileName = BuildFileName(homeowner);
        var bytes = PdfExportHelper.GenerateDuesStatement(settings, homeowner, duesRecords);
        await _auditService.LogAsync(actorUserId, "Generate", "Documents", homeowner.HomeownerId, $"Generated dues statement for '{GetFullName(homeowner)}'.");

        return new GeneratedDocumentResult
        {
            FileName = fileName,
            ContentType = "application/pdf",
            Bytes = bytes
        };
    }

    public async Task<GeneratedDocumentResult?> GenerateHoaClearanceAsync(int clearanceId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "documents", "You do not have permission to generate documents.");
        var actorSubdivisionId = await ResolveActorSubdivisionIdAsync(actorUserId);

        var request = await _context.ClearanceRequests
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .ThenInclude(homeowner => homeowner.Unit)
            .FirstOrDefaultAsync(record => record.ClearanceId == clearanceId && record.SubdivisionId == actorSubdivisionId);

        if (request is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync(request.SubdivisionId);
        var fileName = BuildFileName(GetFullName(request.Homeowner), "hoa_clearance");
        var bytes = PdfExportHelper.GenerateHoaClearance(settings, request);
        await _auditService.LogAsync(actorUserId, "Generate", "Documents", request.ClearanceId, $"Generated HOA clearance for '{GetFullName(request.Homeowner)}'.");

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateCertificateOfResidencyAsync(int homeownerId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "documents", "You do not have permission to generate documents.");

        var homeowner = await GetHomeownerAsync(homeownerId, actorUserId);
        if (homeowner is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync(homeowner.SubdivisionId);
        var fileName = BuildFileName(GetFullName(homeowner), "certificate_of_residency");
        var bytes = PdfExportHelper.GenerateCertificateOfResidency(settings, homeowner);
        await _auditService.LogAsync(actorUserId, "Generate", "Documents", homeowner.HomeownerId, $"Generated certificate of residency for '{GetFullName(homeowner)}'.");

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateCertificateOfGoodStandingAsync(int homeownerId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "documents", "You do not have permission to generate documents.");

        var homeowner = await GetHomeownerAsync(homeownerId, actorUserId);
        if (homeowner is null)
        {
            return null;
        }

        var hasOutstandingDues = await _context.DuesRecords.AnyAsync(record =>
            record.HomeownerId == homeownerId &&
            (record.Status == "Unpaid" || record.Status == "Overdue"));

        var hasOutstandingViolations = await _context.ViolationRecords.AnyAsync(record =>
            record.HomeownerId == homeownerId &&
            record.Status != "Resolved" &&
            record.Status != "Closed");

        if (hasOutstandingDues || hasOutstandingViolations)
        {
            throw new InvalidOperationException("This homeowner is not currently in good standing.");
        }

        var settings = await GetSettingsAsync(homeowner.SubdivisionId);
        var fileName = BuildFileName(GetFullName(homeowner), "certificate_of_good_standing");
        var bytes = PdfExportHelper.GenerateCertificateOfGoodStanding(settings, homeowner);
        await _auditService.LogAsync(actorUserId, "Generate", "Documents", homeowner.HomeownerId, $"Generated certificate of good standing for '{GetFullName(homeowner)}'.");

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateOfficialLetterAsync(int homeownerId, int actorUserId, string? purpose = null)
    {
        await EnsureCanWriteAsync(actorUserId, "documents", "You do not have permission to generate documents.");

        var homeowner = await GetHomeownerAsync(homeownerId, actorUserId);
        if (homeowner is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync(homeowner.SubdivisionId);
        var resolvedPurpose = string.IsNullOrWhiteSpace(purpose) ? "General Purpose" : purpose.Trim();
        var fileName = BuildFileName(GetFullName(homeowner), "official_letter");
        var bytes = PdfExportHelper.GenerateOfficialLetter(settings, homeowner, resolvedPurpose);
        await _auditService.LogAsync(actorUserId, "Generate", "Documents", homeowner.HomeownerId, $"Generated official letter for '{GetFullName(homeowner)}'.");

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateViolationReportAsync(int violationId, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "violation-pdf", "You do not have permission to generate violation PDF reports.");
        var actorSubdivisionId = await ResolveActorSubdivisionIdAsync(actorUserId);

        var violation = await _context.ViolationRecords
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Include(record => record.FiledByUser)
            .Include(record => record.UpdatedByUser)
            .FirstOrDefaultAsync(record => record.ViolationId == violationId && record.SubdivisionId == actorSubdivisionId);

        if (violation is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync(violation.SubdivisionId);
        var fileName = $"{violation.ViolationNumber}_report.pdf";
        var bytes = PdfExportHelper.GenerateViolationReport(settings, violation);
        await _auditService.LogAsync(actorUserId, "Generate", "Documents", violation.ViolationId, $"Generated violation report '{violation.ViolationNumber}'.");

        return CreatePdfResult(fileName, bytes);
    }

    private async Task<Homeowner?> GetHomeownerAsync(int homeownerId, int actorUserId)
    {
        var actorSubdivisionId = await ResolveActorSubdivisionIdAsync(actorUserId);
        return await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Unit)
            .SingleOrDefaultAsync(record =>
                record.HomeownerId == homeownerId &&
                !record.IsDeleted &&
                record.SubdivisionId == actorSubdivisionId);
    }

    private async Task<HOASettings?> GetSettingsAsync(int subdivisionId)
    {
        return await _context.HOASettings
            .AsNoTracking()
            .FirstOrDefaultAsync(record => record.SubdivisionId == subdivisionId);
    }

    private static GeneratedDocumentResult CreatePdfResult(string fileName, byte[] bytes)
    {
        return new GeneratedDocumentResult
        {
            FileName = fileName,
            ContentType = "application/pdf",
            Bytes = bytes
        };
    }

    private static string BuildFileName(Homeowner homeowner)
    {
        return BuildFileName(GetFullName(homeowner), "dues_statement");
    }

    private static string BuildFileName(string fullName, string suffix)
    {
        var sanitized = string.Join("_", fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return $"{sanitized}_{suffix}.pdf";
    }

    private static string GetFullName(Homeowner homeowner)
    {
        var parts = new[] { homeowner.FirstName, homeowner.MiddleName, homeowner.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return string.Join(" ", parts);
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

    private async Task<int> ResolveActorSubdivisionIdAsync(int actorUserId)
    {
        var actorSubdivisionId = await _context.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.SubdivisionId)
            .SingleOrDefaultAsync();

        if (!actorSubdivisionId.HasValue)
        {
            throw new UnauthorizedAccessException("Your account is not assigned to a subdivision.");
        }

        return actorSubdivisionId.Value;
    }
}

public sealed class GeneratedDocumentResult
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public byte[] Bytes { get; init; } = [];
}
