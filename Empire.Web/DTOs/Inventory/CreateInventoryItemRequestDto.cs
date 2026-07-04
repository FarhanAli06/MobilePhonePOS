namespace Empire.Web.DTOs.Inventory;

public class CreateInventoryItemRequestDto
{
    public int? ShopId { get; set; }
    public int? ItemId { get; set; }
    public int? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public int? InventoryCategoryId { get; set; }
    public int? CurrentStock { get; set; }
    public int? ReorderPoint { get; set; }
    public decimal CostPrice { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public bool EnableLowStockNotifications { get; set; }
    public bool IsActive { get; set; }
    public string Notes { get; set; } = string.Empty;
}
