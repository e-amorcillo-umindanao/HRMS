namespace HRMS.Models;

public class MSME
{
    public int MSMEId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string BusinessType { get; set; } = string.Empty;
    public int HomeownerId { get; set; }
    public int? UnitId { get; set; }
    public string? ContactNumber { get; set; }
    public string? Description { get; set; }
    public string RegistrationDate { get; set; } = string.Empty;
    public string? ExpiryDate { get; set; }
    public string Status { get; set; } = "Active";
    public string CreatedAt { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    public Homeowner Homeowner { get; set; } = null!;
    public Unit? Unit { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<InteractionLog> InteractionLogs { get; set; } = new List<InteractionLog>();
}
