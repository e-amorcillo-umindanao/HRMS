namespace HRMS.Models;

public class InteractionLog
{
    public int InteractionLogId { get; set; }
    public int SubdivisionId { get; set; }
    public int? HomeownerId { get; set; }
    public int? MSMEId { get; set; }
    public string InteractionType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string InteractionDate { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    public Subdivision Subdivision { get; set; } = null!;
    public Homeowner? Homeowner { get; set; }
    public MSME? MSME { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
