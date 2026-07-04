using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Empire.Infrastructure.Data;
using Empire.Domain.Entities;

namespace Empire.API.Controllers
{
    [Authorize]
    public class StylingController : BaseApiController
    {
        private readonly EmpireDbContext _context;
        private readonly ILogger<StylingController> _logger;

        public StylingController(EmpireDbContext context, ILogger<StylingController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET /api/styling
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var stylings = await _context.Stylings
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Description,
                        s.Icon,
                        s.Color,
                        s.TextColor,
                        s.BadgeVariant,
                        s.IsActive,
                        s.DisplayOrder,
                        s.CreatedAt,
                        s.UpdatedAt
                    })
                    .ToListAsync();

                return SuccessResponse(stylings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stylings");
                return StatusCode(500, ErrorResponse("Error retrieving stylings"));
            }
        }

        // GET /api/styling/selections  — lightweight list for dropdowns
        [HttpGet("selections")]
        public async Task<IActionResult> GetSelections()
        {
            try
            {
                var stylings = await _context.Stylings
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        s.Icon,
                        s.Color,
                        s.TextColor,
                        s.BadgeVariant
                    })
                    .ToListAsync();

                return SuccessResponse(stylings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting styling selections");
                return StatusCode(500, ErrorResponse("Error retrieving styling selections"));
            }
        }

        // GET /api/styling/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var styling = await _context.Stylings.FindAsync(id);
                if (styling == null)
                    return NotFound(ErrorResponse("Styling not found"));

                return SuccessResponse(new
                {
                    styling.Id,
                    styling.Name,
                    styling.Description,
                    styling.Icon,
                    styling.Color,
                    styling.TextColor,
                    styling.BadgeVariant,
                    styling.IsActive,
                    styling.DisplayOrder,
                    styling.CreatedAt,
                    styling.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting styling {Id}", id);
                return StatusCode(500, ErrorResponse("Error retrieving styling"));
            }
        }

        // GET /api/styling/check-duplicate?name=X&excludeId=Y
        [HttpGet("check-duplicate")]
        public async Task<IActionResult> CheckDuplicate([FromQuery] string name, [FromQuery] int? excludeId = null)
        {
            try
            {
                var exists = await _context.Stylings
                    .AnyAsync(s => s.Name.ToLower() == name.ToLower() &&
                                   (!excludeId.HasValue || s.Id != excludeId.Value));
                return SuccessResponse(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate styling name");
                return StatusCode(500, ErrorResponse("Error checking duplicate"));
            }
        }

        // POST /api/styling
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStylingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ErrorResponse("Invalid request data"));

                var duplicate = await _context.Stylings
                    .AnyAsync(s => s.Name.ToLower() == request.Name.ToLower());
                if (duplicate)
                    return Conflict(ErrorResponse($"A styling named '{request.Name}' already exists"));

                var styling = new Styling
                {
                    Name        = request.Name,
                    Description = request.Description,
                    Icon        = request.Icon ?? "circle",
                    Color       = request.Color ?? "#6c757d",
                    TextColor   = request.TextColor ?? "#ffffff",
                    BadgeVariant = request.BadgeVariant ?? "secondary",
                    IsActive    = request.IsActive,
                    DisplayOrder = request.DisplayOrder,
                    CreatedAt   = DateTime.UtcNow
                };

                _context.Stylings.Add(styling);
                await _context.SaveChangesAsync();

                return SuccessResponse(new { styling.Id, styling.Name, styling.Icon, styling.Color, styling.TextColor, styling.BadgeVariant });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating styling");
                return StatusCode(500, ErrorResponse("Error creating styling"));
            }
        }

        // PUT /api/styling/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStylingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ErrorResponse("Invalid request data"));

                var styling = await _context.Stylings.FindAsync(id);
                if (styling == null)
                    return NotFound(ErrorResponse("Styling not found"));

                var duplicate = await _context.Stylings
                    .AnyAsync(s => s.Name.ToLower() == request.Name.ToLower() && s.Id != id);
                if (duplicate)
                    return Conflict(ErrorResponse($"A styling named '{request.Name}' already exists"));

                styling.Name         = request.Name;
                styling.Description  = request.Description;
                styling.Icon         = request.Icon ?? styling.Icon;
                styling.Color        = request.Color ?? styling.Color;
                styling.TextColor    = request.TextColor ?? styling.TextColor;
                styling.BadgeVariant = request.BadgeVariant ?? styling.BadgeVariant;
                styling.IsActive     = request.IsActive;
                styling.DisplayOrder = request.DisplayOrder;
                styling.UpdatedAt    = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return SuccessResponse(new { styling.Id, styling.Name, styling.Icon, styling.Color, styling.TextColor, styling.BadgeVariant });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating styling {Id}", id);
                return StatusCode(500, ErrorResponse("Error updating styling"));
            }
        }

        // DELETE /api/styling/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var styling = await _context.Stylings.FindAsync(id);
                if (styling == null)
                    return NotFound(ErrorResponse("Styling not found"));

                // Unlink all entities that reference this styling
                await _context.Brands
                    .Where(b => b.StylingId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.StylingId, (int?)null));
                await _context.DeviceCategories
                    .Where(c => c.StylingId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.StylingId, (int?)null));
                await _context.DeviceModels
                    .Where(m => m.StylingId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.StylingId, (int?)null));

                _context.Stylings.Remove(styling);
                await _context.SaveChangesAsync();

                return SuccessResponse(new { message = "Styling deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting styling {Id}", id);
                return StatusCode(500, ErrorResponse("Error deleting styling"));
            }
        }

        // ── Assignment endpoints ─────────────────────────────────────────────────

        // GET /api/styling/assignments  — returns all brands/categories/models with their current styling
        [HttpGet("assignments")]
        public async Task<IActionResult> GetAssignments()
        {
            try
            {
                var brands = await _context.Brands
                    .OrderBy(b => b.Name)
                    .Select(b => new { b.Id, b.Name, b.StylingId })
                    .ToListAsync();

                var categories = await _context.DeviceCategories
                    .OrderBy(c => c.Name)
                    .Select(c => new { c.Id, c.Name, c.StylingId })
                    .ToListAsync();

                var models = await _context.DeviceModels
                    .OrderBy(m => m.Name)
                    .Select(m => new { m.Id, m.Name, m.StylingId })
                    .ToListAsync();

                return SuccessResponse(new { brands, categories, models });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting styling assignments");
                return StatusCode(500, ErrorResponse("Error retrieving assignments"));
            }
        }

        // PUT /api/styling/assign/brand/{brandId}
        [HttpPut("assign/brand/{brandId}")]
        public async Task<IActionResult> AssignToBrand(int brandId, [FromBody] AssignStylingRequest request)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(brandId);
                if (brand == null) return NotFound(ErrorResponse("Brand not found"));

                brand.StylingId = request.StylingId;
                await _context.SaveChangesAsync();
                return SuccessResponse(new { message = "Styling assigned to brand" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning styling to brand {BrandId}", brandId);
                return StatusCode(500, ErrorResponse("Error assigning styling"));
            }
        }

        // PUT /api/styling/assign/category/{categoryId}
        [HttpPut("assign/category/{categoryId}")]
        public async Task<IActionResult> AssignToCategory(int categoryId, [FromBody] AssignStylingRequest request)
        {
            try
            {
                var category = await _context.DeviceCategories.FindAsync(categoryId);
                if (category == null) return NotFound(ErrorResponse("Device category not found"));

                category.StylingId = request.StylingId;
                await _context.SaveChangesAsync();
                return SuccessResponse(new { message = "Styling assigned to category" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning styling to category {CategoryId}", categoryId);
                return StatusCode(500, ErrorResponse("Error assigning styling"));
            }
        }

        // PUT /api/styling/assign/model/{modelId}
        [HttpPut("assign/model/{modelId}")]
        public async Task<IActionResult> AssignToModel(int modelId, [FromBody] AssignStylingRequest request)
        {
            try
            {
                var model = await _context.DeviceModels.FindAsync(modelId);
                if (model == null) return NotFound(ErrorResponse("Device model not found"));

                model.StylingId = request.StylingId;
                await _context.SaveChangesAsync();
                return SuccessResponse(new { message = "Styling assigned to model" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning styling to model {ModelId}", modelId);
                return StatusCode(500, ErrorResponse("Error assigning styling"));
            }
        }
    }

    // ── Request DTOs ─────────────────────────────────────────────────────────────

    public class CreateStylingRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        public string? BadgeVariant { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateStylingRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string? TextColor { get; set; }
        public string? BadgeVariant { get; set; }
        public bool IsActive { get; set; } = true;
        public int DisplayOrder { get; set; } = 0;
    }

    public class AssignStylingRequest
    {
        public int? StylingId { get; set; }
    }
}
