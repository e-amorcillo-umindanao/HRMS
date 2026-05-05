using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class HomeownerService
{
    private readonly AppDbContext _context;
    private readonly AuditService _auditService;

    public HomeownerService(AppDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<List<Homeowner>> GetAllAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .OrderBy(h => h.LastName)
            .ThenBy(h => h.FirstName)
            .ToListAsync();
    }

    public async Task<Homeowner?> GetByIdAsync(int id, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId, includeCreator: true)
            .SingleOrDefaultAsync(h => h.HomeownerId == id);
    }

    public async Task<List<Homeowner>> SearchAsync(int? subdivisionId, string? name, string? status, int? phaseId, int? unitId, string? category)
    {
        var query = BaseQuery(subdivisionId);

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(h =>
                h.FirstName.Contains(name) ||
                h.LastName.Contains(name) ||
                (h.MiddleName != null && h.MiddleName.Contains(name)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(h => h.Status == status);
        }

        if (phaseId.HasValue)
        {
            query = query.Where(h => h.PhaseId == phaseId);
        }

        if (unitId.HasValue)
        {
            query = query.Where(h => h.UnitId == unitId);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(h => h.Categories != null && h.Categories.Contains(category));
        }

        return await query
            .OrderBy(h => h.LastName)
            .ThenBy(h => h.FirstName)
            .ToListAsync();
    }

    public Task<List<Homeowner>> SearchAsync(string? name, string? status, int? phaseId, int? unitId, string? category) =>
        SearchAsync(null, name, status, phaseId, unitId, category);

    public async Task<Homeowner> AddAsync(Homeowner homeowner, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "homeowners", "You do not have write access to the Homeowners module.");
        homeowner.SubdivisionId = await ResolveSubdivisionIdAsync(homeowner.SubdivisionId, actorUserId);
        await EnsureActorCanAccessSubdivisionAsync(homeowner.SubdivisionId, actorUserId);
        await EnsureAssignmentsBelongToSubdivisionAsync(homeowner.PhaseId, homeowner.UnitId, homeowner.SubdivisionId);
        homeowner.CreatedBy = actorUserId;
        homeowner.CreatedAt = DateTime.UtcNow.ToString("o");
        homeowner.Status = string.IsNullOrWhiteSpace(homeowner.Status) ? "Active" : homeowner.Status;
        homeowner.Categories = NormalizeCategories(homeowner.Categories);

        _context.Homeowners.Add(homeowner);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Create", "Homeowners", homeowner.HomeownerId, $"Created homeowner '{GetFullName(homeowner)}'.");

        return homeowner;
    }

    public async Task<Homeowner?> UpdateAsync(Homeowner homeowner, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "homeowners", "You do not have write access to the Homeowners module.");
        var existing = await _context.Homeowners
            .SingleOrDefaultAsync(h => h.HomeownerId == homeowner.HomeownerId && !h.IsDeleted);

        if (existing is null)
        {
            return null;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);
        var targetSubdivisionId = homeowner.SubdivisionId == 0 ? existing.SubdivisionId : homeowner.SubdivisionId;
        await EnsureActorCanAccessSubdivisionAsync(targetSubdivisionId, actorUserId);
        await EnsureAssignmentsBelongToSubdivisionAsync(homeowner.PhaseId, homeowner.UnitId, targetSubdivisionId);

        existing.FirstName = homeowner.FirstName;
        existing.MiddleName = homeowner.MiddleName;
        existing.LastName = homeowner.LastName;
        existing.BirthDate = homeowner.BirthDate;
        existing.Gender = homeowner.Gender;
        existing.CivilStatus = homeowner.CivilStatus;
        existing.ContactNumber = homeowner.ContactNumber;
        existing.Email = homeowner.Email;
        existing.Address = homeowner.Address;
        existing.SubdivisionId = targetSubdivisionId;
        existing.PhaseId = homeowner.PhaseId;
        existing.UnitId = homeowner.UnitId;
        existing.Status = string.IsNullOrWhiteSpace(homeowner.Status) ? "Active" : homeowner.Status;
        existing.Categories = NormalizeCategories(homeowner.Categories);
        existing.ResidencySince = homeowner.ResidencySince;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Update", "Homeowners", existing.HomeownerId, $"Updated homeowner '{GetFullName(existing)}'.");

        return existing;
    }

    public async Task<bool> SoftDeleteAsync(int id, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "homeowners", "You do not have write access to the Homeowners module.");
        var existing = await _context.Homeowners
            .SingleOrDefaultAsync(h => h.HomeownerId == id && !h.IsDeleted);

        if (existing is null)
        {
            return false;
        }

        await EnsureActorCanAccessSubdivisionAsync(existing.SubdivisionId, actorUserId);

        existing.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(actorUserId, "Delete", "Homeowners", existing.HomeownerId, $"Soft-deleted homeowner '{GetFullName(existing)}'.");

        return true;
    }

    public async Task<HomeownerImportResult> ImportFromCsvAsync(Stream csvStream, int actorUserId)
    {
        await EnsureCanWriteAsync(actorUserId, "homeowners", "You do not have write access to the Homeowners module.");
        var parseResult = await CsvImportHelper.ParseHomeownersAsync(csvStream);
        var importResult = new HomeownerImportResult();
        importResult.Errors.AddRange(parseResult.Errors);

        foreach (var record in parseResult.Records)
        {
            var validationError = TryMapCsvRecord(record, actorUserId, out var homeowner);
            if (validationError is not null)
            {
                importResult.Errors.Add(validationError);
                continue;
            }

            try
            {
                await AddAsync(homeowner!, actorUserId);
                importResult.ImportedCount++;
            }
            catch (Exception exception)
            {
                importResult.Errors.Add(new CsvImportError
                {
                    RowNumber = record.RowNumber,
                    Message = exception.Message
                });
            }
        }

        return importResult;
    }

    public async Task<List<Homeowner>> GetByUnitAsync(int unitId, int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(h => h.UnitId == unitId)
            .OrderBy(h => h.LastName)
            .ThenBy(h => h.FirstName)
            .ToListAsync();
    }

    public async Task<List<Homeowner>> GetAllActiveAsync(int? subdivisionId = null)
    {
        return await BaseQuery(subdivisionId)
            .Where(h => h.Status == "Active")
            .OrderBy(h => h.LastName)
            .ThenBy(h => h.FirstName)
            .ToListAsync();
    }

    private IQueryable<Homeowner> BaseQuery(int? subdivisionId, bool includeCreator = false)
    {
        var query = _context.Homeowners
            .AsNoTracking()
            .Include(h => h.Subdivision)
            .Include(h => h.Phase)
            .Include(h => h.Unit)
            .Where(h => !h.IsDeleted);

        if (subdivisionId.HasValue)
        {
            query = query.Where(h => h.SubdivisionId == subdivisionId.Value);
        }

        if (includeCreator)
        {
            query = query.Include(h => h.CreatedByUser);
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

        throw new InvalidOperationException("Subdivision is required for homeowner records.");
    }

    private async Task EnsureAssignmentsBelongToSubdivisionAsync(int? phaseId, int? unitId, int subdivisionId)
    {
        if (phaseId.HasValue)
        {
            var phaseSubdivisionId = await _context.Phases
                .AsNoTracking()
                .Where(phase => phase.PhaseId == phaseId.Value)
                .Select(phase => (int?)phase.SubdivisionId)
                .SingleOrDefaultAsync();

            if (!phaseSubdivisionId.HasValue)
            {
                throw new InvalidOperationException("The selected phase could not be found.");
            }

            if (phaseSubdivisionId.Value != subdivisionId)
            {
                throw new InvalidOperationException("The selected phase does not belong to this subdivision.");
            }
        }

        if (unitId.HasValue)
        {
            var unitSubdivisionId = await _context.Units
                .AsNoTracking()
                .Where(unit => unit.UnitId == unitId.Value)
                .Select(unit => (int?)unit.SubdivisionId)
                .SingleOrDefaultAsync();

            if (!unitSubdivisionId.HasValue)
            {
                throw new InvalidOperationException("The selected unit could not be found.");
            }

            if (unitSubdivisionId.Value != subdivisionId)
            {
                throw new InvalidOperationException("The selected unit does not belong to this subdivision.");
            }
        }
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
            throw new UnauthorizedAccessException("You cannot manage homeowners outside your assigned subdivision.");
        }
    }

    private static CsvImportError? TryMapCsvRecord(HomeownerCsvRecord record, int actorUserId, out Homeowner? homeowner)
    {
        homeowner = null;

        if (string.IsNullOrWhiteSpace(record.FirstName) || string.IsNullOrWhiteSpace(record.LastName))
        {
            return new CsvImportError { RowNumber = record.RowNumber, Message = "FirstName and LastName are required." };
        }

        if (string.IsNullOrWhiteSpace(record.Gender))
        {
            return new CsvImportError { RowNumber = record.RowNumber, Message = "Gender is required." };
        }

        if (!DateTime.TryParse(record.BirthDate, out var birthDate))
        {
            return new CsvImportError { RowNumber = record.RowNumber, Message = $"Invalid BirthDate '{record.BirthDate}'." };
        }

        if (!DateTime.TryParse(record.ResidencySince, out var residencySince))
        {
            return new CsvImportError { RowNumber = record.RowNumber, Message = $"Invalid ResidencySince '{record.ResidencySince}'." };
        }

        homeowner = new Homeowner
        {
            FirstName = record.FirstName.Trim(),
            MiddleName = NormalizeOptional(record.MiddleName),
            LastName = record.LastName.Trim(),
            BirthDate = ToIsoDateString(birthDate),
            Gender = record.Gender.Trim(),
            ContactNumber = NormalizeOptional(record.ContactNumber),
            Email = NormalizeOptional(record.Email),
            Status = string.IsNullOrWhiteSpace(record.Status) ? "Active" : record.Status.Trim(),
            ResidencySince = ToIsoDateString(residencySince),
            CreatedBy = actorUserId
        };

        return null;
    }

    private static string GetFullName(Homeowner homeowner)
    {
        var middle = string.IsNullOrWhiteSpace(homeowner.MiddleName) ? string.Empty : $"{homeowner.MiddleName} ";
        return $"{homeowner.FirstName} {middle}{homeowner.LastName}".Replace("  ", " ").Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeCategories(string? categories)
    {
        if (string.IsNullOrWhiteSpace(categories))
        {
            return null;
        }

        var values = categories
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return values.Length == 0 ? null : string.Join(",", values);
    }

    private static string ToIsoDateString(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Utc).ToString("o");
}
