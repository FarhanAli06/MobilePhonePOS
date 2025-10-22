using Microsoft.AspNetCore.Mvc;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Empire.Web.Authorization;
using Empire.Domain.Entities;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class LookupManagementController : Controller
{
    private readonly EmpireDbContext _context;

    public LookupManagementController(EmpireDbContext context)
    {
        _context = context;
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
            var brands = await _context.Brands
                .OrderBy(b => b.DisplayOrder)
                .ThenBy(b => b.Name)
                .Select(b => new
                {
                    Id = b.Id,
                    Name = b.Name,
                    Description = b.Description,
                    DisplayOrder = b.DisplayOrder,
                    IsActive = b.IsActive,
                    CreatedDate = b.CreatedDate
                })
                .ToListAsync();

            return Json(new { success = true, data = brands });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var brand = new Brand
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Brand created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] UpdateBrandRequest request)
    {
        try
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return Json(new { success = false, message = "Brand not found" });
            }

            brand.Name = request.Name.Trim();
            brand.Description = request.Description?.Trim();
            brand.DisplayOrder = request.DisplayOrder;
            brand.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Brand updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        try
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
            {
                return Json(new { success = false, message = "Brand not found" });
            }

            // Check if brand is used in device models
            var hasModels = await _context.DeviceModels.AnyAsync(dm => dm.BrandId == id);
            if (hasModels)
            {
                return Json(new { success = false, message = "Cannot delete brand that has device models" });
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Brand deleted successfully" });
        }
        catch (Exception ex)
        {
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
            var categories = await _context.DeviceCategories
                .OrderBy(dc => dc.DisplayOrder)
                .ThenBy(dc => dc.Name)
                .Select(dc => new
                {
                    Id = dc.Id,
                    Name = dc.Name,
                    Description = dc.Description,
                    DisplayOrder = dc.DisplayOrder,
                    IsActive = dc.IsActive,
                    CreatedDate = dc.CreatedDate
                })
                .ToListAsync();

            return Json(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeviceCategory([FromBody] CreateDeviceCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var category = new DeviceCategory
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.DeviceCategories.Add(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Device category created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateDeviceCategory(int id, [FromBody] UpdateDeviceCategoryRequest request)
    {
        try
        {
            var category = await _context.DeviceCategories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Device category not found" });
            }

            category.Name = request.Name.Trim();
            category.Description = request.Description?.Trim();
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Device category updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteDeviceCategory(int id)
    {
        try
        {
            var category = await _context.DeviceCategories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Device category not found" });
            }

            // Check if category is used in device models
            var hasModels = await _context.DeviceModels.AnyAsync(dm => dm.DeviceCategoryId == id);
            if (hasModels)
            {
                return Json(new { success = false, message = "Cannot delete device category that has models" });
            }

            _context.DeviceCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Device category deleted successfully" });
        }
        catch (Exception ex)
        {
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
            var models = await _context.DeviceModels
                .Include(dm => dm.DeviceCategory)
                .Include(dm => dm.Brand)
                .OrderBy(dm => dm.Brand.Name)
                .ThenBy(dm => dm.DeviceCategory.Name)
                .ThenBy(dm => dm.DisplayOrder)
                .ThenBy(dm => dm.Name)
                .Select(dm => new
                {
                    Id = dm.Id,
                    Name = dm.Name,
                    Description = dm.Description,
                    DeviceCategoryId = dm.DeviceCategoryId,
                    DeviceCategoryName = dm.DeviceCategory.Name,
                    BrandId = dm.BrandId,
                    BrandName = dm.Brand.Name,
                    DisplayOrder = dm.DisplayOrder,
                    IsActive = dm.IsActive,
                    CreatedDate = dm.CreatedDate
                })
                .ToListAsync();

            return Json(new { success = true, data = models });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateDeviceModel([FromBody] CreateDeviceModelRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var model = new DeviceModel
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                BrandId = request.BrandId,
                DeviceCategoryId = request.DeviceCategoryId,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.DeviceModels.Add(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Device model created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateDeviceModel(int id, [FromBody] UpdateDeviceModelRequest request)
    {
        try
        {
            var model = await _context.DeviceModels.FindAsync(id);
            if (model == null)
            {
                return Json(new { success = false, message = "Device model not found" });
            }

            model.Name = request.Name.Trim();
            model.Description = request.Description?.Trim();
            model.BrandId = request.BrandId;
            model.DeviceCategoryId = request.DeviceCategoryId;
            model.DisplayOrder = request.DisplayOrder;
            model.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Device model updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteDeviceModel(int id)
    {
        try
        {
            var model = await _context.DeviceModels.FindAsync(id);
            if (model == null)
            {
                return Json(new { success = false, message = "Device model not found" });
            }

            // Check if model is used in devices or inventory
            var hasDevices = await _context.Devices.AnyAsync(d => d.DeviceModelId == id);
            var hasInventory = await _context.InventoryItems.AnyAsync(i => i.DeviceModelId == id);
            
            if (hasDevices || hasInventory)
            {
                return Json(new { success = false, message = "Cannot delete device model that is used in devices or inventory" });
            }

            _context.DeviceModels.Remove(model);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Device model deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Lookup Values Management

    [HttpGet]
    public async Task<IActionResult> GetLookupValues()
    {
        try
        {
            var lookupValues = await _context.LookupValues
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
                })
                .ToListAsync();

            return Json(new { success = true, data = lookupValues });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateLookupValue([FromBody] CreateLookupValueRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            // If CategoryId is null or zero, try to find it by category name
            int? categoryId = request.CategoryId;
            if (categoryId == null || categoryId == 0)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Category.Trim().ToLower() && c.IsActive);
                if (category != null)
                {
                    categoryId = category.Id;
                }
            }

            var lookupValue = new LookupValue
            {
                Category = request.Category.Trim(),
                Value = request.Value.Trim(),
                Description = request.Description?.Trim() ?? "",
                CategoryId = categoryId,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                ColorCode = request.ColorCode?.Trim() ?? "",
                CreatedDate = DateTime.UtcNow
            };

            _context.LookupValues.Add(lookupValue);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Lookup value created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateLookupValue(int id, [FromBody] UpdateLookupValueRequest request)
    {
        try
        {
            var lookupValue = await _context.LookupValues.FindAsync(id);
            if (lookupValue == null)
            {
                return Json(new { success = false, message = "Lookup value not found" });
            }

            // If CategoryId is null or zero, try to find it by category name
            int? categoryId = request.CategoryId;
            if (categoryId == null || categoryId == 0)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == request.Category.Trim().ToLower() && c.IsActive);
                if (category != null)
                {
                    categoryId = category.Id;
                }
            }

            lookupValue.Category = request.Category.Trim();
            lookupValue.Value = request.Value.Trim();
            lookupValue.Description = request.Description?.Trim() ?? "";
            lookupValue.ColorCode = request.ColorCode?.Trim() ?? "";
            lookupValue.CategoryId = categoryId;
            lookupValue.DisplayOrder = request.DisplayOrder;
            lookupValue.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Lookup value updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteLookupValue(int id)
    {
        try
        {
            var lookupValue = await _context.LookupValues.FindAsync(id);
            if (lookupValue == null)
            {
                return Json(new { success = false, message = "Lookup value not found" });
            }

            _context.LookupValues.Remove(lookupValue);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Lookup value deleted successfully" });
        }
        catch (Exception ex)
        {
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
            var categories = await _context.InventoryCategories
                .OrderBy(ic => ic.DisplayOrder)
                .ThenBy(ic => ic.Name)
                .Select(ic => new
                {
                    Id = ic.Id,
                    Name = ic.Name,
                    Description = ic.Description,
                    DisplayOrder = ic.DisplayOrder,
                    IsActive = ic.IsActive,
                    CreatedDate = ic.CreatedDate
                })
                .ToListAsync();

            return Json(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateInventoryCategory([FromBody] CreateInventoryCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var category = new InventoryCategory
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.InventoryCategories.Add(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Inventory category created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateInventoryCategory(int id, [FromBody] UpdateInventoryCategoryRequest request)
    {
        try
        {
            var category = await _context.InventoryCategories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Inventory category not found" });
            }

            category.Name = request.Name.Trim();
            category.Description = request.Description?.Trim();
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Inventory category updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteInventoryCategory(int id)
    {
        try
        {
            var category = await _context.InventoryCategories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Inventory category not found" });
            }

            // Check if category is used in inventory
            var hasInventory = await _context.InventoryItems.AnyAsync(i => i.InventoryCategoryId == id);
            if (hasInventory)
            {
                return Json(new { success = false, message = "Cannot delete inventory category that has items" });
            }

            _context.InventoryCategories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Inventory category deleted successfully" });
        }
        catch (Exception ex)
        {
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
            var query = _context.Categories.AsQueryable();
            
            if (!string.IsNullOrEmpty(categoryType))
            {
                query = query.Where(c => c.CategoryType == categoryType);
            }
            
            var categories = await query
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
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                    DisplayOrder = c.DisplayOrder,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    SubCategoriesCount = c.SubCategories.Count,
                    LookupValuesCount = c.LookupValues.Count
                })
                .ToListAsync();

            return Json(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var category = new Category
            {
                Name = request.Name.Trim(),
                Description = request.Description?.Trim(),
                CategoryType = request.CategoryType.Trim(),
                ParentCategoryId = request.ParentCategoryId,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                CreatedDate = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Category created successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryRequest request)
    {
        try
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found" });
            }

            category.Name = request.Name.Trim();
            category.Description = request.Description?.Trim();
            category.CategoryType = request.CategoryType.Trim();
            category.ParentCategoryId = request.ParentCategoryId;
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Category updated successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return Json(new { success = false, message = "Category not found" });
            }

            // Check if category has subcategories
            var hasSubCategories = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id);
            if (hasSubCategories)
            {
                return Json(new { success = false, message = "Cannot delete category that has subcategories" });
            }

            // Check if category has lookup values
            var hasLookupValues = await _context.LookupValues.AnyAsync(lv => lv.CategoryId == id);
            if (hasLookupValues)
            {
                return Json(new { success = false, message = "Cannot delete category that has lookup values" });
            }

            // Check if category is used in inventory items
            var hasInventoryItems = await _context.InventoryItems.AnyAsync(i => i.InventoryCategoryId == id);
            if (hasInventoryItems)
            {
                return Json(new { success = false, message = "Cannot delete category that has inventory items" });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Category deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion
}

#region Request Models

public class CreateBrandRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdateBrandRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateDeviceCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdateDeviceCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateDeviceModelRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdateDeviceModelRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int BrandId { get; set; }
    public int DeviceCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateLookupValueRequest
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorCode { get; set; }
    public int? CategoryId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdateLookupValueRequest
{
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ColorCode { get; set; }
    public int? CategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateInventoryCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdateInventoryCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryType { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string CategoryType { get; set; } = string.Empty;
    public int? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

#endregion

