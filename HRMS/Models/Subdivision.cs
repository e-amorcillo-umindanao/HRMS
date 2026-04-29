namespace HRMS.Models;

public class Subdivision
{
    public int SubdivisionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? ContactNumber { get; set; }
    public string Status { get; set; } = "Active";
    public string SubscriptionStart { get; set; } = string.Empty;
    public string? SubscriptionEnd { get; set; }
    public string? Notes { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Homeowner> Homeowners { get; set; } = new List<Homeowner>();
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public ICollection<Phase> Phases { get; set; } = new List<Phase>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<MSME> MSMEs { get; set; } = new List<MSME>();
    public ICollection<DuesRecord> DuesRecords { get; set; } = new List<DuesRecord>();
    public ICollection<ViolationRecord> ViolationRecords { get; set; } = new List<ViolationRecord>();
    public ICollection<ClearanceRequest> ClearanceRequests { get; set; } = new List<ClearanceRequest>();
    public ICollection<InteractionLog> InteractionLogs { get; set; } = new List<InteractionLog>();
    public ICollection<HOASettings> HOASettings { get; set; } = new List<HOASettings>();
}
