using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

/// <summary>Records exactly which FIFO batch supplied a repair part.</summary>
public class RepairPartBatchAllocation : BaseEntity
{
    [Required] public int RepairPartId { get; set; }
    [Required] public int InventoryBatchId { get; set; }
    [Required] public int Quantity { get; set; }
    [Required] public decimal UnitCost { get; set; }

    public virtual RepairPart RepairPart { get; set; } = null!;
    public virtual InventoryBatch InventoryBatch { get; set; } = null!;
}
