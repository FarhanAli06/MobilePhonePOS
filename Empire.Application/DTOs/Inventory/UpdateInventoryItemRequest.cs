using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Inventory;

public class UpdateInventoryItemRequest
{
    public int? BrandId { get; set; }
    public int? DeviceCategoryId { get; set; }
    public int? DeviceModelId { get; set; }
    public int? InventoryCategoryId { get; set; }
    public int? ItemId { get; set; }
    public int? CategoryId { get; set; }

    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? SKU { get; set; }

    public int? ReorderPoint { get; set; }

    public decimal? CostPrice { get; set; }
    public decimal? RetailPrice { get; set; }
    public decimal? WholesalePrice { get; set; }

    public bool? EnableLowStockNotifications { get; set; }
    public bool? IsActive { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
