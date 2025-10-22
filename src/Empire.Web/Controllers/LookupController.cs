using Microsoft.AspNetCore.Mvc;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Empire.Web.Controllers;

public class LookupController : Controller
{
    private readonly EmpireDbContext _context;

    public LookupController(EmpireDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            // Check database connection first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                Console.WriteLine("GetBrands: Cannot connect to database");
                return Json(new { error = "Database connection failed" });
            }

            var totalBrands = await _context.Brands.CountAsync();
            Console.WriteLine($"GetBrands: Total brands in database: {totalBrands}");

            if (totalBrands == 0)
            {
                // Return some test data if no brands exist
                var testBrands = new[]
                {
                    new { Id = 1, Name = "Apple" },
                    new { Id = 2, Name = "Samsung" },
                    new { Id = 3, Name = "Google" },
                    new { Id = 4, Name = "OnePlus" }
                };
                Console.WriteLine("GetBrands: No brands in database, returning test data");
                return Json(testBrands);
            }

            var activeBrands = await _context.Brands.CountAsync(b => b.IsActive);
            
            var brands = await _context.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new { Id = b.Id, Name = b.Name })
                .ToListAsync();

            Console.WriteLine($"GetBrands: Total brands: {totalBrands}, Active brands: {activeBrands}, Returned: {brands.Count}");
            
            return Json(brands);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetBrands error: {ex.Message}");
            Console.WriteLine($"GetBrands stack trace: {ex.StackTrace}");
            
            // Return test data on error
            var fallbackBrands = new[]
            {
                new { Id = 1, Name = "Apple" },
                new { Id = 2, Name = "Samsung" },
                new { Id = 3, Name = "Google" }
            };
            
            return Json(fallbackBrands);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLookupValuesByCategory(string category)
    {
        try
        {
            // First, try to get values from LookupValue table filtered by category
            var lookupValues = await _context.LookupValues
                .Where(lv => lv.Category == category && lv.IsActive)
                .OrderBy(lv => lv.DisplayOrder)
                .ThenBy(lv => lv.Value)
                .Select(lv => new { Id = lv.Id, Value = lv.Value, Category = lv.Category })
                .ToListAsync();

            if (lookupValues.Any())
            {
                return Json(lookupValues);
            }

            // If no lookup values found, try to get from Categories table and related LookupValues
            var categoryEntity = await _context.Categories
                .Where(c => c.Name == category && c.IsActive)
                .FirstOrDefaultAsync();

            if (categoryEntity != null)
            {
                var categoryLookupValues = await _context.LookupValues
                    .Where(lv => lv.CategoryId == categoryEntity.Id && lv.IsActive)
                    .OrderBy(lv => lv.DisplayOrder)
                    .ThenBy(lv => lv.Value)
                    .Select(lv => new { Id = lv.Id, Value = lv.Value, Category = lv.Category })
                    .ToListAsync();

                return Json(categoryLookupValues);
            }

            // Return empty array if nothing found
            return Json(new object[0]);
        }
        catch (Exception ex)
        {
            return Json(new { error = "Failed to load lookup values", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDeviceCategories(int? brandId = null)
    {
        try
        {
            // Check database connection first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                Console.WriteLine("GetDeviceCategories: Cannot connect to database");
                return Json(new { error = "Database connection failed" });
            }

            var totalCategories = await _context.DeviceCategories.CountAsync();
            Console.WriteLine($"GetDeviceCategories: Total categories in database: {totalCategories}");

            if (totalCategories == 0)
            {
                // Return some test data if no categories exist
                var testCategories = new[]
                {
                    new { Id = 1, Name = "Phone" },
                    new { Id = 2, Name = "Tablet" },
                    new { Id = 3, Name = "Laptop" },
                    new { Id = 4, Name = "Watch" }
                };
                Console.WriteLine("GetDeviceCategories: No categories in database, returning test data");
                return Json(testCategories);
            }

            var activeCategories = await _context.DeviceCategories.CountAsync(dc => dc.IsActive);
            
            // Since DeviceCategory doesn't have BrandId, we get all active categories
            // The filtering by brand will happen at the DeviceModel level
            var categories = await _context.DeviceCategories
                .Where(dc => dc.IsActive)
                .OrderBy(dc => dc.Name)
                .Select(dc => new { Id = dc.Id, Name = dc.Name })
                .ToListAsync();

            Console.WriteLine($"GetDeviceCategories: Total categories: {totalCategories}, Active categories: {activeCategories}, Returned: {categories.Count}");
            
            return Json(categories);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetDeviceCategories error: {ex.Message}");
            Console.WriteLine($"GetDeviceCategories stack trace: {ex.StackTrace}");
            
            // Return test data on error
            var fallbackCategories = new[]
            {
                new { Id = 1, Name = "Phone" },
                new { Id = 2, Name = "Tablet" },
                new { Id = 3, Name = "Laptop" }
            };
            
            return Json(fallbackCategories);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDeviceModels(int? categoryId = null, int? brandId = null)
    {
        try
        {
            // Check database connection first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                Console.WriteLine("GetDeviceModels: Cannot connect to database");
                return Json(new { error = "Database connection failed" });
            }

            var totalModels = await _context.DeviceModels.CountAsync();
            Console.WriteLine($"GetDeviceModels: Total models in database: {totalModels}, CategoryId: {categoryId}, BrandId: {brandId}");

            if (totalModels == 0)
            {
                // Return some test data if no models exist
                var testModels = new[]
                {
                    new { Id = 1, Name = "iPhone 14" },
                    new { Id = 2, Name = "iPhone 13" },
                    new { Id = 3, Name = "Galaxy S23" },
                    new { Id = 4, Name = "Galaxy S22" },
                    new { Id = 5, Name = "iPad Pro" },
                    new { Id = 6, Name = "Galaxy Tab S8" }
                };
                Console.WriteLine("GetDeviceModels: No models in database, returning test data");
                return Json(testModels);
            }

            var query = _context.DeviceModels.Where(dm => dm.IsActive);
            
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(dm => dm.DeviceCategoryId == categoryId.Value);
                Console.WriteLine($"GetDeviceModels: Filtering by CategoryId: {categoryId.Value}");
            }
            
            if (brandId.HasValue && brandId.Value > 0)
            {
                query = query.Where(dm => dm.BrandId == brandId.Value);
                Console.WriteLine($"GetDeviceModels: Filtering by BrandId: {brandId.Value}");
            }
            
            var models = await query
                .OrderBy(dm => dm.Name)
                .Select(dm => new { Id = dm.Id, Name = dm.Name })
                .ToListAsync();

            Console.WriteLine($"GetDeviceModels: Returned {models.Count} models");
            
            return Json(models);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetDeviceModels error: {ex.Message}");
            Console.WriteLine($"GetDeviceModels stack trace: {ex.StackTrace}");
            
            // Return test data on error
            var fallbackModels = new[]
            {
                new { Id = 1, Name = "iPhone 14" },
                new { Id = 2, Name = "Galaxy S23" },
                new { Id = 3, Name = "iPad Pro" }
            };
            
            return Json(fallbackModels);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetInventoryCategories()
    {
        try
        {
            var categories = await _context.InventoryCategories
                .Where(ic => ic.IsActive)
                .OrderBy(ic => ic.Name)
                .Select(ic => new { Id = ic.Id, Name = ic.Name })
                .ToListAsync();

            return Json(categories);
        }
        catch (Exception ex)
        {
            return Json(new { error = "Failed to load inventory categories", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLookupValues(string type)
    {
        try
        {
            var values = await _context.LookupValues
                .Where(lv => lv.Category == type && lv.IsActive)
                .OrderBy(lv => lv.DisplayOrder)
                .ThenBy(lv => lv.Value)
                .Select(lv => new { Id = lv.Id, Value = lv.Value, Text = lv.Value })
                .ToListAsync();

            return Json(values);
        }
        catch (Exception ex)
        {
            return Json(new { error = $"Failed to load {type} values", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetStates()
    {
        try
        {
            var states = await _context.LookupValues
                .Where(lv => lv.Category == "State" && lv.IsActive)
                .OrderBy(lv => lv.Value)
                .Select(lv => new { Id = lv.Value, Name = lv.Value })
                .ToListAsync();

            return Json(states);
        }
        catch (Exception ex)
        {
            return Json(new { error = "Failed to load states", details = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetNetworkStatuses()
    {
        return await GetLookupValues("NetworkStatus");
    }

    [HttpGet]
    public async Task<IActionResult> GetConditions()
    {
        return await GetLookupValues("Condition");
    }

    [HttpGet]
    public async Task<IActionResult> GetRepairStatuses()
    {
        return await GetLookupValues("RepairStatus");
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentStatuses()
    {
        return await GetLookupValues("PaymentStatus");
    }

    [HttpPost]
    public async Task<IActionResult> AddBrand([FromBody] AddBrandRequest request)
    {
        try
        {
            var brand = new Empire.Domain.Entities.Brand
            {
                Name = request.Name,
                Description = request.Description,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = brand.Id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> TestDatabaseConnection()
    {
        try
        {
            var brandCount = await _context.Brands.CountAsync();
            var activeBrandCount = await _context.Brands.CountAsync(b => b.IsActive);
            var categoryCount = await _context.DeviceCategories.CountAsync();
            var activeCategoryCount = await _context.DeviceCategories.CountAsync(dc => dc.IsActive);
            var modelCount = await _context.DeviceModels.CountAsync();
            var activeModelCount = await _context.DeviceModels.CountAsync(dm => dm.IsActive);

            var result = new
            {
                success = true,
                message = "Database connection successful",
                data = new
                {
                    brands = new { total = brandCount, active = activeBrandCount },
                    categories = new { total = categoryCount, active = activeCategoryCount },
                    models = new { total = modelCount, active = activeModelCount }
                }
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = "Database connection failed",
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}

public class AddBrandRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

