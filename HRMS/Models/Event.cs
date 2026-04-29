namespace HRMS.Models;

public class Event
{
    public int EventId { get; set; }
    public int SubdivisionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EventDate { get; set; } = string.Empty;
    public string? Venue { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    public Subdivision Subdivision { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
