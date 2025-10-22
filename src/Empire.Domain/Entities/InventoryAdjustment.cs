using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class InventoryAdjustment : BaseEntity
{
    [Required]
    public int InventoryId { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string AdjustmentType { get; set; } = string.Empty; // Received, Sold, Damaged, Lost, etc.
    
    [Required]
    public int Quantity { get; set; } // Can be positive or negative
    
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;
    
    public DateTime AdjustmentDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Inventory Inventory { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}

