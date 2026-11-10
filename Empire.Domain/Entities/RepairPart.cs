using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class RepairPart : BaseEntity
{
    [Required]
    public int RepairId { get; set; }
    
    [Required]
    public int InventoryItemId { get; set; }
    
    [Required]
    public int Quantity { get; set; } = 1;
    
    [Required]
    public decimal UnitPrice { get; set; }
    
    public decimal TotalPrice => Quantity * UnitPrice;

    /// <summary>Weighted actual FIFO cost of the physical parts used.</summary>
    public decimal ActualUnitCost { get; set; }

    /// <summary>
    /// True only after this repair part has been physically allocated from inventory.
    /// A false value represents a part requested for an out-of-stock repair.
    /// </summary>
    public bool IsAllocatedFromStock { get; set; } = true;

    public DateTime? StockAllocatedDate { get; set; }
    
    [MaxLength(200)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Repair Repair { get; set; } = null!;
    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual ICollection<RepairPartBatchAllocation> BatchAllocations { get; set; } = new List<RepairPartBatchAllocation>();
}
