using System.ComponentModel.DataAnnotations;
using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Inventory;

public class CreateInventoryRequest
{
    [Required]
    public int ShopId { get; set; }
    
    public int? DeviceId { get; set; } // Optional for Phone/Laptop, null for Part/Accessories
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public DeviceType DeviceType { get; set; } // New field for categorization
    
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;
    
    [Required]
    public int Stock { get; set; }
    
    [Required]
    public int ReorderPoint { get; set; }
    
    [Required]
    public decimal CostPrice { get; set; }
    
    [Required]
    public decimal RetailPrice { get; set; }
    
    public bool LowStockNotification { get; set; } = true;
}

