using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class DocumentService
{
    private readonly AppDbContext _context;
    private readonly DuesService _duesService;

    public DocumentService(AppDbContext context, DuesService duesService)
    {
        _context = context;
        _duesService = duesService;
    }

    public async Task<GeneratedDocumentResult?> GenerateDuesStatementAsync(int homeownerId)
    {
        var homeowner = await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Unit)
            .SingleOrDefaultAsync(record => record.HomeownerId == homeownerId && !record.IsDeleted);

        if (homeowner is null)
        {
            return null;
        }

        var duesRecords = await _duesService.GetByHomeownerAsync(homeownerId);
        var settings = await _context.HOASettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        var fileName = BuildFileName(homeowner);
        var bytes = PdfExportHelper.GenerateDuesStatement(settings, homeowner, duesRecords);

        return new GeneratedDocumentResult
        {
            FileName = fileName,
            ContentType = "application/pdf",
            Bytes = bytes
        };
    }

    public async Task<GeneratedDocumentResult?> GenerateHoaClearanceAsync(int clearanceId)
    {
        var request = await _context.ClearanceRequests
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .ThenInclude(homeowner => homeowner.Unit)
            .FirstOrDefaultAsync(record => record.ClearanceId == clearanceId);

        if (request is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync();
        var fileName = BuildFileName(GetFullName(request.Homeowner), "hoa_clearance");
        var bytes = PdfExportHelper.GenerateHoaClearance(settings, request);

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateCertificateOfResidencyAsync(int homeownerId)
    {
        var homeowner = await GetHomeownerAsync(homeownerId);
        if (homeowner is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync();
        var fileName = BuildFileName(GetFullName(homeowner), "certificate_of_residency");
        var bytes = PdfExportHelper.GenerateCertificateOfResidency(settings, homeowner);

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateCertificateOfGoodStandingAsync(int homeownerId)
    {
        var homeowner = await GetHomeownerAsync(homeownerId);
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

        var settings = await GetSettingsAsync();
        var fileName = BuildFileName(GetFullName(homeowner), "certificate_of_good_standing");
        var bytes = PdfExportHelper.GenerateCertificateOfGoodStanding(settings, homeowner);

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateOfficialLetterAsync(int homeownerId, string? purpose = null)
    {
        var homeowner = await GetHomeownerAsync(homeownerId);
        if (homeowner is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync();
        var resolvedPurpose = string.IsNullOrWhiteSpace(purpose) ? "General Purpose" : purpose.Trim();
        var fileName = BuildFileName(GetFullName(homeowner), "official_letter");
        var bytes = PdfExportHelper.GenerateOfficialLetter(settings, homeowner, resolvedPurpose);

        return CreatePdfResult(fileName, bytes);
    }

    public async Task<GeneratedDocumentResult?> GenerateViolationReportAsync(int violationId)
    {
        var violation = await _context.ViolationRecords
            .AsNoTracking()
            .Include(record => record.Homeowner)
            .Include(record => record.FiledByUser)
            .Include(record => record.UpdatedByUser)
            .FirstOrDefaultAsync(record => record.ViolationId == violationId);

        if (violation is null)
        {
            return null;
        }

        var settings = await GetSettingsAsync();
        var fileName = $"{violation.ViolationNumber}_report.pdf";
        var bytes = PdfExportHelper.GenerateViolationReport(settings, violation);

        return CreatePdfResult(fileName, bytes);
    }

    private async Task<Homeowner?> GetHomeownerAsync(int homeownerId)
    {
        return await _context.Homeowners
            .AsNoTracking()
            .Include(record => record.Unit)
            .SingleOrDefaultAsync(record => record.HomeownerId == homeownerId && !record.IsDeleted);
    }

    private async Task<HOASettings?> GetSettingsAsync()
    {
        return await _context.HOASettings
            .AsNoTracking()
            .FirstOrDefaultAsync();
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
}

public sealed class GeneratedDocumentResult
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public byte[] Bytes { get; init; } = [];
}
