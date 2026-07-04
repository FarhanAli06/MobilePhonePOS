using Empire.Web.DTOs.Styling;

namespace Empire.Web.Services.Styling;

public interface IStylingApiService
{
    Task<List<StylingDto>?> GetAllAsync();
    Task<List<StylingSelectionDto>?> GetSelectionsAsync();
    Task<StylingDto?> GetByIdAsync(int id);
    Task<StylingDto?> CreateAsync(CreateStylingRequestDto request);
    Task<StylingDto?> UpdateAsync(int id, UpdateStylingRequestDto request);
    Task<bool> DeleteAsync(int id);
    Task<bool> CheckDuplicateNameAsync(string name, int? excludeId = null);
    Task<object?> GetAssignmentsAsync();
    Task<bool> AssignToBrandAsync(int brandId, int? stylingId);
    Task<bool> AssignToCategoryAsync(int categoryId, int? stylingId);
    Task<bool> AssignToModelAsync(int modelId, int? stylingId);
}
