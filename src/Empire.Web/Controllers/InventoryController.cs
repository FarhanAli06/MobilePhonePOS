using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Empire.Application.DTOs.Inventory;
using Empire.Application.Interfaces;
using Empire.Domain.Enums;
using Empire.Web.Authorization;

namespace Empire.Web.Controllers;

[ApiController]
[Route("api/shops/{shopId}/[controller]")]
[SessionAuthorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost]
    public async Task<ActionResult<InventoryDto>> CreateInventory(int shopId, [FromBody] CreateInventoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!HasShopAccess(shopId))
            return Forbid();

        request.ShopId = shopId;

        try
        {
            var inventory = await _inventoryService.CreateInventoryAsync(request);
            return CreatedAtAction(nameof(GetInventory), new { shopId, id = inventory.Id }, inventory);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InventoryDto>> GetInventory(int shopId, int id)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        var inventory = await _inventoryService.GetInventoryByIdAsync(id, shopId);
        if (inventory == null)
            return NotFound();

        return Ok(inventory);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetInventories(
        int shopId,
        [FromQuery] DeviceType? deviceType = null,
        [FromQuery] string? category = null,
        [FromQuery] bool? lowStockOnly = null,
        [FromQuery] bool? lowStockNotificationOnly = null,
        [FromQuery] string? searchTerm = null)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        var filter = new InventoryFilterRequest
        {
            ShopId = shopId,
            DeviceType = deviceType,
            Category = category,
            LowStockOnly = lowStockOnly,
            LowStockNotificationOnly = lowStockNotificationOnly,
            SearchTerm = searchTerm
        };

        var inventories = await _inventoryService.GetInventoryAsync(filter);
        return Ok(inventories);
    }

    [HttpGet("device-type/{deviceType}")]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetInventoriesByDeviceType(int shopId, DeviceType deviceType)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        var inventories = await _inventoryService.GetInventoryByDeviceTypeAsync(shopId, deviceType);
        return Ok(inventories);
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetLowStockItems(int shopId)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        var inventories = await _inventoryService.GetLowStockItemsAsync(shopId);
        return Ok(inventories);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InventoryDto>> UpdateInventory(int shopId, int id, [FromBody] UpdateInventoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!HasShopAccess(shopId))
            return Forbid();

        var inventory = await _inventoryService.UpdateInventoryAsync(id, request);
        if (inventory == null)
            return NotFound();

        return Ok(inventory);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteInventory(int shopId, int id)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        // Only managers can delete inventory items
        if (!HasManagerRole(shopId))
            return Forbid();

        var success = await _inventoryService.DeleteInventoryAsync(id, shopId);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPost("adjust")]
    public async Task<ActionResult> AdjustInventory(int shopId, [FromBody] InventoryAdjustmentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!HasShopAccess(shopId))
            return Forbid();

        var userId = GetCurrentUserId();
        var success = await _inventoryService.AdjustInventoryAsync(request, userId);
        
        if (!success)
            return BadRequest(new { message = "Failed to adjust inventory" });

        return Ok(new { message = "Inventory adjusted successfully" });
    }

    [HttpGet("device-types")]
    public ActionResult<object> GetDeviceTypes()
    {
        var deviceTypes = Enum.GetValues<DeviceType>()
            .Select(dt => new { value = (int)dt, name = dt.ToString() })
            .ToList();

        return Ok(deviceTypes);
    }

    private bool HasShopAccess(int shopId)
    {
        var shopClaims = User.Claims.Where(c => c.Type == "ShopId").ToList();
        return shopClaims.Any(c => c.Value == shopId.ToString());
    }

    private bool HasManagerRole(int shopId)
    {
        var roleClaimType = $"Shop_{shopId}_Role";
        var roleClaim = User.Claims.FirstOrDefault(c => c.Type == roleClaimType);
        return roleClaim?.Value == UserRole.Manager.ToString();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }
}

