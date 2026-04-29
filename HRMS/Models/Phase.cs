namespace HRMS.Models;

public class Phase
{
    public int PhaseId { get; set; }
    public int SubdivisionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CreatedAt { get; set; } = string.Empty;

    public Subdivision Subdivision { get; set; } = null!;
    public ICollection<Homeowner> Homeowners { get; set; } = new List<Homeowner>();
    public ICollection<Unit> Units { get; set; } = new List<Unit>();
}
