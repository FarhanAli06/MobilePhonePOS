using Empire.Web.DTOs.Lookup;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empire.Web.Services.Lookup
{
    /// <summary>
    /// Interface for Lookup API service operations
    /// </summary>
    public interface ILookupApiService
    {
        Task<IEnumerable<LookupValueDto>> GetByCategoryAsync(string category);
        Task<SelectList> GetSelectListAsync(string category, string? selectedValue = null);
    }
}
