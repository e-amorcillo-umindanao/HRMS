namespace HRMS.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int? HomeownerId { get; set; }
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = string.Empty;
    public string? LastLoginAt { get; set; }

    public Role Role { get; set; } = null!;
    public Homeowner? Homeowner { get; set; }
}
