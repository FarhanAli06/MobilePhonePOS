using Empire.Application.DTOs.Inventory;
using Empire.Domain.Enums;

namespace Empire.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryDto> CreateInventoryAsync(CreateInventoryRequest request);
    Task<InventoryDto?> GetInventoryByIdAsync(int inventoryId, int shopId);
    Task<IEnumerable<InventoryDto>> GetInventoryAsync(InventoryFilterRequest filter);
    Task<InventoryDto?> UpdateInventoryAsync(int inventoryId, UpdateInventoryRequest request);
    Task<bool> DeleteInventoryAsync(int inventoryId, int shopId);
    Task<IEnumerable<InventoryDto>> GetLowStockItemsAsync(int shopId);
    Task<bool> AdjustInventoryAsync(InventoryAdjustmentRequest request, int userId);
    Task<IEnumerable<InventoryDto>> GetInventoryByDeviceTypeAsync(int shopId, DeviceType deviceType);
}

