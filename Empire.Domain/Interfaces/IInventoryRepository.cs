using Empire.Domain.Entities;
using Empire.Domain.Enums;

namespace Empire.Domain.Interfaces;

public interface IInventoryRepository : IRepository<Inventory>
{
    Task<IEnumerable<Inventory>> GetInventoryByShopIdAsync(int shopId);
    Task<IEnumerable<Inventory>> GetInventoryByDeviceTypeAsync(int shopId, DeviceType deviceType);
    Task<IEnumerable<Inventory>> GetLowStockItemsAsync(int shopId);
    Task<IEnumerable<Inventory>> GetLowStockItemsWithNotificationAsync(int shopId);
}

