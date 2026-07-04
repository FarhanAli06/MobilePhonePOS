using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.DeviceModel;

namespace Empire.Web.Services.DeviceModel
{
    /// <summary>
    /// Interface for Device Model API service operations
    /// </summary>
    public interface IDeviceModelApiService
    {
        Task<object?> GetAllSortedAsync();
        Task<List<DeviceModelDto>> GetAllAsync();
        Task<DeviceModelDto?> GetByIdAsync(int id);
        Task<DeviceModelDto?> CreateAsync(CreateDeviceModelRequestDto request);
        Task<DeviceModelDto?> UpdateAsync(int id, UpdateDeviceModelRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<List<DeviceModelDto>> GetByBrandAndCategoryAsync(int brandId, int categoryId);
    }
}
