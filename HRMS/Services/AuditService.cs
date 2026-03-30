using HRMS.Data;
using HRMS.Models;

namespace HRMS.Services;

public class AuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int userId, string action, string tableAffected, int? recordId = null, string? details = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            TableAffected = tableAffected,
            RecordId = recordId,
            Details = details,
            Timestamp = DateTime.UtcNow.ToString("o")
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
