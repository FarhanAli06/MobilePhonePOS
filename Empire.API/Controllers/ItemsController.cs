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
    public class ItemsController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<ItemsController> _logger;

        public ItemsController(EmpireDbContext context, ILogger<ItemsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>Get all items for the current shop.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var shopId = GetCurrentShopId();
                var items = await _context.Items
                    .Where(i => i.ShopId == shopId && !i.IsDeleted)
                    .OrderBy(i => i.Name)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.IsActive,
                        i.CreatedDate,
                        i.ModifiedDate
                    })
                    .ToListAsync();
                return SuccessResponse(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving items");
                return StatusCode(500, "An error occurred while retrieving items");
            }
        }

        /// <summary>Get active items for the current shop.</summary>
        [HttpGet("active")]
        public async Task<IActionResult> GetActive()
        {
            try
            {
                var shopId = GetCurrentShopId();
                var items = await _context.Items
                    .Where(i => i.ShopId == shopId && i.IsActive && !i.IsDeleted)
                    .OrderBy(i => i.Name)
                    .Select(i => new { i.Id, i.Name })
                    .ToListAsync();
                return SuccessResponse(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active items");
                return StatusCode(500, "An error occurred while retrieving active items");
            }
        }

        /// <summary>Get a single item by ID (shop-scoped).</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var item = await _context.Items
                    .Where(i => i.Id == id && i.ShopId == shopId && !i.IsDeleted)
                    .Select(i => new
                    {
                        i.Id,
                        i.Name,
                        i.Description,
                        i.IsActive,
                        i.CreatedDate,
                        i.ModifiedDate
                    })
                    .FirstOrDefaultAsync();
                if (item == null)
                    return NotFoundResponse("Item not found");
                return SuccessResponse(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving item {Id}", id);
                return StatusCode(500, "An error occurred while retrieving the item");
            }
        }

        /// <summary>Check for duplicate item name in the current shop.</summary>
        [HttpGet("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] int? excludeId = null)
        {
            var shopId = GetCurrentShopId();
            var query = _context.Items.Where(i => i.ShopId == shopId && i.Name == name && !i.IsDeleted);
            if (excludeId.HasValue)
                query = query.Where(i => i.Id != excludeId.Value);
            var isDuplicate = await query.AnyAsync();
            return SuccessResponse(isDuplicate);
        }

        /// <summary>Create a new item for the current shop.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateItemApiRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid item data");
            try
            {
                var shopId = GetCurrentShopId();
                var item = new Item
                {
                    ShopId = shopId,
                    Name = request.Name,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow
                };
                _context.Items.Add(item);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Item {Name} created for shop {ShopId}", item.Name, shopId);
                return SuccessResponse(new { item.Id, item.Name, item.Description, item.IsActive, item.CreatedDate }, "Item created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating item");
                return StatusCode(500, "An error occurred while creating the item");
            }
        }

        /// <summary>Update an existing item (shop-scoped).</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateItemApiRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid item data");
            try
            {
                var shopId = GetCurrentShopId();
                var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.ShopId == shopId && !i.IsDeleted);
                if (item == null)
                    return NotFoundResponse("Item not found");
                item.Name = request.Name;
                item.Description = request.Description;
                item.IsActive = request.IsActive;
                item.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Item {Id} updated for shop {ShopId}", id, shopId);
                return SuccessResponse(new { item.Id, item.Name, item.Description, item.IsActive, item.ModifiedDate }, "Item updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating item {Id}", id);
                return StatusCode(500, "An error occurred while updating the item");
            }
        }

        /// <summary>Delete an item (shop-scoped).</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var shopId = GetCurrentShopId();
                var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == id && i.ShopId == shopId && !i.IsDeleted);
                if (item == null)
                    return NotFoundResponse("Item not found");
                // Soft delete
                item.IsDeleted = true;
                item.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Item {Id} deleted for shop {ShopId}", id, shopId);
                return SuccessResponse("Item deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting item {Id}", id);
                return StatusCode(500, "An error occurred while deleting the item");
            }
        }
    }

    public class CreateItemApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateItemApiRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
