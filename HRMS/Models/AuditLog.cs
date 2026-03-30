namespace HRMS.Models;

public class AuditLog
{
    public int AuditLogId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableAffected { get; set; } = string.Empty;
    public int? RecordId { get; set; }
    public string? Details { get; set; }
    public string Timestamp { get; set; } = string.Empty;

    public User User { get; set; } = null!;
}
