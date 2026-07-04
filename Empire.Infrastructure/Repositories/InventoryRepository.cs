using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Enums;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Repositories;

public class InventoryRepository : Repository<Inventory>, IInventoryRepository
{
    public InventoryRepository(EmpireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Inventory>> GetInventoryByShopIdAsync(int shopId)
    {
        return await _dbSet
            .Where(i => i.ShopId == shopId && !i.IsDeleted)
            .Include(i => i.Device)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> GetInventoryByDeviceTypeAsync(int shopId, DeviceType deviceType)
    {
        return await _dbSet
            .Where(i => i.ShopId == shopId && i.DeviceType == deviceType && !i.IsDeleted)
            .Include(i => i.Device)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> GetLowStockItemsAsync(int shopId)
    {
        return await _dbSet
            .Where(i => i.ShopId == shopId && i.Stock <= i.ReorderPoint && !i.IsDeleted)
            .Include(i => i.Device)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Inventory>> GetLowStockItemsWithNotificationAsync(int shopId)
    {
        return await _dbSet
            .Where(i => i.ShopId == shopId && 
                       i.Stock <= i.ReorderPoint && 
                       i.LowStockNotification && 
                       !i.IsDeleted)
            .Include(i => i.Device)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }
}

