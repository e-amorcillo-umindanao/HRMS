using System.ComponentModel.DataAnnotations;

namespace HRMS.Models;

public class ViolationRecord
{
    [Key]
    public int ViolationId { get; set; }
    public string ViolationNumber { get; set; } = string.Empty;
    public int? HomeownerId { get; set; }
    public string HomeownerName { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string ViolationDate { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public string? Resolution { get; set; }
    public string FiledAt { get; set; } = string.Empty;
    public int FiledBy { get; set; }
    public string? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public Homeowner? Homeowner { get; set; }
    public User FiledByUser { get; set; } = null!;
    public User? UpdatedByUser { get; set; }
}
