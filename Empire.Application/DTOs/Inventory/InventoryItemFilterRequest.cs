namespace Empire.Application.DTOs.Inventory;

public class InventoryItemFilterRequest
{
    public int ShopId { get; set; }
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public int? InventoryCategoryId { get; set; }
    public string? SearchTerm { get; set; }
    public bool? IsActive { get; set; }
    public bool? LowStockOnly { get; set; }
}
