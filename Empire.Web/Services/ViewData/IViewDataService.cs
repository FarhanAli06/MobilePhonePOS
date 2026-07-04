using Empire.Web.DTOs.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empire.Web.Services.ViewData
{
    /// <summary>
    /// Service for preparing view data (dropdowns, select lists, etc.)
    /// </summary>
    public interface IViewDataService
    {
        /// <summary>
        /// Get brands as dropdown items, optionally filtered by active status
        /// </summary>
        Task<List<DropdownItemDto>> GetBrandsDropdownAsync(bool activeOnly = true);

        /// <summary>
        /// Get categories as dropdown items, optionally filtered by active status
        /// </summary>
        Task<List<DropdownItemDto>> GetCategoriesDropdownAsync(bool activeOnly = true);

        /// <summary>
        /// Get device models as dropdown items
        /// </summary>
        Task<List<DropdownItemDto>> GetDeviceModelsDropdownAsync();

        /// <summary>
        /// Get customers as dropdown items for a specific shop
        /// </summary>
        Task<List<DropdownItemDto>> GetCustomersDropdownAsync(int shopId);

        /// <summary>
        /// Get lookup values as select list items by category
        /// </summary>
        Task<List<SelectListItem>> GetLookupSelectListAsync(string category);
    }
}
