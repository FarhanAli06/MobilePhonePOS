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
    public class InventoryCategoriesController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<InventoryCategoriesController> _logger;

        public InventoryCategoriesController(EmpireDbContext context, ILogger<InventoryCategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var shopId = GetCurrentShopId();
            var categories = await _context.InventoryCategories
                .Where(c => c.ShopId == shopId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.DisplayOrder,
                    c.IsActive,
                    c.CreatedDate
                })
                .ToListAsync();
            return SuccessResponse(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var shopId = GetCurrentShopId();
            var category = await _context.InventoryCategories
                .Where(c => c.Id == id && c.ShopId == shopId)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.DisplayOrder,
                    c.IsActive,
                    c.CreatedDate
                })
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFoundResponse("Inventory category not found");
            return SuccessResponse(category);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            var shopId = GetCurrentShopId();
            var categories = await _context.InventoryCategories
                .Where(c => c.IsActive && c.ShopId == shopId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            return SuccessResponse(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInventoryCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid inventory category data");

            var shopId = GetCurrentShopId();
            var category = new InventoryCategory
            {
                ShopId = shopId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                
                ModifiedDate = DateTime.UtcNow
            };

            _context.InventoryCategories.Add(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("InventoryCategory {Name} created for shop {ShopId}", category.Name, shopId);
            return SuccessResponse(new { category.Id, category.Name }, "Inventory category created successfully");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInventoryCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid inventory category data");

            var shopId = GetCurrentShopId();
            var category = await _context.InventoryCategories.FirstOrDefaultAsync(c => c.Id == id && c.ShopId == shopId);
            if (category == null)
                return NotFoundResponse("Inventory category not found");

            category.Name = request.Name;
            category.Description = request.Description ?? string.Empty;
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;
            category.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("InventoryCategory {Id} updated for shop {ShopId}", id, shopId);
            return SuccessResponse(new { category.Id, category.Name }, "Inventory category updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var shopId = GetCurrentShopId();
            var category = await _context.InventoryCategories.FirstOrDefaultAsync(c => c.Id == id && c.ShopId == shopId);
            if (category == null)
                return NotFoundResponse("Inventory category not found");

            var inUse = await _context.InventoryItems.AnyAsync(i => i.InventoryCategoryId == id);
            if (inUse)
                return ErrorResponse("Cannot delete inventory category that has associated inventory items");

            _context.InventoryCategories.Remove(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("InventoryCategory {Id} deleted for shop {ShopId}", id, shopId);
            return SuccessResponse("Inventory category deleted successfully");
        }
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
}
