using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Empire.Application.DTOs.Repair;
using Empire.Application.Interfaces;
using Empire.Domain.Enums;
using Empire.Web.Authorization;

namespace Empire.Web.Controllers;

[ApiController]
[Route("api/shops/{shopId}/[controller]")]
[SessionAuthorize]
public class RepairController : ControllerBase
{
    private readonly IRepairService _repairService;

    public RepairController(IRepairService repairService)
    {
        _repairService = repairService;
    }

    [HttpPost]
    public async Task<ActionResult<RepairDto>> CreateRepair(int shopId, [FromBody] CreateRepairRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verify user has access to this shop
        if (!HasShopAccess(shopId))
            return Forbid();

        request.ShopId = shopId;
        var userId = GetCurrentUserId();

        try
        {
            var repair = await _repairService.CreateRepairAsync(request, userId);
            return CreatedAtAction(nameof(GetRepair), new { shopId, id = repair.Id }, repair);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RepairDto>> GetRepair(int shopId, int id)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        var repair = await _repairService.GetRepairByIdAsync(id, shopId);
        if (repair == null)
            return NotFound();

        return Ok(repair);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RepairDto>>> GetRepairs(
        int shopId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] RepairStatus? status = null,
        [FromQuery] PaymentStatus? paymentStatus = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? searchTerm = null)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        var filter = new RepairFilterRequest
        {
            ShopId = shopId,
            StartDate = startDate,
            EndDate = endDate,
            Status = status,
            PaymentStatus = paymentStatus,
            CustomerId = customerId,
            SearchTerm = searchTerm
        };

        var repairs = await _repairService.GetRepairsAsync(filter);
        return Ok(repairs);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RepairDto>> UpdateRepair(int shopId, int id, [FromBody] UpdateRepairRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (!HasShopAccess(shopId))
            return Forbid();

        var userId = GetCurrentUserId();
        var repair = await _repairService.UpdateRepairAsync(id, request, userId);
        
        if (repair == null)
            return NotFound();

        return Ok(repair);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRepair(int shopId, int id)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        // Only managers can delete repairs
        if (!HasManagerRole(shopId))
            return Forbid();

        var success = await _repairService.DeleteRepairAsync(id, shopId);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<RepairDto>>> GetRepairsByCustomer(int shopId, int customerId)
    {
        if (!HasShopAccess(shopId))
            return Forbid();

        var repairs = await _repairService.GetRepairsByCustomerAsync(customerId, shopId);
        return Ok(repairs);
    }

    [HttpGet("status-options")]
    public ActionResult<object> GetStatusOptions()
    {
        var statusOptions = Enum.GetValues<RepairStatus>()
            .Select(s => new { value = (int)s, name = s.ToString() })
            .ToList();

        var paymentStatusOptions = Enum.GetValues<PaymentStatus>()
            .Select(s => new { value = (int)s, name = s.ToString() })
            .ToList();

        return Ok(new 
        { 
            repairStatuses = statusOptions,
            paymentStatuses = paymentStatusOptions
        });
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

