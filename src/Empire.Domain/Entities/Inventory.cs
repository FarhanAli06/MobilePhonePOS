using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;
using Empire.Domain.Enums;

namespace Empire.Domain.Entities;

public class Inventory : BaseEntity
{
    [Required]
    public int ShopId { get; set; }
    
    public int? DeviceId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public DeviceType DeviceType { get; set; } // New field
    
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    public int Stock { get; set; }
    
    [Required]
    public int ReorderPoint { get; set; }
    
    public int MinimumStock { get; set; }
    
    [Required]
    public decimal CostPrice { get; set; }
    
    [Required]
    public decimal RetailPrice { get; set; }
    
    public bool LowStockNotification { get; set; } = true;
    
    // Navigation properties
    public virtual Shop Shop { get; set; } = null!;
    public virtual Device? Device { get; set; }
    public virtual ICollection<InventoryAdjustment> Adjustments { get; set; } = new List<InventoryAdjustment>();
}

