using System.ComponentModel.DataAnnotations;

namespace HRMS.Models;

public class ClearanceRequest
{
    [Key]
    public int ClearanceId { get; set; }
    public int HomeownerId { get; set; }
    public string ClearanceType { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string RequestedAt { get; set; } = string.Empty;
    public string? ProcessedAt { get; set; }
    public int? ProcessedBy { get; set; }
    public string? Remarks { get; set; }
    public string? ValidUntil { get; set; }

    public Homeowner Homeowner { get; set; } = null!;
    public User? ProcessedByUser { get; set; }
}
