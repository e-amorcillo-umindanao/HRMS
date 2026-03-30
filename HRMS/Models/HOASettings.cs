using System.ComponentModel.DataAnnotations;

namespace HRMS.Models;

public class HOASettings
{
    [Key]
    public int SettingId { get; set; } = 1;
    public string HOAName { get; set; } = string.Empty;
    public string Subdivision { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string? PresidentName { get; set; }
    public string? SecretaryName { get; set; }
    public string? TreasurerName { get; set; }
    public string? ContactNumber { get; set; }
    public string? LogoPath { get; set; }
    public string UpdatedAt { get; set; } = string.Empty;
    public int UpdatedBy { get; set; }

    public User UpdatedByUser { get; set; } = null!;
}
