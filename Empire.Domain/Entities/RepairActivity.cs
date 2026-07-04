using System.ComponentModel.DataAnnotations;

namespace Empire.Domain.Entities;

/// <summary>
/// Tracks every create/update action performed on a repair for full audit history.
/// </summary>
public class RepairActivity
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int RepairId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // e.g. "Created", "StatusChanged", "PartAdded", "CostUpdated", "PaymentUpdated"

    [MaxLength(2000)]
    public string? Description { get; set; } // Human-readable summary of what changed

    [MaxLength(2000)]
    public string? OldValues { get; set; } // JSON snapshot of changed fields before

    [MaxLength(2000)]
    public string? NewValues { get; set; } // JSON snapshot of changed fields after

    public int? PerformedByUserId { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Repair Repair { get; set; } = null!;
    public virtual User? PerformedByUser { get; set; }
}
