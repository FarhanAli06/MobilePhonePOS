using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class StockMovement : BaseEntity
{
    [Required]
    public int InventoryItemId { get; set; }
    
    [Required]
    public int CreatedBy { get; set; }
    
    [Required]
    [MaxLength(20)]
    public string MovementType { get; set; } = string.Empty; // IN, OUT, ADJUSTMENT, TRANSFER
    
    public int Quantity { get; set; }
    
    public int PreviousStock { get; set; }
    
    public int NewStock { get; set; }
    
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string ReferenceNumber { get; set; } = string.Empty; // Sale ID, PO Number, etc.
    
    public decimal UnitCost { get; set; } = 0;
    
    public decimal TotalCost { get; set; } = 0;
    
    public DateTime MovementDate { get; set; }
    
    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
    
    // Navigation properties
    public virtual InventoryItem InventoryItem { get; set; } = null!;
    public virtual User CreatedByUser { get; set; } = null!;
}

