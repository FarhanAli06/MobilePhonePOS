using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Repair;
using Empire.Web.Models;
using Empire.Domain.Enums;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Empire.Web.Controllers;

public class RepairsController : Controller
{
    private readonly IRepairService _repairService;
    private readonly ICustomerService _customerService;
    private readonly IDeviceService _deviceService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RepairsController> _logger;

    public RepairsController(
        IRepairService repairService,
        ICustomerService customerService,
        IDeviceService deviceService,
        IServiceProvider serviceProvider,
        ILogger<RepairsController> logger)
    {
        _repairService = repairService;
        _customerService = customerService;
        _deviceService = deviceService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string searchTerm = "", string status = "", string paymentStatus = "", DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        try
        {
            var currentShopId = GetCurrentShopId();
            var filter = new RepairFilterRequest
            {
                ShopId = currentShopId,
                SearchTerm = searchTerm,
                Status = !string.IsNullOrEmpty(status) ? Enum.Parse<RepairStatus>(status) : null,
                PaymentStatus = !string.IsNullOrEmpty(paymentStatus) ? Enum.Parse<PaymentStatus>(paymentStatus) : null,
                StartDate = startDate,
                EndDate = endDate
            };

            var repairs = await _repairService.GetRepairsAsync(filter);

            var model = new RepairIndexViewModel
            {
                Repairs = repairs,
                SearchTerm = searchTerm,
                Status = status,
                PaymentStatus = paymentStatus,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repairs");
            ViewBag.Error = "Error loading repairs";
            return View(new RepairIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        try
        {
            var currentShopId = GetCurrentShopId();
            await LoadCreateViewData(currentShopId);
            return View(new CreateRepairViewModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create repair page");
            ViewBag.Error = "Error loading page";
            return View(new CreateRepairViewModel());
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRepairViewModel model)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        if (!ModelState.IsValid)
        {
            await LoadCreateViewData(GetCurrentShopId());
            return View(model);
        }

        try
        {
            var request = new CreateRepairRequest
            {
                ShopId = GetCurrentShopId(),
                CustomerId = model.CustomerId,
                BrandId = model.BrandId,
                DeviceCategoryId = model.DeviceCategoryId,
                DeviceModelId = model.DeviceModelId,
                Issue = model.Issue,
                Description = model.Description,
                Comments = model.Comments,
                Cost = model.Cost,
                PaymentStatus = model.PaymentStatus
            };

            await _repairService.CreateRepairAsync(request, GetCurrentUserId());
            TempData["Success"] = "Repair created successfully";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repair");
            ModelState.AddModelError("", "Error creating repair. Please try again.");
            await LoadCreateViewData(GetCurrentShopId());
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        try
        {
            var currentShopId = GetCurrentShopId();
            var repair = await _repairService.GetRepairByIdAsync(id, currentShopId);
            
            if (repair == null)
                return NotFound();

            var model = new EditRepairViewModel
            {
                Id = repair.Id,
                BrandId = repair.BrandId,
                DeviceCategoryId = repair.DeviceCategoryId,
                DeviceModelId = repair.DeviceModelId,
                Issue = repair.Issue,
                Description = repair.Description,
                Comments = repair.Comments,
                Status = repair.Status,
                PaymentStatus = repair.PaymentStatus,
                Cost = repair.Cost,
                CustomerName = repair.CustomerName,
                BrandName = repair.DeviceBrand
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair {RepairId}", id);
            return NotFound();
        }
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditRepairViewModel model)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var request = new UpdateRepairRequest
            {
                BrandId = model.BrandId,
                DeviceCategoryId = model.DeviceCategoryId,
                DeviceModelId = model.DeviceModelId,
                Issue = model.Issue,
                Description = model.Description,
                Comments = model.Comments,
                Status = model.Status,
                PaymentStatus = model.PaymentStatus,
                Cost = model.Cost
            };

            await _repairService.UpdateRepairAsync(model.Id, request, GetCurrentUserId());
            TempData["Success"] = "Repair updated successfully";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repair {RepairId}", model.Id);
            ModelState.AddModelError("", "Error updating repair. Please try again.");
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAuthenticated())
            return RedirectToAction("Login", "Home");

        if (!IsManager())
        {
            TempData["Error"] = "Only managers can delete repairs";
            return RedirectToAction("Index");
        }

        try
        {
            var currentShopId = GetCurrentShopId();
            var success = await _repairService.DeleteRepairAsync(id, currentShopId);
            
            if (success)
                TempData["Success"] = "Repair deleted successfully";
            else
                TempData["Error"] = "Repair not found";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repair {RepairId}", id);
            TempData["Error"] = "Error deleting repair";
        }

        return RedirectToAction("Index");
    }

    private async Task LoadCreateViewData(int shopId)
    {
        try
        {
            var customers = await _customerService.GetCustomersAsync(new Empire.Application.DTOs.Customer.CustomerFilterRequest { ShopId = shopId });
            var devices = await _deviceService.GetDevicesByShopAsync(shopId);

            ViewBag.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.FirstName} {c.LastName} - {c.Phone}"
            }).ToList();

            ViewBag.Devices = devices.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.DisplayName ?? $"{d.Brand} {d.Model}"
            }).ToList();

            _logger.LogInformation($"Loaded {customers.Count()} customers and {devices.Count()} devices for repair create form");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create view data");
            ViewBag.Customers = new List<SelectListItem>();
            ViewBag.Devices = new List<SelectListItem>();
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDeviceCategories()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
            
            var categories = await dbContext.DeviceCategories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new { id = c.Id, name = c.Name })
                .ToListAsync();

            _logger.LogInformation($"Retrieved {categories.Count} device categories");
            return Json(new { success = true, data = categories });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device categories");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDeviceModels(int categoryId = 0, int brandId = 0)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
            
            var query = dbContext.DeviceModels.Where(m => m.IsActive);
            
            if (categoryId > 0)
                query = query.Where(m => m.DeviceCategoryId == categoryId);
            
            if (brandId > 0)
                query = query.Where(m => m.BrandId == brandId);
            
            var models = await query
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name
                })
                .ToListAsync();
            
            return Json(new { success = true, data = models });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving device models");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EmpireDbContext>();
            
            var brands = await dbContext.Brands
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ThenBy(b => b.Name)
                .Select(b => new { id = b.Id, name = b.Name })
                .ToListAsync();

            _logger.LogInformation($"Retrieved {brands.Count} brands");
            return Json(new { success = true, data = brands });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brands");
            return Json(new { success = false, message = ex.Message });
        }
    }

    private int GetCurrentShopId()
    {
        var shopIdString = HttpContext.Session.GetString("CurrentShopId");
        return int.TryParse(shopIdString, out var shopId) ? shopId : 0;
    }

    private int GetCurrentUserId()
    {
        var userIdString = HttpContext.Session.GetString("UserId");
        return int.TryParse(userIdString, out var userId) ? userId : 0;
    }

    private bool IsAuthenticated()
    {
        return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserId"));
    }

    private bool IsManager()
    {
        return HttpContext.Session.GetString("UserRole") == "Manager";
    }
}

