using Empire.Application.DTOs.Inventory;

public interface IInventoryItemService
{
    Task<IEnumerable<InventoryItemDto>> GetInventoryItemsAsync(InventoryItemFilterRequest filter);
    Task<InventoryItemDto?> GetInventoryItemByIdAsync(int id, int shopId);
    Task<InventoryItemDto> CreateInventoryItemAsync(CreateInventoryItemRequest request);
    Task<InventoryItemDto?> UpdateInventoryItemAsync(int id, UpdateInventoryItemRequest request, int shopId);
    Task<bool> DeleteInventoryItemAsync(int id, int shopId);
    Task<string> GenerateSkuAsync(int brandId, int deviceCategoryId, int deviceModelId);
    Task<bool> SkuExistsAsync(string sku, int shopId, int? excludeItemId = null);
}
