using Empire.Domain.Enums;

namespace Empire.Application.DTOs.Inventory;

public class InventoryDto
{
    public int Id { get; set; }
    public int ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public int? DeviceId { get; set; }
    public string? DeviceBrand { get; set; }
    public string? DeviceCategory { get; set; }
    public string? DeviceModel { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DeviceType DeviceType { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }
    public int ReorderPoint { get; set; }
    public int? MinimumStock { get; set; }
    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public bool LowStockNotification { get; set; }
    public bool IsLowStock => Stock <= ReorderPoint;
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
}

