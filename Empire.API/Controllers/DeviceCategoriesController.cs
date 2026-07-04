using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Empire.Infrastructure.Data;
using Empire.Domain.Entities;

namespace Empire.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceCategoriesController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<DeviceCategoriesController> _logger;

        public DeviceCategoriesController(EmpireDbContext context, ILogger<DeviceCategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shopId = GetCurrentShopId();
            var categories = await _context.DeviceCategories
                .Include(c => c.Styling)
                .Where(c => c.IsActive && c.ShopId == shopId)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.DisplayOrder,
                    c.IsActive,
                    c.CreatedDate,
                    Icon = c.Styling != null ? c.Styling.Icon : "devices",
                    Color = c.Styling != null ? c.Styling.Color : "#000000"
                })
                .ToListAsync();
            return SuccessResponse(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shopId = GetCurrentShopId();
            var category = await _context.DeviceCategories
                .Include(c => c.Styling)
                .Where(c => c.Id == id && c.ShopId == shopId)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.DisplayOrder,
                    c.IsActive,
                    c.CreatedDate,
                    Icon = c.Styling != null ? c.Styling.Icon : "devices",
                    Color = c.Styling != null ? c.Styling.Color : "#000000"
                })
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFoundResponse("Device category not found");
            return SuccessResponse(category);
        }

        [HttpGet("by-brand/{brandId}")]
        public async Task<IActionResult> GetByBrand(int brandId)
        {
            var shopId = GetCurrentShopId();
            var categories = await _context.DeviceModels
                .Where(dm => dm.BrandId == brandId && dm.IsActive && dm.ShopId == shopId)
                .Select(dm => dm.DeviceCategory)
                .Distinct()
                .Include(c => c.Styling)
                .Where(c => c.IsActive && c.ShopId == shopId)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    Icon = c.Styling != null ? c.Styling.Icon : "devices",
                    Color = c.Styling != null ? c.Styling.Color : "#000000"
                })
                .ToListAsync();
            return SuccessResponse(categories);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var shopId = GetCurrentShopId();
            var categories = await _context.DeviceCategories
                .Where(c => c.IsActive && c.ShopId == shopId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return SuccessResponse(categories);
        }

        [HttpGet("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] int? brandId = null, [FromQuery] int? excludeId = null)
        {
            var shopId = GetCurrentShopId();
            var query = _context.DeviceCategories.Where(c => c.ShopId == shopId && c.Name == name);
            if (excludeId.HasValue)
                query = query.Where(c => c.Id != excludeId.Value);
            var isDuplicate = await query.AnyAsync();
            return SuccessResponse(isDuplicate);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDeviceCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid device category data");

            var shopId = GetCurrentShopId();
            var category = new DeviceCategory
            {
                ShopId = shopId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                
                ModifiedDate = DateTime.UtcNow
            };

            _context.DeviceCategories.Add(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("DeviceCategory {Name} created for shop {ShopId}", category.Name, shopId);
            return SuccessResponse(new { category.Id, category.Name }, "Device category created successfully");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid device category data");

            var shopId = GetCurrentShopId();
            var category = await _context.DeviceCategories.FirstOrDefaultAsync(c => c.Id == id && c.ShopId == shopId);
            if (category == null)
                return NotFoundResponse("Device category not found");

            category.Name = request.Name;
            category.Description = request.Description ?? string.Empty;
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;
            category.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("DeviceCategory {Id} updated for shop {ShopId}", id, shopId);
            return SuccessResponse(new { category.Id, category.Name }, "Device category updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var shopId = GetCurrentShopId();
            var category = await _context.DeviceCategories.FirstOrDefaultAsync(c => c.Id == id && c.ShopId == shopId);
            if (category == null)
                return NotFoundResponse("Device category not found");

            var inUse = await _context.DeviceModels.AnyAsync(dm => dm.DeviceCategoryId == id && dm.ShopId == shopId);
            if (inUse)
                return ErrorResponse("Cannot delete device category that has associated device models");

            _context.DeviceCategories.Remove(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("DeviceCategory {Id} deleted for shop {ShopId}", id, shopId);
            return SuccessResponse("Device category deleted successfully");
        }
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
}
