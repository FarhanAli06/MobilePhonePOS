using System.ComponentModel.DataAnnotations;

namespace Empire.Application.DTOs.Inventory;

public class CreateInventoryItemRequest
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

    public int? ItemId { get; set; }

    public int? CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? SKU { get; set; }

    public int CurrentStock { get; set; } = 0;

    public int ReorderPoint { get; set; } = 5;

    public decimal CostPrice { get; set; } = 0;

    public decimal RetailPrice { get; set; } = 0;

    public decimal WholesalePrice { get; set; } = 0;

    public bool EnableLowStockNotifications { get; set; } = true;

    public bool IsActive { get; set; } = true;

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
