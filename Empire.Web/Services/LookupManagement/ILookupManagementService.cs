using Empire.Web.DTOs;
using Empire.Web.DTOs.Brand;
using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.InventoryCategory;
using Empire.Web.DTOs.Lookup;

namespace Empire.Web.Services.LookupManagement;

public interface ILookupManagementService
{
    Task<List<BrandDto>> GetBrandsAsync();
    Task<List<DeviceCategoryDto>> GetDeviceCategoriesAsync(int? brandId = null);
    Task<List<DeviceModelDto>> GetDeviceModelsAsync(int? categoryId = null, int? brandId = null);
    Task<List<InventoryCategoryDto>> GetInventoryCategoriesAsync();
    Task<List<LookupValueDto>> GetLookupValuesByCategoryAsync(string category);
    Task<List<LookupValueDto>> GetLookupValuesByTypeAsync(string type);
    Task<List<string>> GetStatesAsync();
}
