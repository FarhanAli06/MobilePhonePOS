using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Empire.Web.DTOs.Device;
using Empire.Web.Authorization;
using Empire.Web.Services;
using Empire.Web.Services.Device;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class DevicesController : BaseController
{
    private readonly IDeviceApiService _deviceApiService;
    private readonly IMapper _mapper;

    public DevicesController(
        IDeviceApiService deviceApiService, 
        IMapper mapper,
        ITimezoneService timezoneService, 
        ILogger<DevicesController> logger)
        : base(logger, timezoneService)
    {
        _deviceApiService = deviceApiService;
        _mapper = mapper;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                TempData["Error"] = "No shop selected. Please select a shop first.";
                return RedirectToAction("Index", "Shop");
            }

            var devices = await _deviceApiService.GetByShopAsync(currentShopId);

            // Convert dates to user timezone
            if (devices != null)
            {
                foreach (var device in devices)
                {
                    device.CreatedDate = _timezoneService.ConvertToUserTime(device.CreatedDate);
                    if (device.SoldDate.HasValue)
                    {
                        device.SoldDate = _timezoneService.ConvertToUserTime(device.SoldDate.Value);
                    }
                }
            }

            ViewBag.ShopPhone = "N/A";
            ViewBag.PageTitle = "Device Management";

            return View(devices ?? Enumerable.Empty<DeviceSelectionDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading devices");
            TempData["Error"] = $"Error loading devices: {ex.Message}";
            return RedirectToAction("Dashboard", "Home");
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        var currentShopId = GetCurrentShopId();
        if (currentShopId == 0)
        {
            return Json(new { success = false, message = "No shop selected" });
        }
        return PartialView("_CreateDeviceModal");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid data provided" });
            }

            var currentUserId = GetCurrentUserId();
            var currentShopId = GetCurrentShopId();

            if (currentUserId == 0 || currentShopId == 0)
            {
                return Json(new { success = false, message = "Authentication error" });
            }

            request.ShopId = currentShopId;
            var device = await _deviceApiService.CreateAsync(request);

            if (device != null)
            {
                return Json(new { success = true, message = "Device created successfully", device = device });
            }
            else
            {
                return Json(new { success = false, message = "Failed to create device" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating device");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDetails(int id)
    {
        try
        {
            var device = await _deviceApiService.GetByIdAsync(id);
            if (device == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            // Check if device belongs to current shop
            var currentShopId = GetCurrentShopId();
            if (device.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            return Json(new { success = true, device = device });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting device details");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var device = await _deviceApiService.GetByIdAsync(id);
            if (device == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            if (device.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            return PartialView("_EditDeviceModal", device);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading device for edit");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [Route("Devices/Edit/{id}")]
    public async Task<IActionResult> Edit(int id, [FromBody] UpdateDeviceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value?.Errors.Select(e => e.ErrorMessage) ?? new List<string>() })
                    .ToList();

                var errorMessage = "Validation failed: " + string.Join("; ",
                    errors.SelectMany(e => e.Errors.Select(err => $"{e.Field}: {err}")));

                return Json(new { success = false, message = errorMessage, validationErrors = errors });
            }

            var device = await _deviceApiService.UpdateAsync(id, request);

            if (device != null)
            {
                return Json(new { success = true, message = "Device updated successfully", device = device });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update device" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDeviceStatusRequestDto request)
    {
        try
        {
            // Get the current device first
            var currentDevice = await _deviceApiService.GetByIdAsync(id);
            if (currentDevice == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            var currentShopId = GetCurrentShopId();
            if (currentDevice.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Map device to update request using AutoMapper
            var updateRequest = _mapper.Map<UpdateDeviceRequest>(currentDevice);

            // Update status fields from request
            updateRequest.IsAvailableForSale = request.IsAvailableForSale;
            updateRequest.IsSold = request.IsSold;
            updateRequest.SoldToCustomerId = request.SoldToCustomerId;

            var device = await _deviceApiService.UpdateAsync(id, updateRequest);

            if (device != null)
            {
                return Json(new { success = true, message = "Device status updated successfully", device = device });
            }
            else
            {
                return Json(new { success = false, message = "Failed to update device status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating device status");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var currentShopId = GetCurrentShopId();

            // Verify device belongs to current shop
            var device = await _deviceApiService.GetByIdAsync(id);
            if (device == null || device.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            var success = await _deviceApiService.DeleteAsync(id, currentShopId);

            if (success)
            {
                return Json(new { success = true, message = "Device deleted successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Device not found or cannot be deleted" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting device");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsSold(int id, [FromBody] MarkAsSoldRequestDto request)
    {
        try
        {
            // Get current device
            var device = await _deviceApiService.GetByIdAsync(id);
            if (device == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            var currentShopId = GetCurrentShopId();
            if (device.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Use service layer method to mark as sold
            var updatedDevice = await _deviceApiService.MarkAsSoldAsync(
                id,
                request.CustomerId,
                request.SalePrice ?? device.SellingPrice ?? 0m);

            if (updatedDevice != null)
            {
                return Json(new { success = true, message = "Device marked as sold successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Failed to mark device as sold" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking device as sold");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableForSale()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var allDevices = await _deviceApiService.GetByShopAsync(currentShopId);

            // Filter for available devices
            var devices = allDevices?.Where(d => d.IsAvailableForSale && !d.IsSold).ToList();

            return Json(new { success = true, devices = devices ?? new List<DeviceSelectionDto>() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available devices");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSoldDevices()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var allDevices = await _deviceApiService.GetByShopAsync(currentShopId);

            // Filter for sold devices
            var devices = allDevices?.Where(d => d.IsSold).ToList();

            return Json(new { success = true, devices = devices ?? new List<DeviceSelectionDto>() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sold devices");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAvailableForSale(int id)
    {
        try
        {
            var device = await _deviceApiService.GetByIdAsync(id);
            if (device == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            var currentShopId = GetCurrentShopId();
            if (device.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Use service layer method to toggle availability
            var updatedDevice = await _deviceApiService.ToggleAvailabilityAsync(id);

            if (updatedDevice != null)
            {
                return Json(new {
                    success = true,
                    message = updatedDevice.IsAvailableForSale ? "Device marked as available for sale" : "Device marked as not for sale",
                    device = updatedDevice
                });
            }
            else
            {
                return Json(new { success = false, message = "Failed to toggle device status" });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling device availability");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Filter([FromBody] DeviceFilterRequestDto? filter)
    {
        try
        {
            var currentShopId = GetCurrentShopId();

            // Get all devices for the shop
            var devices = await _deviceApiService.GetByShopAsync(currentShopId);

            // Apply client-side filtering if filter is provided
            if (filter != null && devices != null)
            {
                if (!string.IsNullOrEmpty(filter.SearchTerm))
                {
                    devices = devices.Where(d =>
                        (d.Brand != null && d.Brand.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (d.Model != null && d.Model.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (d.IMEISerialNumber != null && d.IMEISerialNumber.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                if (filter.IsAvailableForSale.HasValue)
                {
                    devices = devices.Where(d => d.IsAvailableForSale == filter.IsAvailableForSale.Value).ToList();
                }

                if (filter.IsSold.HasValue)
                {
                    devices = devices.Where(d => d.IsSold == filter.IsSold.Value).ToList();
                }
            }

            return Json(new { success = true, devices = devices ?? new List<DeviceSelectionDto>() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering devices");
            return Json(new { success = false, message = ex.Message });
        }
    }
}
