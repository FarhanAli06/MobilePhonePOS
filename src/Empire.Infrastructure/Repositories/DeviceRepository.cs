using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Device;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Repositories;

public class DeviceRepository : Repository<Device>, IDeviceRepository
{
    public DeviceRepository(EmpireDbContext context) : base(context)
    {
    }

    public Task<IEnumerable<string>> GetBrandsAsync()
    {
        // Note: This method needs to be updated for new lookup structure
        return Task.FromResult<IEnumerable<string>>(new List<string>());
    }

    public Task<IEnumerable<string>> GetCategoriesByBrandAsync(string brand)
    {
        // Note: This method needs to be updated for new lookup structure
        return Task.FromResult<IEnumerable<string>>(new List<string>());
    }

    public Task<IEnumerable<Device>> GetModelsByBrandAndCategoryAsync(string brand, string category)
    {
        // Note: This method needs to be updated for new lookup structure
        return Task.FromResult<IEnumerable<Device>>(new List<Device>());
    }

    public Task<Device?> GetByBrandCategoryModelAsync(string brand, string category, string model)
    {
        // Note: This method needs to be updated for new lookup structure
        return Task.FromResult<Device?>(null);
    }

    public async Task<IEnumerable<Device>> GetDevicesByShopAsync(int shopId)
    {
        return await _dbSet
            .Where(d => d.ShopId == shopId && !d.IsDeleted)
            .Include(d => d.SoldToCustomer)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();
    }

    // Additional method for filtering (used by DeviceService)
    public async Task<IEnumerable<Device>> GetDevicesAsync(DeviceFilterRequest filter)
    {
        var query = _dbSet.Where(d => !d.IsDeleted);

        if (filter.ShopId.HasValue)
            query = query.Where(d => d.ShopId == filter.ShopId.Value);

        // Remove DeviceType filter for now since it's not in the new structure
        // if (filter.DeviceType.HasValue)
        //     query = query.Where(d => d.DeviceType == filter.DeviceType.Value);

        if (!string.IsNullOrEmpty(filter.Brand))
            query = query.Where(d => d.Brand != null && d.Brand.Name.Contains(filter.Brand));

        if (!string.IsNullOrEmpty(filter.Category))
            query = query.Where(d => d.DeviceCategory != null && d.DeviceCategory.Name.Contains(filter.Category));

        if (!string.IsNullOrEmpty(filter.Model))
            query = query.Where(d => d.DeviceModel != null && d.DeviceModel.Name.Contains(filter.Model));

        if (!string.IsNullOrEmpty(filter.IMEISerialNumber))
            query = query.Where(d => d.IMEISerialNumber != null && d.IMEISerialNumber.Contains(filter.IMEISerialNumber));

        if (filter.IsAvailableForSale.HasValue)
            query = query.Where(d => d.IsAvailableForSale == filter.IsAvailableForSale.Value);

        if (filter.IsSold.HasValue)
            query = query.Where(d => d.IsSold == filter.IsSold.Value);

        if (!string.IsNullOrEmpty(filter.NetworkStatus))
            query = query.Where(d => d.NetworkStatus == filter.NetworkStatus);

        if (!string.IsNullOrEmpty(filter.ScratchesCondition))
            query = query.Where(d => d.ScratchesCondition == filter.ScratchesCondition);

        if (filter.MinBuyingPrice.HasValue)
            query = query.Where(d => d.BuyingPrice >= filter.MinBuyingPrice.Value);

        if (filter.MaxBuyingPrice.HasValue)
            query = query.Where(d => d.BuyingPrice <= filter.MaxBuyingPrice.Value);

        if (filter.MinSellingPrice.HasValue)
            query = query.Where(d => d.SellingPrice >= filter.MinSellingPrice.Value);

        if (filter.MaxSellingPrice.HasValue)
            query = query.Where(d => d.SellingPrice <= filter.MaxSellingPrice.Value);

        if (filter.CreatedFromDate.HasValue)
            query = query.Where(d => d.CreatedDate >= filter.CreatedFromDate.Value);

        if (filter.CreatedToDate.HasValue)
            query = query.Where(d => d.CreatedDate <= filter.CreatedToDate.Value);

        if (filter.SoldFromDate.HasValue)
            query = query.Where(d => d.SoldDate >= filter.SoldFromDate.Value);

        if (filter.SoldToDate.HasValue)
            query = query.Where(d => d.SoldDate <= filter.SoldToDate.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(d => 
                (d.Brand != null && d.Brand.Name.Contains(filter.SearchTerm)) ||
                (d.DeviceModel != null && d.DeviceModel.Name.Contains(filter.SearchTerm)) ||
                (d.IMEISerialNumber != null && d.IMEISerialNumber.Contains(filter.SearchTerm)) ||
                (d.Notes != null && d.Notes.Contains(filter.SearchTerm)));
        }

        return await query
            .Include(d => d.Brand)
            .Include(d => d.DeviceCategory)
            .Include(d => d.DeviceModel)
            .Include(d => d.SoldToCustomer)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync();
    }
}

