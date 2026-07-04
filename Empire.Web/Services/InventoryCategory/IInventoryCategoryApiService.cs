using Empire.Web.DTOs.InventoryCategory;

namespace Empire.Web.Services.API;

public interface IInventoryCategoryApiService
{
    Task<List<InventoryCategoryDto>?> GetAllAsync();
    Task<InventoryCategoryDto?> GetByIdAsync(int id);
    Task<InventoryCategoryDto?> CreateAsync(CreateInventoryCategoryRequestDto request);
    Task<InventoryCategoryDto?> UpdateAsync(int id, UpdateInventoryCategoryRequestDto request);
    Task<bool> DeleteAsync(int id);
}
