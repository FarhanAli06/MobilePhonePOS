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
    
    [MaxLength(200)]
    public string? Notes { get; set; }
    
    // Navigation properties
    public virtual Repair Repair { get; set; } = null!;
    public virtual InventoryItem InventoryItem { get; set; } = null!;
}
