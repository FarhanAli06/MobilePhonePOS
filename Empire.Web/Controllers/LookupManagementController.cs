using Microsoft.AspNetCore.Mvc;
using Empire.Web.Authorization;
using Empire.Web.DTOs.Brand;
using Empire.Web.DTOs.Category;
using Empire.Web.DTOs.DeviceCategory;
using Empire.Web.DTOs.Lookup;
using Empire.Web.DTOs.Item;
using Empire.Web.DTOs.InventoryCategory;
using Empire.Web.DTOs.DeviceModel;
using Empire.Web.Services.API;
using Empire.Web.Services.Brand;
using Empire.Web.Services.DeviceCategory;
using Empire.Web.Services.DeviceModel;
using Empire.Web.Services.LookupValue;
using Empire.Web.Services.Category;
using Empire.Web.Services.Item;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class LookupManagementController : Controller
{
    private readonly IBrandApiService _brandApi;
    private readonly IDeviceCategoryApiService _deviceCategoryApi;
    private readonly IDeviceModelApiService _deviceModelApi;
    private readonly ILookupValueApiService _lookupValueApi;
    private readonly IInventoryCategoryApiService _inventoryCategoryApi;
    private readonly ICategoryApiService _categoryApi;
    private readonly IItemApiService _itemService;
    private readonly ILogger<LookupManagementController> _logger;

    public LookupManagementController(
        IBrandApiService brandApi,
        IDeviceCategoryApiService deviceCategoryApi,
        IDeviceModelApiService deviceModelApi,
        ILookupValueApiService lookupValueApi,
        IInventoryCategoryApiService inventoryCategoryApi,
        ICategoryApiService categoryApi,
        IItemApiService itemService,
        ILogger<LookupManagementController> logger)
    {
        _brandApi = brandApi;
        _deviceCategoryApi = deviceCategoryApi;
        _deviceModelApi = deviceModelApi;
        _lookupValueApi = lookupValueApi;
        _inventoryCategoryApi = inventoryCategoryApi;
        _categoryApi = categoryApi;
        _itemService = itemService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        ViewBag.PageTitle = "Lookup Management";
        return View();
    }

    #region Brand Management

    [HttpGet]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            var data = await _brandApi.GetAllSortedAsync();
            if (data == null)
            {
                return Json(new { success = false, message = "Error retrieving brands" });
            }

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Check for duplicate name via API
            var isDuplicate = await _brandApi.CheckDuplicateNameAsync(request.Name);
            if (isDuplicate)
            {
                return Json(new { success = false, message = "A brand with this name already exists" });
            }

            var result = await _brandApi.CreateAsync(request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error creating brand" });
            }

            return Json(new { success = true, message = "Brand created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] UpdateBrandRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // Check if brand exists
            var existing = await _brandApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Brand not found" });
            }

            // Check for duplicate name (excluding current brand)
            var isDuplicate = await _brandApi.CheckDuplicateNameAsync(request.Name, id);
            if (isDuplicate)
            {
                return Json(new { success = false, message = "A brand with this name already exists" });
            }

            var result = await _brandApi.UpdateAsync(id, request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error updating brand" });
            }

            return Json(new { success = true, message = "Brand updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        try
        {
            // Check if brand exists
            var existing = await _brandApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Brand not found" });
            }

            // Note: The API should handle checking if brand is used in device models
            var result = await _brandApi.DeleteAsync(id);
            if (!result)
            {
                return Json(new { success = false, message = "Cannot delete brand that has device models" });
            }

            return Json(new { success = true, message = "Brand deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Device Category Management

    [HttpGet]
    public async Task<IActionResult> GetDeviceCategories()
    {
        try
        {
            var categories = await _deviceCategoryApi.GetAllAsync();
            if (categories == null)
            {
                return Json(new { success = false, message = "Error retrieving device categories" });
            }

            var data = categories
                .OrderBy(dc => dc.DisplayOrder)
                .ThenBy(dc => dc.Name)
                .Select(dc => new
                {
                    Id = dc.Id,
                    Name = dc.Name,
                    Description = dc.Description,
                    DisplayOrder = dc.DisplayOrder,
                    IsActive = dc.IsActive
                });

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device categories");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeviceCategory([FromBody] CreateDeviceCategoryRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _deviceCategoryApi.CreateAsync(request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error creating device category" });
            }

            return Json(new { success = true, message = "Device category created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device category");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateDeviceCategory(int id, [FromBody] UpdateDeviceCategoryRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var existing = await _deviceCategoryApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Device category not found" });
            }

            var result = await _deviceCategoryApi.UpdateAsync(id, request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error updating device category" });
            }

            return Json(new { success = true, message = "Device category updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device category {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteDeviceCategory(int id)
    {
        try
        {
            var existing = await _deviceCategoryApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Device category not found" });
            }

            var result = await _deviceCategoryApi.DeleteAsync(id);
            if (!result)
            {
                return Json(new { success = false, message = "Cannot delete device category that has models" });
            }

            return Json(new { success = true, message = "Device category deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device category {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Device Model Management

    [HttpGet]
    public async Task<IActionResult> GetDeviceModels()
    {
        try
        {
            var data = await _deviceModelApi.GetAllSortedAsync();
            if (data == null)
            {
                return Json(new { success = false, message = "Error retrieving device models" });
            }

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device models");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeviceModel([FromBody] CreateDeviceModelRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _deviceModelApi.CreateAsync(request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error creating device model" });
            }

            return Json(new { success = true, message = "Device model created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device model");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateDeviceModel(int id, [FromBody] UpdateDeviceModelRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var existing = await _deviceModelApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Device model not found" });
            }

            var result = await _deviceModelApi.UpdateAsync(id, request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error updating device model" });
            }

            return Json(new { success = true, message = "Device model updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device model {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteDeviceModel(int id)
    {
        try
        {
            var existing = await _deviceModelApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Device model not found" });
            }

            var result = await _deviceModelApi.DeleteAsync(id);
            if (!result)
            {
                return Json(new { success = false, message = "Cannot delete device model that is in use" });
            }

            return Json(new { success = true, message = "Device model deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device model {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion


    #region Lookup Value Management

    [HttpGet]
    public async Task<IActionResult> GetLookupValues()
    {
        try
        {
            var lookupValues = await _lookupValueApi.GetAllAsync();
            if (lookupValues == null)
            {
                return Json(new { success = false, message = "Error retrieving lookup values" });
            }

            var data = lookupValues
                .OrderBy(lv => lv.Category)
                .ThenBy(lv => lv.DisplayOrder)
                .ThenBy(lv => lv.Value)
                .Select(lv => new
                {
                    Id = lv.Id,
                    Category = lv.Category,
                    Value = lv.Value,
                    CategoryId = lv.CategoryId,
                    DisplayOrder = lv.DisplayOrder,
                    IsActive = lv.IsActive,
                    CreatedDate = lv.CreatedDate
                });

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLookupValue([FromBody] CreateLookupValueRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _lookupValueApi.CreateAsync(request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error creating lookup value" });
            }

            return Json(new { success = true, message = "Lookup value created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lookup value");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLookupValue(int id, [FromBody] UpdateLookupValueRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var existing = await _lookupValueApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Lookup value not found" });
            }

            var result = await _lookupValueApi.UpdateAsync(id, request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error updating lookup value" });
            }

            return Json(new { success = true, message = "Lookup value updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lookup value {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteLookupValue(int id)
    {
        try
        {
            var existing = await _lookupValueApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Lookup value not found" });
            }

            var result = await _lookupValueApi.DeleteAsync(id);
            if (!result)
            {
                return Json(new { success = false, message = "Error deleting lookup value" });
            }

            return Json(new { success = true, message = "Lookup value deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lookup value {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Inventory Category Management

    [HttpGet]
    public async Task<IActionResult> GetInventoryCategories()
    {
        try
        {
            var categories = await _inventoryCategoryApi.GetAllAsync();
            if (categories == null)
            {
                return Json(new { success = false, message = "Error retrieving inventory categories" });
            }

            var data = categories
                .OrderBy(ic => ic.DisplayOrder)
                .ThenBy(ic => ic.Name)
                .Select(ic => new
                {
                    Id = ic.Id,
                    Name = ic.Name,
                    Description = ic.Description,
                    DisplayOrder = ic.DisplayOrder,
                    IsActive = ic.IsActive,
                });

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory categories");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateInventoryCategory([FromBody] CreateInventoryCategoryRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _inventoryCategoryApi.CreateAsync(request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error creating inventory category" });
            }

            return Json(new { success = true, message = "Inventory category created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory category");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateInventoryCategory(int id, [FromBody] UpdateInventoryCategoryRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var existing = await _inventoryCategoryApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Inventory category not found" });
            }

            var result = await _inventoryCategoryApi.UpdateAsync(id, request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error updating inventory category" });
            }

            return Json(new { success = true, message = "Inventory category updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory category {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteInventoryCategory(int id)
    {
        try
        {
            var existing = await _inventoryCategoryApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Inventory category not found" });
            }

            var result = await _inventoryCategoryApi.DeleteAsync(id);
            if (!result)
            {
                return Json(new { success = false, message = "Cannot delete inventory category that is in use" });
            }

            return Json(new { success = true, message = "Inventory category deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory category {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Category Management

    [HttpGet]
    public async Task<IActionResult> GetCategories(string? categoryType = null)
    {
        try
        {
            var categories = await _categoryApi.GetAllAsync(categoryType);
            if (categories == null)
            {
                return Json(new { success = false, message = "Error retrieving categories" });
            }

            var data = categories
                .OrderBy(c => c.CategoryType)
                .ThenBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CategoryType = c.CategoryType,
                    ParentCategoryId = c.ParentCategoryId,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive
                });

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _categoryApi.CreateAsync(request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error creating category" });
            }

            return Json(new { success = true, message = "Category created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var existing = await _categoryApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Category not found" });
            }

            var result = await _categoryApi.UpdateAsync(id, request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error updating category" });
            }

            return Json(new { success = true, message = "Category updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var existing = await _categoryApi.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Category not found" });
            }

            var result = await _categoryApi.DeleteAsync(id);
            if (!result)
            {
                return Json(new { success = false, message = "Cannot delete category that has subcategories, lookup values, or inventory items" });
            }

            return Json(new { success = true, message = "Category deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Item Management

    [HttpGet]
    public async Task<IActionResult> GetItems()
    {
        try
        {
            var items = await _itemService.GetAllAsync();
            if (items == null)
            {
                return Json(new { success = false, message = "Error retrieving items" });
            }

            var data = items
                .OrderBy(i => i.Name)
                .Select(i => new
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    SKU = i.SKU,
                    UnitPrice = i.UnitPrice,
                    CostPrice = i.CostPrice,
                    IsActive = i.IsActive,
                    CreatedDate = i.CreatedDate
                });

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting items");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var result = await _itemService.CreateAsync(request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error creating item" });
            }

            return Json(new { success = true, message = "Item created successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating item");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateItemRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var existing = await _itemService.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            var result = await _itemService.UpdateAsync(id, request);
            if (result == null)
            {
                return Json(new { success = false, message = "Error updating item" });
            }

            return Json(new { success = true, message = "Item updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating item {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteItem(int id)
    {
        try
        {
            var existing = await _itemService.GetByIdAsync(id);
            if (existing == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            var result = await _itemService.DeleteAsync(id);
            if (!result)
            {
                return Json(new { success = false, message = "Error deleting item" });
            }

            return Json(new { success = true, message = "Item deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion
}
