using AutoMapper;
using Empire.Web.DTOs.Common;
using Empire.Web.Services.Brand;
using Empire.Web.Services.Customer;
using Empire.Web.Services.DeviceCategory;
using Empire.Web.Services.DeviceModel;
using Empire.Web.Services.Lookup;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Empire.Web.Services.ViewData
{
    /// <summary>
    /// Service for preparing view data (dropdowns, select lists, etc.)
    /// Uses AutoMapper for projections
    /// </summary>
    public class ViewDataService : IViewDataService
    {
        private readonly IBrandApiService _brandApi;
        private readonly IDeviceCategoryApiService _categoryApi;
        private readonly IDeviceModelApiService _modelApi;
        private readonly ICustomerApiService _customerApi;
        private readonly ILookupApiService _lookupApi;
        private readonly IMapper _mapper;
        private readonly ILogger<ViewDataService> _logger;

        public ViewDataService(
            IBrandApiService brandApi,
            IDeviceCategoryApiService categoryApi,
            IDeviceModelApiService modelApi,
            ICustomerApiService customerApi,
            ILookupApiService lookupApi,
            IMapper mapper,
            ILogger<ViewDataService> logger)
        {
            _brandApi = brandApi;
            _categoryApi = categoryApi;
            _modelApi = modelApi;
            _customerApi = customerApi;
            _lookupApi = lookupApi;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<DropdownItemDto>> GetBrandsDropdownAsync(bool activeOnly = true)
        {
            try
            {
                var brands = await _brandApi.GetAllAsync();
                
                if (brands == null)
                    return new List<DropdownItemDto>();

                var filteredBrands = activeOnly 
                    ? brands.Where(b => b.IsActive)
                    : brands;

                return filteredBrands
                    .OrderBy(b => b.DisplayOrder)
                    .ThenBy(b => b.Name)
                    .Select(b => _mapper.Map<DropdownItemDto>(b))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting brands dropdown");
                return new List<DropdownItemDto>();
            }
        }

        public async Task<List<DropdownItemDto>> GetCategoriesDropdownAsync(bool activeOnly = true)
        {
            try
            {
                var categories = await _categoryApi.GetAllAsync();
                
                if (categories == null)
                    return new List<DropdownItemDto>();

                var filteredCategories = activeOnly 
                    ? categories.Where(c => c.IsActive)
                    : categories;

                return filteredCategories
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .Select(c => _mapper.Map<DropdownItemDto>(c))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories dropdown");
                return new List<DropdownItemDto>();
            }
        }

        public async Task<List<DropdownItemDto>> GetDeviceModelsDropdownAsync()
        {
            try
            {
                var models = await _modelApi.GetAllAsync();
                
                if (models == null)
                    return new List<DropdownItemDto>();

                return models
                    .OrderBy(m => m.Name)
                    .Select(m => _mapper.Map<DropdownItemDto>(m))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting device models dropdown");
                return new List<DropdownItemDto>();
            }
        }

        public async Task<List<DropdownItemDto>> GetCustomersDropdownAsync(int shopId)
        {
            try
            {
                var customers = await _customerApi.GetByShopAsync(shopId);
                
                if (customers == null)
                    return new List<DropdownItemDto>();

                return customers
                    .OrderBy(c => c.FirstName)
                    .ThenBy(c => c.LastName)
                    .Select(c => _mapper.Map<DropdownItemDto>(c))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customers dropdown for shop {ShopId}", shopId);
                return new List<DropdownItemDto>();
            }
        }

        public async Task<List<SelectListItem>> GetLookupSelectListAsync(string category)
        {
            try
            {
                var lookups = await _lookupApi.GetByCategoryAsync(category);
                
                if (lookups == null)
                    return new List<SelectListItem>();

                return lookups
                    .Select(l => _mapper.Map<SelectListItem>(l))
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting lookup select list for category {Category}", category);
                return new List<SelectListItem>();
            }
        }
    }
}
