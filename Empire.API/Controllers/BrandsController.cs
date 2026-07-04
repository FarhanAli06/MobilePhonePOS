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
    public class BrandsController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<BrandsController> _logger;

        public BrandsController(EmpireDbContext context, ILogger<BrandsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shopId = GetCurrentShopId();
            var brands = await _context.Brands
                .Include(b => b.Styling)
                .Where(b => b.IsActive && b.ShopId == shopId)
                .OrderBy(b => b.DisplayOrder)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Description,
                    b.DisplayOrder,
                    b.IsActive,
                    b.CreatedDate,
                    Icon = b.Styling != null ? b.Styling.Icon : "devices",
                    Color = b.Styling != null ? b.Styling.Color : "#000000"
                })
                .ToListAsync();
            return SuccessResponse(brands);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shopId = GetCurrentShopId();
            var brand = await _context.Brands
                .Include(b => b.Styling)
                .Where(b => b.Id == id && b.ShopId == shopId)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Description,
                    b.DisplayOrder,
                    b.IsActive,
                    b.CreatedDate,
                    Icon = b.Styling != null ? b.Styling.Icon : "devices",
                    Color = b.Styling != null ? b.Styling.Color : "#000000"
                })
                .FirstOrDefaultAsync();
            if (brand == null)
                return NotFoundResponse("Brand not found");
            return SuccessResponse(brand);
        }

        [HttpGet("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] int? excludeId = null)
        {
            var shopId = GetCurrentShopId();
            var query = _context.Brands.Where(b => b.ShopId == shopId && b.Name == name);
            if (excludeId.HasValue)
                query = query.Where(b => b.Id != excludeId.Value);
            var isDuplicate = await query.AnyAsync();
            return SuccessResponse(isDuplicate);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var shopId = GetCurrentShopId();
            var brands = await _context.Brands
                .Where(b => b.IsActive && b.ShopId == shopId)
                .OrderBy(b => b.DisplayOrder)
                .ThenBy(b => b.Name)
                .Select(b => new { b.Id, b.Name })
                .ToListAsync();
            return SuccessResponse(brands);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBrandRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid brand data");

            var shopId = GetCurrentShopId();
            var brand = new Brand
            {
                ShopId = shopId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                
                ModifiedDate = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Brand {Name} created for shop {ShopId}", brand.Name, shopId);
            return SuccessResponse(new { brand.Id, brand.Name }, "Brand created successfully");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateBrandRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid brand data");

            var shopId = GetCurrentShopId();
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id && b.ShopId == shopId);
            if (brand == null)
                return NotFoundResponse("Brand not found");

            brand.Name = request.Name;
            brand.Description = request.Description ?? string.Empty;
            brand.DisplayOrder = request.DisplayOrder;
            brand.IsActive = request.IsActive;
            brand.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Brand {Id} updated for shop {ShopId}", id, shopId);
            return SuccessResponse(new { brand.Id, brand.Name }, "Brand updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var shopId = GetCurrentShopId();
            var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id && b.ShopId == shopId);
            if (brand == null)
                return NotFoundResponse("Brand not found");

            var inUse = await _context.DeviceModels.AnyAsync(dm => dm.BrandId == id && dm.ShopId == shopId);
            if (inUse)
                return ErrorResponse("Cannot delete brand that has associated device models");

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Brand {Id} deleted for shop {ShopId}", id, shopId);
            return SuccessResponse("Brand deleted successfully");
        }
    }

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
}
