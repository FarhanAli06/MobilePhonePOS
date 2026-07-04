using Microsoft.EntityFrameworkCore;
using Empire.Domain.Entities;
using Empire.Domain.Enums;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Repositories;

public class RepairRepository : Repository<Repair>, IRepairRepository
{
    public RepairRepository(EmpireDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Repair>> GetRepairsByShopIdAsync(int shopId)
    {
        return await _dbSet
            .Where(r => r.ShopId == shopId)
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Repair>> GetRepairsByDateRangeAsync(int shopId, DateTime startDate, DateTime endDate)
    {
        return await _dbSet
            .Where(r => r.ShopId == shopId && 
                       r.CreatedDate >= startDate && 
                       r.CreatedDate <= endDate)
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Repair>> GetRepairsByStatusAsync(int shopId, RepairStatus status)
    {
        return await _dbSet
            .Where(r => r.ShopId == shopId && r.Status == status.ToString())
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Repair>> GetRepairsByDateRangeAndStatusAsync(int shopId, DateTime startDate, DateTime endDate, RepairStatus status)
    {
        return await _dbSet
            .Where(r => r.ShopId == shopId && 
                       r.CreatedDate >= startDate && 
                       r.CreatedDate <= endDate && 
                       r.Status == status.ToString())
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Repair>> GetRepairsByCustomerIdAsync(int customerId)
    {
        return await _dbSet
            .Where(r => r.CustomerId == customerId)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();
    }

    public async Task<string> GenerateRepairNumberAsync(int shopId)
    {
        var today = DateTime.UtcNow.Date;
        var todayRepairsCount = await _dbSet
            .Where(r => r.ShopId == shopId && r.CreatedDate.Date == today)
            .CountAsync();

        var shopCode = shopId.ToString("D2");
        var dateCode = today.ToString("yyyyMMdd");
        var sequenceNumber = (todayRepairsCount + 1).ToString("D3");

        return $"REP{shopCode}{dateCode}{sequenceNumber}";
    }
}

