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
    public class CategoriesController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(EmpireDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? categoryType = null)
        {
            var shopId = GetCurrentShopId();
            var query = _context.Categories
                .Include(c => c.ParentCategory)
                .Where(c => c.ShopId == shopId);

            if (!string.IsNullOrEmpty(categoryType))
                query = query.Where(c => c.CategoryType == categoryType);

            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CategoryType,
                    c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
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
            var category = await _context.Categories
                .Include(c => c.ParentCategory)
                .Where(c => c.Id == id && c.ShopId == shopId)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.CategoryType,
                    c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.Name : null,
                    c.DisplayOrder,
                    c.IsActive,
                    c.CreatedDate
                })
                .FirstOrDefaultAsync();
            if (category == null)
                return NotFoundResponse("Category not found");
            return SuccessResponse(category);
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] string? categoryType = null)
        {
            var shopId = GetCurrentShopId();
            var query = _context.Categories.Where(c => c.IsActive && c.ShopId == shopId);
            if (!string.IsNullOrEmpty(categoryType))
                query = query.Where(c => c.CategoryType == categoryType);

            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.CategoryType })
                .ToListAsync();
            return SuccessResponse(categories);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid category data");

            var shopId = GetCurrentShopId();
            var category = new Category
            {
                ShopId = shopId,
                Name = request.Name,
                Description = request.Description ?? string.Empty,
                CategoryType = request.CategoryType,
                ParentCategoryId = request.ParentCategoryId,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                
                ModifiedDate = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Category {Name} created for shop {ShopId}", category.Name, shopId);
            return SuccessResponse(new { category.Id, category.Name }, "Category created successfully");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryRequest request)
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse("Invalid category data");

            var shopId = GetCurrentShopId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.ShopId == shopId);
            if (category == null)
                return NotFoundResponse("Category not found");

            category.Name = request.Name;
            category.Description = request.Description ?? string.Empty;
            category.CategoryType = request.CategoryType;
            category.ParentCategoryId = request.ParentCategoryId;
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;
            category.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Category {Id} updated for shop {ShopId}", id, shopId);
            return SuccessResponse(new { category.Id, category.Name }, "Category updated successfully");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var shopId = GetCurrentShopId();
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id && c.ShopId == shopId);
            if (category == null)
                return NotFoundResponse("Category not found");

            var hasSubCategories = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id && c.ShopId == shopId);
            if (hasSubCategories)
                return ErrorResponse("Cannot delete category that has sub-categories");

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Category {Id} deleted for shop {ShopId}", id, shopId);
            return SuccessResponse("Category deleted successfully");
        }
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
}
