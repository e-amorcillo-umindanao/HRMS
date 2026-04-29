namespace HRMS.Models;

public class Unit
{
    public int UnitId { get; set; }
    public int SubdivisionId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int? PhaseId { get; set; }
    public int? HeadHomeownerId { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    public Subdivision Subdivision { get; set; } = null!;
    public Phase? Phase { get; set; }
    public Homeowner? HeadHomeowner { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Homeowner> Homeowners { get; set; } = new List<Homeowner>();
}
