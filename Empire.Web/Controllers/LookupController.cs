using Microsoft.AspNetCore.Mvc;
using Empire.Web.Services.LookupManagement;

namespace Empire.Web.Controllers;

public class LookupController : Controller
{
    private readonly ILookupManagementService _lookupService;
    private readonly ILogger<LookupController> _logger;

    public LookupController(
        ILookupManagementService lookupService,
        ILogger<LookupController> logger)
    {
        _lookupService = lookupService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            var brands = await _lookupService.GetBrandsAsync();
            
            _logger.LogInformation("GetBrands: Returned {Count} brands", brands.Count);

            var result = brands.Select(b => new { Id = b.Id, Name = b.Name }).ToList();
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBrands error");
            return Json(new { error = "Failed to load brands", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLookupValuesByCategory(string category)
    {
        try
        {
            var lookupValues = await _lookupService.GetLookupValuesByCategoryAsync(category);

            var result = lookupValues.Select(lv => new 
            { 
                Id = lv.Id, 
                Value = lv.Value, 
                Category = lv.Category 
            }).ToList();

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values for category {Category}", category);
            return Json(new { error = "Failed to load lookup values", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDeviceCategories(int? brandId = null)
    {
        try
        {
            var categories = await _lookupService.GetDeviceCategoriesAsync(brandId);

            _logger.LogInformation("GetDeviceCategories: Returned {Count} categories", categories.Count);

            var result = categories.Select(dc => new { Id = dc.Id, Name = dc.Name }).ToList();
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDeviceCategories error");
            return Json(new { error = "Failed to load device categories", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDeviceModels(int? categoryId = null, int? brandId = null)
    {
        try
        {
            var models = await _lookupService.GetDeviceModelsAsync(categoryId, brandId);

            _logger.LogInformation(
                "GetDeviceModels: CategoryId={CategoryId}, BrandId={BrandId}, Returned {Count} models",
                categoryId, brandId, models.Count);

            var result = models.Select(dm => new { Id = dm.Id, Name = dm.Name }).ToList();
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDeviceModels error");
            return Json(new { error = "Failed to load device models", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetInventoryCategories()
    {
        try
        {
            var categories = await _lookupService.GetInventoryCategoriesAsync();

            var result = categories.Select(ic => new { Id = ic.Id, Name = ic.Name }).ToList();
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inventory categories");
            return Json(new { error = "Failed to load inventory categories", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLookupValues(string type)
    {
        try
        {
            var values = await _lookupService.GetLookupValuesByTypeAsync(type);

            var result = values.Select(lv => new 
            { 
                Id = lv.Id, 
                Value = lv.Value, 
                Text = lv.Value 
            }).ToList();

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values for type {Type}", type);
            return Json(new { error = $"Failed to load {type} values", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetStates()
    {
        try
        {
            var states = await _lookupService.GetStatesAsync();

            var result = states.Select(s => new { Id = s, Name = s }).ToList();
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting states");
            return Json(new { error = "Failed to load states", details = ex.Message });
        }
    }
}
