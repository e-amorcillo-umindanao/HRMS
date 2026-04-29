using System.ComponentModel.DataAnnotations;

namespace HRMS.Models;

public class DuesRecord
{
    [Key]
    public int DuesId { get; set; }
    public int SubdivisionId { get; set; }
    public int HomeownerId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    public string DueDate { get; set; } = string.Empty;
    public string? PaidDate { get; set; }
    public string Status { get; set; } = "Unpaid";
    public string CreatedAt { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    public Subdivision Subdivision { get; set; } = null!;
    public Homeowner Homeowner { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}
