using Microsoft.AspNetCore.Mvc;
using Empire.Application.DTOs.Shop;
using Empire.Application.Interfaces;

namespace Empire.API.Controllers;

/// <summary>
/// Shop management endpoints
/// </summary>
public class ShopsController : BaseApiController
{
    private readonly IShopService _shopService;
    private readonly ILogger<ShopsController> _logger;

    public ShopsController(IShopService shopService, ILogger<ShopsController> logger)
    {
        _shopService = shopService;
        _logger = logger;
    }

    /// <summary>Get all shops</summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var shops = await _shopService.GetAllShopsAsync();
            return SuccessResponse(shops, "Shops retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all shops");
            return ErrorResponse("An error occurred while retrieving shops");
        }
    }

    /// <summary>Get a shop by ID</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var shop = await _shopService.GetShopByIdAsync(id);
            if (shop == null)
                return NotFoundResponse($"Shop with ID {id} not found");

            return SuccessResponse(shop, "Shop retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shop {ShopId}", id);
            return ErrorResponse("An error occurred while retrieving the shop");
        }
    }

    /// <summary>Get all shops accessible by a specific user</summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(int userId)
    {
        try
        {
            _logger.LogInformation("Getting shops for user {UserId}", userId);
            var shops = await _shopService.GetShopsByUserAsync(userId);
            return SuccessResponse(shops, "User shops retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shops for user {UserId}", userId);
            return ErrorResponse("An error occurred while retrieving user shops");
        }
    }

    /// <summary>Create a new shop</summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateShopRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var userId = GetCurrentUserId();
            var shop = await _shopService.CreateShopAsync(request, userId);
            return SuccessResponse(shop, "Shop created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shop");
            return ErrorResponse("An error occurred while creating the shop");
        }
    }

    /// <summary>Update an existing shop</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShopRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var userId = GetCurrentUserId();
            var shop = await _shopService.UpdateShopAsync(id, request, userId);
            return SuccessResponse(shop, "Shop updated successfully");
        }
        catch (ArgumentException ex)
        {
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shop {ShopId}", id);
            return ErrorResponse("An error occurred while updating the shop");
        }
    }

    /// <summary>Delete a shop</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _shopService.DeleteShopAsync(id);
            return SuccessResponse("Shop deleted successfully");
        }
        catch (ArgumentException ex)
        {
            return NotFoundResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shop {ShopId}", id);
            return ErrorResponse("An error occurred while deleting the shop");
        }
    }
}
