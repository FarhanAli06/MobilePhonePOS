using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>A single received shipment of one inventory item.</summary>
public class InventoryBatch : BaseEntity
{
    [Required] public int InventoryItemId { get; set; }
    [Required] public int ReceivedQuantity { get; set; }
    [Required] public int RemainingQuantity { get; set; }
    [Required] public decimal UnitCost { get; set; }
    [MaxLength(100)] public string Vendor { get; set; } = string.Empty;
    [MaxLength(50)] public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
    [MaxLength(500)] public string Notes { get; set; } = string.Empty;

    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual ICollection<RepairPartBatchAllocation> RepairAllocations { get; set; } = new List<RepairPartBatchAllocation>();
}
