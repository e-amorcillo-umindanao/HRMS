namespace HRMS.Models;

public class Homeowner
{
    public int HomeownerId { get; set; }
    public int SubdivisionId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string BirthDate { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string? CivilStatus { get; set; }
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public int? PhaseId { get; set; }
    public int? UnitId { get; set; }
    public string Status { get; set; } = "Active";
    public string? Categories { get; set; }
    public string ResidencySince { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    public Subdivision Subdivision { get; set; } = null!;
    public Phase? Phase { get; set; }
    public Unit? Unit { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
