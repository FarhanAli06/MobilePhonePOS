using Empire.Domain.Entities;

namespace Empire.Domain.Interfaces;

public interface IDeviceRepository : IRepository<Device>
{
    Task<IEnumerable<string>> GetBrandsAsync();
    Task<IEnumerable<string>> GetCategoriesByBrandAsync(string brand);
    Task<IEnumerable<Device>> GetModelsByBrandAndCategoryAsync(string brand, string category);
    Task<Device?> GetByBrandCategoryModelAsync(string brand, string category, string model);
    Task<IEnumerable<Device>> GetDevicesByShopAsync(int shopId);
}

