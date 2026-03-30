namespace HRMS.Models;

public class Attendance
{
    public int AttendanceId { get; set; }
    public int EventId { get; set; }
    public int HomeownerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RecordedAt { get; set; } = string.Empty;
    public int RecordedBy { get; set; }

    public Event Event { get; set; } = null!;
    public Homeowner Homeowner { get; set; } = null!;
    public User RecordedByUser { get; set; } = null!;
}
