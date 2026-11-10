using System.ComponentModel.DataAnnotations;

namespace Empire.Domain.Entities;

public class DeviceActivity
{
    public int Id { get; set; }
    public int DeviceId { get; set; }
    [MaxLength(50)] public string Action { get; set; } = string.Empty;
    [MaxLength(500)] public string Description { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public int? PerformedByUserId { get; set; }
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public virtual Device Device { get; set; } = null!;
    public virtual User? PerformedByUser { get; set; }
}
