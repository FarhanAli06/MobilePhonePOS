namespace Empire.Application.DTOs.Repair;

public class RepairActivityDto
{
    public int Id { get; set; }
    public int RepairId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public int? PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = "Unknown";
    public DateTime PerformedAt { get; set; }
    public string PerformedAtFormatted { get; set; } = string.Empty;
}
