namespace Empire.Web.DTOs.Repair;

/// <summary>
/// Represents a single audit/activity record for a repair.
/// </summary>
public class RepairActivityDto
{
    public int Id { get; set; }
    public int RepairId { get; set; }

    /// <summary>Action type: Created | Updated | PartsUpdated | StatusChanged | etc.</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Human-readable summary of what changed.</summary>
    public string? Description { get; set; }

    /// <summary>JSON or text snapshot of values before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON or text snapshot of values after the change.</summary>
    public string? NewValues { get; set; }

    public int? PerformedByUserId { get; set; }
    public string PerformedByUserName { get; set; } = "System";

    public DateTime PerformedAt { get; set; }

    /// <summary>Pre-formatted display string (e.g. "May 22, 2026 03:45 PM UTC").</summary>
    public string PerformedAtFormatted { get; set; } = string.Empty;
}
