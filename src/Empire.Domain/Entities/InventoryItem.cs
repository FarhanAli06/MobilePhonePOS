using System.ComponentModel.DataAnnotations;
using Empire.Domain.Common;

namespace Empire.Domain.Entities;

public class InventoryItem : BaseEntity
{
    [Required]
    public int ShopId { get; set; }
    
    [Required]
    public int BrandId { get; set; }
    
    [Required]
    public int DeviceCategoryId { get; set; }
    
    [Required]
    public int DeviceModelId { get; set; }
    
    [Required]
    public int InventoryCategoryId { get; set; }
    
    // Item lookup (e.g., Screen, Battery, Charging Port)
    public int? ItemId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(50)]
    public string SKU { get; set; } = string.Empty;
    
    public int CurrentStock { get; set; } = 0;
    
    public int ReorderPoint { get; set; } = 5;
    
    public decimal CostPrice { get; set; } = 0;
    
    public decimal RetailPrice { get; set; } = 0;
    
    public decimal WholesalePrice { get; set; } = 0;
    
    public bool EnableLowStockNotifications { get; set; } = true;
    
    public bool IsActive { get; set; } = true;
    
    [MaxLength(1000)]
    public string Notes { get; set; } = string.Empty;
    
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Shop Shop { get; set; } = null!;
    public virtual Brand Brand { get; set; } = null!;
    public virtual DeviceCategory DeviceCategory { get; set; } = null!;
    public virtual DeviceModel DeviceModel { get; set; } = null!;
    public virtual InventoryCategory InventoryCategory { get; set; } = null!;
    public virtual Item? Item { get; set; }
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public virtual ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
    
    /// <summary>
    /// Minimum stock level for low stock alerts
    /// </summary>
    public int MinStockLevel { get; set; } = 5;
    
    /// <summary>
    /// Quantity to reorder when stock is low
    /// </summary>
    public int ReorderQuantity { get; set; } = 10;
}

