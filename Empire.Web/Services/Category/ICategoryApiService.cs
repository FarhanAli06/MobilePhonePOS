using Empire.Web.DTOs.Category;

namespace Empire.Web.Services.Category
{
    public interface ICategoryApiService
    {
        Task<List<CategoryDto>?> GetAllAsync(string? type = null);
        Task<CategoryDto?> GetByIdAsync(int id);
        Task<CategoryDto?> CreateAsync(CreateCategoryRequestDto request);
        Task<CategoryDto?> UpdateAsync(int id, UpdateCategoryRequestDto request);
        Task<bool> DeleteAsync(int id);
    }
}
