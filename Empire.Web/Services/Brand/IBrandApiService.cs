using Empire.Web.DTOs.Brand;

namespace Empire.Web.Services.Brand
{
    /// <summary>
    /// Interface for Brand API service operations
    /// </summary>
    public interface IBrandApiService
    {
        Task<List<BrandDto>> GetAllAsync();
        Task<BrandDto?> GetByIdAsync(int id);
        Task<BrandDto?> CreateAsync(CreateBrandRequestDto request);
        Task<BrandDto?> UpdateAsync(int id, UpdateBrandRequestDto request);
        Task<bool> DeleteAsync(int id);
        Task<bool> CheckDuplicateNameAsync(string name, int? excludeId = null);
        Task<List<BrandDto>> GetActiveBrandsAsync();
        Task<object?> GetAllSortedAsync();
    }
}
