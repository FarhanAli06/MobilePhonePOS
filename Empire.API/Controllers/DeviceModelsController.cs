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
    public class DeviceModelsController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<DeviceModelsController> _logger;

        public DeviceModelsController(EmpireDbContext context, ILogger<DeviceModelsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shopId = GetCurrentShopId();
            var models = await _context.DeviceModels
                .Include(m => m.Styling)
                .Include(m => m.Brand)
                .Include(m => m.DeviceCategory)
                .Where(m => m.IsActive && m.ShopId == shopId)
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.ModelNumber,
                    m.Year,
                    m.Description,
                    m.BrandId,
                    BrandName = m.Brand.Name,
                    m.DeviceCategoryId,
                    CategoryName = m.DeviceCategory.Name,
                    m.DisplayOrder,
                    m.IsActive,
                    m.CreatedDate,
                    Icon = m.Styling != null ? m.Styling.Icon : "phone_iphone",
                    Color = m.Styling != null ? m.Styling.Color : "#000000"
                })
                .ToListAsync();
            return SuccessResponse(models);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shopId = GetCurrentShopId();
            var model = await _context.DeviceModels
                .Include(m => m.Styling)
                .Include(m => m.Brand)
                .Include(m => m.DeviceCategory)
                .Where(m => m.Id == id && m.ShopId == shopId)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.ModelNumber,
                    m.Year,
                    m.Description,
                    m.BrandId,
                    BrandName = m.Brand.Name,
                    m.DeviceCategoryId,
                    CategoryName = m.DeviceCategory.Name,
                    m.DisplayOrder,
                    m.IsActive,
                    m.CreatedDate,
                    Icon = m.Styling != null ? m.Styling.Icon : "phone_iphone",
                    Color = m.Styling != null ? m.Styling.Color : "#000000"
                })
                .FirstOrDefaultAsync();
            if (model == null)
                return NotFoundResponse("Device model not found");
            return SuccessResponse(model);
        }

        [HttpGet("by-brand-category")]
        public async Task<IActionResult> GetByBrandAndCategory([FromQuery] int brandId, [FromQuery] int categoryId)
        {
            var shopId = GetCurrentShopId();
            var models = await _context.DeviceModels
                .Include(m => m.Styling)
                .Where(m => m.BrandId == brandId && m.DeviceCategoryId == categoryId && m.IsActive && m.ShopId == shopId)
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new
                {
                    m.Id,
                    m.Name,
                    m.ModelNumber,
                    m.Year,
                    m.Description,
                    Icon = m.Styling != null ? m.Styling.Icon : "phone_iphone",
                    Color = m.Styling != null ? m.Styling.Color : "#000000"
                })
                .ToListAsync();
            return SuccessResponse(models);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateDeviceModelRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid device model data");

            var shopId = GetCurrentShopId();

            // Validate brand and category belong to this shop
            var brandExists = await _context.Brands.AnyAsync(b => b.Id == request.BrandId && b.ShopId == shopId);
            if (!brandExists)
                return ErrorResponse("Brand not found or does not belong to this shop");

            var categoryExists = await _context.DeviceCategories.AnyAsync(c => c.Id == request.DeviceCategoryId && c.ShopId == shopId);
            if (!categoryExists)
                return ErrorResponse("Device category not found or does not belong to this shop");

            var model = new DeviceModel
            {
                ShopId = shopId,
                BrandId = request.BrandId,
                DeviceCategoryId = request.DeviceCategoryId,
                Name = request.Name,
                ModelNumber = request.ModelNumber ?? string.Empty,
                Year = request.Year,
                Description = request.Description ?? string.Empty,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                
                ModifiedDate = DateTime.UtcNow
            };

            _context.DeviceModels.Add(model);
            await _context.SaveChangesAsync();
            _logger.LogInformation("DeviceModel {Name} created for shop {ShopId}", model.Name, shopId);
            return SuccessResponse(new { model.Id, model.Name }, "Device model created successfully");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceModelRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid device model data");

            var shopId = GetCurrentShopId();
            var model = await _context.DeviceModels.FirstOrDefaultAsync(m => m.Id == id && m.ShopId == shopId);
            if (model == null)
                return NotFoundResponse("Device model not found");

            model.BrandId = request.BrandId;
            model.DeviceCategoryId = request.DeviceCategoryId;
            model.Name = request.Name;
            model.ModelNumber = request.ModelNumber ?? string.Empty;
            model.Year = request.Year;
            model.Description = request.Description ?? string.Empty;
            model.DisplayOrder = request.DisplayOrder;
            model.IsActive = request.IsActive;
            model.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("DeviceModel {Id} updated for shop {ShopId}", id, shopId);
            return SuccessResponse(new { model.Id, model.Name }, "Device model updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var shopId = GetCurrentShopId();
            var model = await _context.DeviceModels.FirstOrDefaultAsync(m => m.Id == id && m.ShopId == shopId);
            if (model == null)
                return NotFoundResponse("Device model not found");

            _context.DeviceModels.Remove(model);
            await _context.SaveChangesAsync();
            _logger.LogInformation("DeviceModel {Id} deleted for shop {ShopId}", id, shopId);
            return SuccessResponse("Device model deleted successfully");
        }
    }

    public class CreateDeviceModelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ModelNumber { get; set; }
        public int? Year { get; set; }
        public string? Description { get; set; }
        public int BrandId { get; set; }
        public int DeviceCategoryId { get; set; }
        public int DisplayOrder { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateDeviceModelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? ModelNumber { get; set; }
        public int? Year { get; set; }
        public string? Description { get; set; }
        public int BrandId { get; set; }
        public int DeviceCategoryId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
