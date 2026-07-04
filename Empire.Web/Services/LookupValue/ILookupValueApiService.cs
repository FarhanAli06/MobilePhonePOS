using Empire.Web.DTOs.Lookup;

namespace Empire.Web.Services.LookupValue
{
    public interface ILookupValueApiService
    {
        Task<List<LookupValueDto>?> GetAllAsync(string? category = null, string? type = null);
        Task<LookupValueDto?> GetByIdAsync(int id);
        Task<LookupValueDto?> CreateAsync(CreateLookupValueRequestDto request);
        Task<LookupValueDto?> UpdateAsync(int id, UpdateLookupValueRequestDto request);
        Task<bool> DeleteAsync(int id);
    }
}
