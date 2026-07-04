using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Device;

namespace Empire.API.Controllers;

/// <summary>
/// Device management endpoints
/// </summary>
[Authorize]
public class DevicesController : BaseApiController
{
    private readonly IDeviceService _deviceService;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(IDeviceService deviceService, ILogger<DevicesController> logger)
    {
        _deviceService = deviceService;
        _logger = logger;
    }

    /// <summary>
    /// Get all devices for current shop
    /// </summary>
    /// <returns>List of devices</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevices()
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var devices = await _deviceService.GetDevicesByShopAsync(shopId);
            return SuccessResponse(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices");
            return ErrorResponse("Error retrieving devices", 500);
        }
    }

    /// <summary>
    /// Get device by ID
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>Device details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDevice(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var device = await _deviceService.GetDeviceByIdAsync(id);

            if (device == null)
                return NotFoundResponse("Device not found");

            return SuccessResponse(device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device {DeviceId}", id);
            return ErrorResponse("Error retrieving device", 500);
        }
    }

    /// <summary>
    /// Get available devices for sale
    /// </summary>
    /// <returns>Available devices</returns>
    [HttpGet("available")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableDevices()
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var devices = await _deviceService.GetAvailableDevicesForSaleAsync(shopId);
            return SuccessResponse(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving available devices");
            return ErrorResponse("Error retrieving available devices", 500);
        }
    }

    /// <summary>
    /// Create new device
    /// </summary>
    /// <param name="request">Device details</param>
    /// <returns>Created device</returns>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceRequest request)
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
            var device = await _deviceService.CreateDeviceAsync(request, userId);

            _logger.LogInformation("Device created: {DeviceId} by user {UserId}", device.Id, userId);

            return StatusCode(201, new
            {
                success = true,
                message = "Device created successfully",
                data = device,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device");
            return ErrorResponse("Error creating device", 500);
        }
    }

    /// <summary>
    /// Update device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <param name="request">Updated device details</param>
    /// <returns>Updated device</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return ValidationErrorResponse();

            var userId = GetCurrentUserId();
            var device = await _deviceService.UpdateDeviceAsync(id, request, userId);

            if (device == null)
                return NotFoundResponse("Device not found");

            _logger.LogInformation("Device updated: {DeviceId} by user {UserId}", id, userId);

            return SuccessResponse(device, "Device updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device {DeviceId}", id);
            return ErrorResponse("Error updating device", 500);
        }
    }

    /// <summary>
    /// Delete device
    /// </summary>
    /// <param name="id">Device ID</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDevice(int id)
    {
        try
        {
            var shopId = GetCurrentShopId();
            var userId = GetCurrentUserId();
            var success = await _deviceService.DeleteDeviceAsync(id, userId);

            if (!success)
                return NotFoundResponse("Device not found");

            _logger.LogInformation("Device deleted: {DeviceId}", id);

            return SuccessResponse("Device deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device {DeviceId}", id);
            return ErrorResponse("Error deleting device", 500);
        }
    }

    /// <summary>
    /// Filter devices
    /// </summary>
    /// <param name="filter">Filter criteria</param>
    /// <returns>Filtered devices</returns>
    [HttpPost("filter")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> FilterDevices([FromBody] DeviceFilterRequest filter)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            filter.ShopId = shopId;
            var devices = await _deviceService.GetDevicesAsync(filter);

            return SuccessResponse(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering devices");
            return ErrorResponse("Error filtering devices", 500);
        }
    }

    /// <summary>
    /// Get devices by customer
    /// </summary>
    /// <param name="customerId">Customer ID</param>
    /// <returns>List of customer devices</returns>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevicesByCustomer(int customerId)
    {
        try
        {
            var shopId = GetCurrentShopId();
            // Note: GetDevicesByCustomerAsync not in interface - using filter instead
            var filter = new DeviceFilterRequest { ShopId = shopId };
            var devices = await _deviceService.GetDevicesAsync(filter);

            return SuccessResponse(devices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving devices for customer {CustomerId}", customerId);
            return ErrorResponse("Error retrieving customer devices", 500);
        }
    }
}
