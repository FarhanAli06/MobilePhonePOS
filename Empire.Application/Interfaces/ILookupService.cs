using Empire.Application.DTOs.Lookup;
namespace Empire.Application.Interfaces;
public interface ILookupService
{
    /// <summary>Returns all lookup values for the given shop (plus global values where ShopId = 0).</summary>
    Task<List<LookupValueDto>> GetAllAsync(int shopId);
    /// <summary>Returns lookup values for the given category scoped to the shop (plus global values).</summary>
    Task<List<LookupValueDto>> GetByCategoryAsync(string category, int shopId);
    Task<LookupValueDto?> GetByIdAsync(int id);
    Task<List<string>> GetCategoriesAsync(int shopId);
}
