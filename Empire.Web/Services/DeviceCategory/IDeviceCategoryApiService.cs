using Empire.Web.DTOs.Device;
using Empire.Web.DTOs.DeviceCategory;

namespace Empire.Web.Services.DeviceCategory
{
    /// <summary>
    /// Interface for Device Category API service operations
    /// </summary>
    public interface IDeviceCategoryApiService
    {
        Task<List<DeviceCategoryDto>> GetAllAsync();
        Task<DeviceCategoryDto?> GetByIdAsync(int id);
        Task<List<DeviceCategoryDto>> GetByBrandIdAsync(int brandId);
        Task<DeviceCategoryDto?> CreateAsync(CreateDeviceCategoryRequestDto request);
        Task<DeviceCategoryDto?> UpdateAsync(int id, UpdateDeviceCategoryRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<bool> CheckDuplicateNameAsync(string name, int brandId, int? excludeId = null);
    }
}
