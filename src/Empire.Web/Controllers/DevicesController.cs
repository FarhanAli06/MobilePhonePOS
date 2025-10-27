using Microsoft.AspNetCore.Mvc;
using Empire.Application.DTOs.Device;
using Empire.Application.Interfaces;
using Empire.Web.Authorization;
using System.Security.Claims;

namespace Empire.Web.Controllers;

[SessionAuthorizeWithShop]
public class DevicesController : Controller
{
    private readonly IDeviceService _deviceService;
    private readonly IShopService _shopService;

    public DevicesController(IDeviceService deviceService, IShopService shopService)
    {
        _deviceService = deviceService;
        _shopService = shopService;
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

            var devices = await _deviceService.GetDevicesByShopAsync(currentShopId);
            
            // Get shop info for print
            var shop = await _shopService.GetShopByIdAsync(currentShopId);
            ViewBag.ShopPhone = shop?.Phone ?? "N/A";
            ViewBag.CurrentShopId = currentShopId;
            ViewBag.PageTitle = "Device Management";
            
            return View(devices);
        }
        catch (Exception ex)
        {
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

        ViewBag.CurrentShopId = currentShopId;
        return PartialView("_CreateDeviceModal");
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Empire.Application.DTOs.Device.CreateDeviceRequest request)
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
            var device = await _deviceService.CreateDeviceAsync(request, currentUserId);
            
            return Json(new { success = true, message = "Device created successfully", device = device });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDetails(int id)
    {
        try
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
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
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            var currentShopId = GetCurrentShopId();
            if (device.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            return PartialView("_EditDeviceModal", device);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
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

            var currentUserId = GetCurrentUserId();
            var device = await _deviceService.UpdateDeviceAsync(id, request, currentUserId);
            
            return Json(new { success = true, message = "Device updated successfully", device = device });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDeviceStatusRequest request)
    {
        try
        {
            // Get the current device first
            var currentDevice = await _deviceService.GetDeviceByIdAsync(id);
            if (currentDevice == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            var currentShopId = GetCurrentShopId();
            if (currentDevice.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Create a complete update request with existing data
            var updateRequest = new UpdateDeviceRequest
            {
                Brand = currentDevice.Brand ?? "",
                Category = currentDevice.Category ?? "",
                Model = currentDevice.Model ?? "",
                ModelNumber = currentDevice.ModelNumber ?? "",
                Year = currentDevice.Year,
                DeviceType = currentDevice.DeviceType,
                IMEISerialNumber = currentDevice.IMEISerialNumber ?? "",
                BatteryHealthPercentage = currentDevice.BatteryHealthPercentage,
                NetworkStatus = currentDevice.NetworkStatus ?? "Unlocked",
                ScratchesCondition = currentDevice.ScratchesCondition ?? "Excellent",
                BuyingPrice = currentDevice.BuyingPrice,
                SellingPrice = currentDevice.SellingPrice,
                Source = currentDevice.Source ?? "",
                Notes = currentDevice.Notes ?? "",
                IsAvailableForSale = request.IsAvailableForSale,
                IsSold = request.IsSold,
                SoldToCustomerId = request.SoldToCustomerId
            };

            var currentUserId = GetCurrentUserId();
            var device = await _deviceService.UpdateDeviceAsync(id, updateRequest, currentUserId);
            
            return Json(new { success = true, message = "Device status updated successfully", device = device });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _deviceService.DeleteDeviceAsync(id, currentUserId);
            
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
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MarkAsSold(int id, [FromBody] MarkAsSoldRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            var success = await _deviceService.MarkDeviceAsSoldAsync(id, request.CustomerId, request.SalePrice, currentUserId);
            
            if (success)
            {
                return Json(new { success = true, message = "Device marked as sold successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Device not found or already sold" });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableForSale()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var devices = await _deviceService.GetAvailableDevicesForSaleAsync(currentShopId);
            return Json(new { success = true, devices = devices });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSoldDevices()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var devices = await _deviceService.GetSoldDevicesAsync(currentShopId);
            return Json(new { success = true, devices = devices });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> ToggleAvailableForSale(int id)
    {
        try
        {
            var device = await _deviceService.GetDeviceByIdAsync(id);
            if (device == null)
            {
                return Json(new { success = false, message = "Device not found" });
            }

            var currentShopId = GetCurrentShopId();
            if (device.ShopId != currentShopId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var updateRequest = new UpdateDeviceRequest
            {
                Brand = device.Brand,
                Category = device.Category,
                Model = device.Model,
                ModelNumber = device.ModelNumber,
                Year = device.Year,
                DeviceType = device.DeviceType,
                IMEISerialNumber = device.IMEISerialNumber,
                BatteryHealthPercentage = device.BatteryHealthPercentage,
                NetworkStatus = device.NetworkStatus,
                ScratchesCondition = device.ScratchesCondition,
                BuyingPrice = device.BuyingPrice,
                SellingPrice = device.SellingPrice,
                Source = device.Source,
                Notes = device.Notes,
                IsAvailableForSale = !device.IsAvailableForSale, // Toggle the status
                IsSold = device.IsSold,
                SoldToCustomerId = device.SoldToCustomerId
            };

            var currentUserId = GetCurrentUserId();
            var updatedDevice = await _deviceService.UpdateDeviceAsync(id, updateRequest, currentUserId);
            
            return Json(new { 
                success = true, 
                message = updatedDevice.IsAvailableForSale ? "Device marked as available for sale" : "Device marked as not for sale",
                device = updatedDevice 
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Filter([FromBody] DeviceFilterRequest? filter)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            
            // Handle null filter
            if (filter == null)
            {
                filter = new DeviceFilterRequest();
            }
            
            filter.ShopId = currentShopId; // Ensure shop-based filtering
            
            var devices = await _deviceService.GetDevicesAsync(filter);
            return Json(new { success = true, devices = devices });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        return int.TryParse(userIdString, out var userId) ? userId : 0;
    }

    private int GetCurrentShopId()
    {
        var shopIdString = HttpContext.Session.GetString("CurrentShopId");
        return int.TryParse(shopIdString, out var shopId) ? shopId : 0;
    }
}


public class MarkAsSoldRequest
{
    public int CustomerId { get; set; }
    public decimal? SalePrice { get; set; }
}

