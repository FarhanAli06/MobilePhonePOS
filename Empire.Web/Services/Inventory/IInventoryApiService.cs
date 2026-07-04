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
        Task<IEnumerable<InventoryItemDto>> GetInventoryAsync(int shopId);
       // Task<bool> UpdateStockAsync(int id, int quantity);
    }
}
