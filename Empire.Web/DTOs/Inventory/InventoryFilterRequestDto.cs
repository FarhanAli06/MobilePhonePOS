using Empire.Web.DTOs.DeviceType;

namespace Empire.Web.DTOs.Inventory;

public class InventoryFilterRequestDto
{
    public int ShopId { get; set; }
    
    public DeviceTypeDto? DeviceType { get; set; } // Filter by device type
    
    public string? Category { get; set; } // Filter by category
    
    public bool? LowStockOnly { get; set; } // Show only low stock items
    
    public bool? LowStockNotificationOnly { get; set; } // Show only items with low stock notifications enabled
    
    public string? SearchTerm { get; set; } // Search by name, brand, model, etc.
}

