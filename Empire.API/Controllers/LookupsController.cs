using Empire.Application.DTOs.Lookup;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Empire.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LookupsController : BaseApiController
{
    private readonly ILookupService _lookupService;
    private readonly EmpireDbContext _context;
    private readonly ILogger<LookupsController> _logger;

    // Static list of US states returned by GET /api/lookups/states
    private static readonly List<string> _usStates = new()
    {
        "Alabama","Alaska","Arizona","Arkansas","California","Colorado","Connecticut",
        "Delaware","Florida","Georgia","Hawaii","Idaho","Illinois","Indiana","Iowa",
        "Kansas","Kentucky","Louisiana","Maine","Maryland","Massachusetts","Michigan",
        "Minnesota","Mississippi","Missouri","Montana","Nebraska","Nevada",
        "New Hampshire","New Jersey","New Mexico","New York","North Carolina",
        "North Dakota","Ohio","Oklahoma","Oregon","Pennsylvania","Rhode Island",
        "South Carolina","South Dakota","Tennessee","Texas","Utah","Vermont",
        "Virginia","Washington","West Virginia","Wisconsin","Wyoming"
    };

    public LookupsController(ILookupService lookupService, EmpireDbContext context, ILogger<LookupsController> logger)
    {
        _lookupService = lookupService;
        _context = context;
        _logger = logger;
    }

    /// <summary>Get all lookup values for the current shop (plus global values).</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var shopId = GetCurrentShopId();
            var lookups = await _lookupService.GetAllAsync(shopId);
            return SuccessResponse(lookups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all lookup values");
            return StatusCode(500, "An error occurred while retrieving lookup values");
        }
    }

    /// <summary>Get lookup values by category for the current shop (plus global values).</summary>
    [HttpGet("category/{category}")]
    public async Task<IActionResult> GetByCategory(string category)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var lookups = await _lookupService.GetByCategoryAsync(category, shopId);
            return SuccessResponse(lookups);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup values for category: {Category}", category);
            return StatusCode(500, $"An error occurred while retrieving lookup values for category: {category}");
        }
    }

    /// <summary>Get lookup value by ID.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var lookup = await _lookupService.GetByIdAsync(id);
            if (lookup == null)
                return NotFoundResponse($"Lookup value with ID {id} not found");
            return SuccessResponse(lookup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup value by ID: {Id}", id);
            return StatusCode(500, "An error occurred while retrieving the lookup value");
        }
    }

    /// <summary>Get all lookup categories for the current shop.</summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var shopId = GetCurrentShopId();
            var categories = await _lookupService.GetCategoriesAsync(shopId);
            return SuccessResponse(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lookup categories");
            return StatusCode(500, "An error occurred while retrieving lookup categories");
        }
    }

    /// <summary>Get all US states (static list, no shop scoping required).</summary>
    [HttpGet("states")]
    public IActionResult GetStates()
    {
        return SuccessResponse(_usStates);
    }

    /// <summary>Create a new lookup value for the current shop.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLookupValueRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationErrorResponse("Invalid lookup value data");

        try
        {
            var shopId = GetCurrentShopId();
            var lookup = new LookupValue
            {
                ShopId = shopId,
                Category = request.Category,
                Value = request.Value,
                Description = request.Description ?? string.Empty,
                ColorCode = request.ColorCode ?? string.Empty,
                CategoryId = request.CategoryId,
                DisplayOrder = request.DisplayOrder,
                IsActive = request.IsActive,
                
                ModifiedDate = DateTime.UtcNow
            };

            _context.LookupValues.Add(lookup);
            await _context.SaveChangesAsync();
            _logger.LogInformation("LookupValue {Category}/{Value} created for shop {ShopId}", lookup.Category, lookup.Value, shopId);
            return SuccessResponse(new { lookup.Id, lookup.Category, lookup.Value }, "Lookup value created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lookup value");
            return StatusCode(500, "An error occurred while creating the lookup value");
        }
    }

    /// <summary>Update an existing lookup value (shop-scoped).</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLookupValueRequest request)
    {
        if (!ModelState.IsValid)
            return ValidationErrorResponse("Invalid lookup value data");

        try
        {
            var shopId = GetCurrentShopId();
            var lookup = await _context.LookupValues.FirstOrDefaultAsync(l => l.Id == id && l.ShopId == shopId);
            if (lookup == null)
                return NotFoundResponse($"Lookup value with ID {id} not found");

            lookup.Category = request.Category;
            lookup.Value = request.Value;
            lookup.Description = request.Description ?? string.Empty;
            lookup.ColorCode = request.ColorCode ?? string.Empty;
            lookup.CategoryId = request.CategoryId;
            lookup.DisplayOrder = request.DisplayOrder;
            lookup.IsActive = request.IsActive;
            lookup.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("LookupValue {Id} updated for shop {ShopId}", id, shopId);
            return SuccessResponse(new { lookup.Id, lookup.Category, lookup.Value }, "Lookup value updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lookup value {Id}", id);
            return StatusCode(500, "An error occurred while updating the lookup value");
        }
    }

    /// <summary>Delete a lookup value (shop-scoped only — global values cannot be deleted).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var lookup = await _context.LookupValues.FirstOrDefaultAsync(l => l.Id == id && l.ShopId == shopId);
            if (lookup == null)
                return NotFoundResponse($"Lookup value with ID {id} not found");

            _context.LookupValues.Remove(lookup);
            await _context.SaveChangesAsync();
            _logger.LogInformation("LookupValue {Id} deleted for shop {ShopId}", id, shopId);
            return SuccessResponse("Lookup value deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lookup value {Id}", id);
            return StatusCode(500, "An error occurred while deleting the lookup value");
        }
    }
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
