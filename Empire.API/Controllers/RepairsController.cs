using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Repair;

namespace Empire.API.Controllers;

/// <summary>
/// Repair management endpoints
/// </summary>
[Authorize]
public class RepairsController : BaseApiController
{
    private readonly IRepairService _repairService;
    private readonly ILogger<RepairsController> _logger;

    public RepairsController(IRepairService repairService, ILogger<RepairsController> logger)
    {
        _repairService = repairService;
        _logger = logger;
    }

    /// <summary>
    /// Get all repairs with optional filtering
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>List of repairs</returns>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairs([FromBody] RepairFilterRequest filter)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            filter.ShopId = shopId;
            var repairs = await _repairService.GetRepairsAsync(filter);

            return SuccessResponse(repairs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving repairs");
            return ErrorResponse("Error retrieving repairs", 500);
        }
    }

    /// <summary>
    /// Get repair by ID
    /// </summary>
    /// <param name="id">Repair ID</param>
    /// <returns>Repair details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRepair(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var repair = await _repairService.GetRepairByIdAsync(id, shopId);

            if (repair == null)
                return NotFoundResponse("Repair not found");

            return SuccessResponse(repair);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving repair {RepairId}", id);
            return ErrorResponse("Error retrieving repair", 500);
        }
    }

    /// <summary>
    /// Create new repair
    /// </summary>
    /// <param name="request">Repair details</param>
    /// <returns>Created repair</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRepair([FromBody] CreateRepairRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var shopId = GetCurrentShopId();
            var userId = GetCurrentUserId();

            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            request.ShopId = shopId;
            var repair = await _repairService.CreateRepairAsync(request, userId);

            _logger.LogInformation("Repair created: {RepairId} by user {UserId}", repair.Id, userId);

            return StatusCode(201, new
            {
                success = true,
                message = "Repair created successfully",
                data = repair,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repair");
            return ErrorResponse(ex.Message, 500);
        }
    }

    /// <summary>
    /// Update repair
    /// </summary>
    /// <param name="id">Repair ID</param>
    /// <param name="request">Updated repair details</param>
    /// <returns>Updated repair</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateRepair(int id, [FromBody] UpdateRepairRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var userId = GetCurrentUserId();
            var repair = await _repairService.UpdateRepairAsync(id, request, userId);

            if (repair == null)
                return NotFoundResponse("Repair not found");

            _logger.LogInformation("Repair updated: {RepairId} by user {UserId}", id, userId);

            return SuccessResponse(repair, "Repair updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repair {RepairId}", id);
            return ErrorResponse("Error updating repair", 500);
        }
    }

    /// <summary>
    /// Delete repair
    /// </summary>
    /// <param name="id">Repair ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRepair(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var success = await _repairService.DeleteRepairAsync(id, shopId);

            if (!success)
                return NotFoundResponse("Repair not found");

            _logger.LogInformation("Repair deleted: {RepairId}", id);

            return SuccessResponse("Repair deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repair {RepairId}", id);
            return ErrorResponse("Error deleting repair", 500);
        }
    }

    /// <summary>
    /// Get all repairs for a specific shop
    /// </summary>
    /// <param name="shopId">Shop ID</param>
    /// <returns>List of repairs for the shop</returns>
    [HttpGet("shop/{shopId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairsByShop(int shopId)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            // Use the shopId from the route; fall back to JWT claim if 0
            var effectiveShopId = shopId > 0 ? shopId : currentShopId;
            if (effectiveShopId == 0)
                return UnauthorizedResponse("No shop selected");

            var filter = new RepairFilterRequest { ShopId = effectiveShopId };
            var repairs = await _repairService.GetRepairsAsync(filter);
            return SuccessResponse(repairs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving repairs for shop {ShopId}", shopId);
            return ErrorResponse("Error retrieving repairs", 500);
        }
    }

    /// <summary>
    /// Get repairs by customer ID
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <returns>List of customer repairs</returns>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairsByCustomer(int customerId)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var repairs = await _repairService.GetRepairsByCustomerAsync(customerId, shopId);

            return SuccessResponse(repairs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving repairs for customer {CustomerId}", customerId);
            return ErrorResponse("Error retrieving customer repairs", 500);
        }
    }

    /// <summary>
    /// Get repair statistics
    /// </summary>
    /// <returns>Repair statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairStatistics()
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            // Get all repairs for the shop
            var filter = new RepairFilterRequest { ShopId = shopId };
            var repairs = await _repairService.GetRepairsAsync(filter);

            var statistics = new
            {
                total = repairs.Count(),
                inProgress = repairs.Count(r => r.Status == "InProgress"),
                completed = repairs.Count(r => r.Status == "Completed" || r.Status == "Complete"),
                pickedUp = repairs.Count(r => r.Status == "PickedUp"),
                waitingForParts = repairs.Count(r => r.Status == "WaitingForParts"),
                paid = repairs.Count(r => r.PaymentStatus == "Paid"),
                unpaid = repairs.Count(r => r.PaymentStatus == "Unpaid"),
                partial = repairs.Count(r => r.PaymentStatus == "Partial"),
                totalRevenue = repairs.Where(r => r.PaymentStatus == "Paid").Sum(r => r.Cost),
                pendingRevenue = repairs.Where(r => r.PaymentStatus != "Paid").Sum(r => r.RemainingDues)
            };

            return SuccessResponse(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving repair statistics");
            return ErrorResponse("Error retrieving statistics", 500);
        }
    }

    /// <summary>
    /// Add parts to an existing repair.
    /// Supports flat ID list (qty=1 each) or detailed lines with quantity and price.
    /// Also deducts stock via StockMovement records.
    /// </summary>
    /// <param name="id">Repair ID</param>
    /// <param name="request">Parts request</param>
    /// <returns>Success message</returns>
    [HttpPost("{id}/parts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPartsToRepair(int id, [FromBody] AddRepairPartsRequest request)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            // Inject shopId and userId from JWT claims
            request.ShopId = shopId;
            request.UserId = GetCurrentUserId();

            var success = await _repairService.AddPartsToRepairAsync(id, request, shopId);
            if (!success)
                return NotFoundResponse("Repair not found or does not belong to this shop");

            _logger.LogInformation("Parts added to repair {RepairId} by user {UserId}", id, request.UserId);
            return SuccessResponse("Parts added to repair successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding parts to repair {RepairId}", id);
            return ErrorResponse("Error adding parts to repair", 500);
        }
    }

    /// <summary>Get parts used in a repair.</summary>
    [HttpGet("{id}/parts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairParts(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var parts  = await _repairService.GetRepairPartsAsync(id, shopId);
            return SuccessResponse(parts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parts for repair {RepairId}", id);
            return ErrorResponse("Error retrieving repair parts", 500);
        }
    }

    /// <summary>
    /// Replace all parts on a repair: removes existing parts (restoring stock) and adds the new set.
    /// </summary>
    [HttpPut("{id}/parts")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceRepairParts(int id, [FromBody] AddRepairPartsRequest request)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            request.ShopId = shopId;
            request.UserId = GetCurrentUserId();

            var success = await _repairService.ReplacePartsAsync(id, request, shopId);
            if (!success)
                return NotFoundResponse("Repair not found or does not belong to this shop");

            _logger.LogInformation("Parts replaced on repair {RepairId} by user {UserId}", id, request.UserId);
            return SuccessResponse("Parts replaced successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing parts on repair {RepairId}", id);
            return ErrorResponse("Error replacing repair parts", 500);
        }
    }

    /// <summary>Get the full activity / audit history for a repair.</summary>
    [HttpGet("{id}/activities")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairActivities(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var activities = await _repairService.GetRepairActivitiesAsync(id, shopId);
            return SuccessResponse(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving activities for repair {RepairId}", id);
            return ErrorResponse("Error retrieving repair activities", 500);
        }
    }

    // ── Payment Ledger Endpoints ───────────────────────────────────────────────────────

    /// <summary>Get all payment/refund records for a repair.</summary>
    [HttpGet("{id}/payments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRepairPayments(int id)
    {
        try
        {
            var payments = await _repairService.GetRepairPaymentsAsync(id);
            return SuccessResponse(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for repair {RepairId}", id);
            return ErrorResponse("Error retrieving repair payments", 500);
        }
    }

    /// <summary>Add a payment or refund to a repair.</summary>
    [HttpPost("{id}/payments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddRepairPayment(int id, [FromBody] AddRepairPaymentRequest request)
    {
        try
        {
            request.RepairId = id;
            request.UserId = GetCurrentUserId();
            var payment = await _repairService.AddRepairPaymentAsync(request);
            return SuccessResponse(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to repair {RepairId}", id);
            return ErrorResponse("Error adding repair payment", 500);
        }
    }

    /// <summary>Update an existing payment/refund record.</summary>
    [HttpPut("payments/{paymentId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRepairPayment(int paymentId, [FromBody] UpdateRepairPaymentRequest request)
    {
        try
        {
            var payment = await _repairService.UpdateRepairPaymentAsync(paymentId, request);
            if (payment == null)
                return NotFoundResponse("Payment not found");
            return SuccessResponse(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId}", paymentId);
            return ErrorResponse("Error updating repair payment", 500);
        }
    }

    /// <summary>Delete a payment/refund record.</summary>
    [HttpDelete("payments/{paymentId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRepairPayment(int paymentId)
    {
        try
        {
            var success = await _repairService.DeleteRepairPaymentAsync(paymentId);
            if (!success)
                return NotFoundResponse("Payment not found");
            return SuccessResponse("Payment deleted");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment {PaymentId}", paymentId);
            return ErrorResponse("Error deleting repair payment", 500);
        }
    }
}
