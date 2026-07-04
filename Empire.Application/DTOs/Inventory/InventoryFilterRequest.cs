using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Inventory;

public class InventoryFilterRequest
{
    public int ShopId { get; set; }
    
    public DeviceType? DeviceType { get; set; } // Filter by device type
    
    public string? Category { get; set; } // Filter by category
    
    public bool? LowStockOnly { get; set; } // Show only low stock items
    
    public bool? LowStockNotificationOnly { get; set; } // Show only items with low stock notifications enabled
    
    public string? SearchTerm { get; set; } // Search by name, brand, model, etc.
}

