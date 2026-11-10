using Empire.Web.DTOs.Inventory;

namespace Empire.Web.Services.Inventory
{
    /// <summary>
    /// Interface for Inventory API service operations
    /// </summary>
    public interface IInventoryApiService
    {
        Task<InventoryItemDto?> GetByIdAsync(int id);
       // Task<InventoryItemDto?> CreateAsync(CreateInventoryItemRequestDto request);
        Task<InventoryItemDto?> UpdateAsync(int id, UpdateInventoryItemRequestDto request);
        Task<bool> DeleteAsync(int id);
       // Task<IEnumerable<InventoryItemDto>> SearchAsync(string searchTerm);
        Task<IEnumerable<InventoryItemDto>> GetLowStockItemsAsync(int shopId = 0, int threshold = 10);
        Task<IEnumerable<InventoryItemDto>> GetInventoryAsync(
            int shopId,
            int? brandId = null,
            int? deviceCategoryId = null,
            int? deviceModelId = null,
            int? inventoryCategoryId = null,
            string? search = null,
            bool? lowStockOnly = null);
       // Task<bool> UpdateStockAsync(int id, int quantity);
    }
}
