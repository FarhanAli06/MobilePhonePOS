using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Web.DTOs.Repair;
using Empire.Web.DTOs.Device;
using Empire.Web.Models;
using Empire.Web.Constants;
using Empire.Web.Services.Repair;
using Empire.Web.Services.Customer;
using Empire.Web.Services.Device;
using Empire.Web.Services.Brand;
using Empire.Web.Services.DeviceCategory;
using Empire.Web.Services.DeviceModel;
using Empire.Web.Services.Shop;
using Empire.Web.Services.Lookup;
using Empire.Web.Services.Inventory;
using Empire.Web.Enums;
namespace Empire.Web.Controllers;

/// <summary>
/// Controller for managing repairs
/// Refactored to use BaseController, Constants, and remove code duplication
/// </summary>
public class RepairsController : BaseController
{
    private readonly IRepairApiService _repairApiService;
    private readonly ICustomerApiService _customerApiService;
    private readonly IDeviceApiService _deviceApiService;
    private readonly IBrandApiService _brandApiService;
    private readonly IDeviceCategoryApiService _deviceCategoryApiService;
    private readonly IDeviceModelApiService _deviceModelApiService;
    private readonly IShopApiService _shopApiService;
    private readonly IInventoryApiService _inventoryApiService;
    private readonly IServiceProvider _serviceProvider;

    public RepairsController(
        IRepairApiService repairApiService,
        ICustomerApiService customerApiService,
        IDeviceApiService deviceApiService,
        ILookupApiService lookupApiService,
        IBrandApiService brandApiService,
        IDeviceCategoryApiService deviceCategoryApiService,
        IDeviceModelApiService deviceModelApiService,
        IShopApiService shopApiService,
        IInventoryApiService inventoryApiService,
        IServiceProvider serviceProvider,
        ILogger<RepairsController> logger,
        Empire.Web.Services.ITimezoneService timezoneService)
        : base(logger, timezoneService, lookupApiService)
    {
        _repairApiService = repairApiService;
        _customerApiService = customerApiService;
        _deviceApiService = deviceApiService;
        _brandApiService = brandApiService;
        _deviceCategoryApiService = deviceCategoryApiService;
        _deviceModelApiService = deviceModelApiService;
        _shopApiService = shopApiService;
        _inventoryApiService = inventoryApiService;
        _serviceProvider = serviceProvider;
    }

    public async Task<IActionResult> Index(string searchTerm = AppConstants.EmptyString, 
        string status = AppConstants.EmptyString, 
        string paymentStatus = AppConstants.EmptyString, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        if (!IsAuthenticated())
            return RedirectToLogin();

        try
        {
            var currentShopId = GetCurrentShopId();
            
            // Get all repairs for the shop via API
            var repairs = await _repairApiService.GetByShopAsync(currentShopId);

            // Apply client-side filtering
            if (!string.IsNullOrEmpty(searchTerm))
            {
                repairs = repairs?.Where(r => 
                    (r.RepairNumber != null && r.RepairNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (r.CustomerName != null && r.CustomerName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (r.Description != null && r.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (!string.IsNullOrEmpty(status))
            {
                repairs = repairs?.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(paymentStatus))
            {
                repairs = repairs?.Where(r => r.PaymentStatus.Equals(paymentStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (startDate.HasValue)
            {
                repairs = repairs?.Where(r => r.CreatedDate >= startDate.Value).ToList();
            }

            if (endDate.HasValue)
            {
                repairs = repairs?.Where(r => r.CreatedDate <= endDate.Value).ToList();
            }

            // Convert dates to user timezone
            if (repairs != null)
            {
                foreach (var repair in repairs)
                {
                    repair.CreatedDate = ToUserTime(repair.CreatedDate);
                    if (repair.CompletedDate.HasValue)
                    {
                        repair.CompletedDate = ToUserTime(repair.CompletedDate.Value);
                    }
                }
            }

            // Get shop info for print
            var shop = await _shopApiService.GetByIdAsync(currentShopId);
            ViewBag.ShopPhone = shop?.Phone ?? PageConstants.Repair.NotAvailable;

            // Load lookup values for filters using BaseController method
            ViewBag.RepairStatuses = await GetLookupSelectListAsync(LookupCategories.RepairStatus, status);
            ViewBag.PaymentStatuses = await GetLookupSelectListAsync(LookupCategories.PaymentStatus, paymentStatus);

            // Apply pagination
            var allRepairs = repairs ?? new List<RepairDto>();
            var totalCount = allRepairs.Count();
            var pagedRepairs = allRepairs
                .OrderByDescending(r => r.CreatedDate)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var model = new RepairIndexViewModel
            {
                Repairs = pagedRepairs,
                SearchTerm = searchTerm,
                Status = status,
                PaymentStatus = paymentStatus,
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
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
            return RedirectToLogin();

        try
        {
            var currentShopId = GetCurrentShopId();
            await LoadCreateViewDataAsync(currentShopId);
            return View(new CreateRepairViewModel());
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error loading repair page");
            return RedirectToAction(nameof(Index));;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRepairViewModel model)
    {
        if (!IsAuthenticated())
            return RedirectToLogin();

        if (!ModelState.IsValid)
        {
            await LoadCreateViewDataAsync(GetCurrentShopId());
            return View(model);
        }

        try
        {
            var currentShopId = GetCurrentShopId();
            var request = new CreateRepairRequest
            {
                ShopId = currentShopId,
                CustomerId = model.CustomerId,
                BrandId = model.BrandId,
                DeviceCategoryId = model.DeviceCategoryId,
                DeviceModelId = model.DeviceModelId,
                Description = model.Description,
                Comments = model.Comments,
                Cost = model.Cost,
                // Map string to enum (model.PaymentStatus comes from a lookup dropdown as a string)
                PaymentStatus = (model.PaymentStatus ?? "Unpaid").ToLower() switch
                {
                    "paid"    => PaymentStatus.Paid,
                    "partial" => PaymentStatus.Partial,
                    _         => PaymentStatus.Unpaid
                },
                CompanyId = model.CompanyId
            };

            var repair = await _repairApiService.CreateAsync(request);
            
            if (repair == null)
            {
                ModelState.AddModelError(AppConstants.EmptyString, ValidationMessages.Repair.ErrorCreatingRepair);
                await LoadCreateViewDataAsync(GetCurrentShopId());
                return View(model);
            }
            
            // Add repair parts and manage inventory
            _logger.LogInformation("InventoryParts count: {Count}", model.InventoryParts?.Count ?? 0);
            if (model.InventoryParts != null && model.InventoryParts.Any())
            {
                await ProcessRepairPartsAsync(model.InventoryParts, repair, currentShopId);
            }
            
            SetSuccessMessage(PageConstants.Repair.RepairCreatedSuccess);
            return RedirectToAction(RouteConstants.Actions.Index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repair");
            ModelState.AddModelError(AppConstants.EmptyString, ValidationMessages.Repair.ErrorCreatingRepair);
            await LoadCreateViewDataAsync(GetCurrentShopId());
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!IsAuthenticated())
            return RedirectToLogin();
        try
        {
            var repair = await _repairApiService.GetByIdAsync(id);
            if (repair == null)
                return NotFound();
            // Convert UTC dates to user local timezone for display
            repair.CreatedDate = ToUserTime(repair.CreatedDate);
            if (repair.CompletedDate.HasValue)
                repair.CompletedDate = ToUserTime(repair.CompletedDate.Value);
            return View(repair);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair details {RepairId}", id);
            return NotFound();
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!IsAuthenticated())
            return RedirectToLogin();

        try
        {
            var repair = await _repairApiService.GetByIdAsync(id);
            
            if (repair == null)
                return NotFound();

            var model = new EditRepairViewModel
            {
                Id = repair.Id,
                BrandId = repair.BrandId,
                DeviceCategoryId = repair.DeviceCategoryId,
                DeviceModelId = repair.DeviceModelId,
                Description = repair.Description,
                Comments = repair.Comments,
                Status = repair.Status,
                PaymentStatus = repair.PaymentStatus,
                Cost = repair.Cost,
                AmountPaid = repair.AmountPaid,
                CustomerName = repair.CustomerName,
                BrandName = repair.DeviceBrand
            };

            // Load lookup values using BaseController method
            ViewBag.RepairStatuses = await GetLookupSelectListAsync(LookupCategories.RepairStatus);
            ViewBag.PaymentStatuses = await GetLookupSelectListAsync(LookupCategories.PaymentStatus);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair {RepairId}", id);
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditRepairViewModel model)
    {
        if (!IsAuthenticated())
            return RedirectToLogin();

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var currentUserId = GetCurrentUserId();
            
            var request = new UpdateRepairRequestDto
            {
                BrandId = model.BrandId,
                DeviceCategoryId = model.DeviceCategoryId,
                DeviceModelId = model.DeviceModelId,
                Description = model.Description,
                Comments = model.Comments,
                Status = model.Status,
                PaymentStatus = model.PaymentStatus,
                Cost = model.Cost
            };

            var updatedRepair = await _repairApiService.UpdateAsync(model.Id, request);
            
            if (updatedRepair == null)
            {
                ModelState.AddModelError(AppConstants.EmptyString, ValidationMessages.Repair.ErrorUpdatingRepair);
                return View(model);
            }

            // Handle inventory parts if provided
            if (model.InventoryParts != null && model.InventoryParts.Any())
            {
                var currentShopId = GetCurrentShopId();
                await ProcessRepairPartsAsync(model.InventoryParts, new RepairDto { Id = model.Id }, currentShopId);
            }
            
            SetSuccessMessage(PageConstants.Repair.RepairUpdatedSuccess);
            return RedirectToAction(RouteConstants.Actions.Index);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repair");
            ViewBag.Error = "Error updating repair";
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAuthenticated())
            return RedirectToLogin();

        if (!IsManager())
        {
            SetErrorMessage(ValidationMessages.Repair.OnlyManagersCanDelete);
            return RedirectToAction(RouteConstants.Actions.Index);
        }

        try
        {
            var currentShopId = GetCurrentShopId();
            
            // Verify repair exists (ShopId check is enforced at the API level)
            var repair = await _repairApiService.GetByIdAsync(id);
            if (repair == null)
            {
                SetErrorMessage(PageConstants.Repair.RepairNotFound);
                return RedirectToAction(RouteConstants.Actions.Index);
            }
            
            var success = await _repairApiService.DeleteAsync(id);
            
            if (success)
                SetSuccessMessage(PageConstants.Repair.RepairDeletedSuccess);
            else
                SetErrorMessage(PageConstants.Repair.RepairNotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repair {RepairId}", id);
            SetErrorMessage(PageConstants.Repair.ErrorDeletingRepair);
        }

        return RedirectToAction(RouteConstants.Actions.Index);
    }

    #region API Endpoints for AJAX

    /// <summary>Returns repairs for the current shop as JSON (used by POS AJAX search).</summary>
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GetRepairsJson(string searchTerm = "")
    {
        try
        {
            var shopId = GetCurrentShopId();
            var repairs = await _repairApiService.GetByShopAsync(shopId);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                repairs = repairs.Where(r =>
                    (r.RepairNumber  != null && r.RepairNumber.ToLower().Contains(term))  ||
                    (r.CustomerName  != null && r.CustomerName.ToLower().Contains(term))  ||
                    (r.Description   != null && r.Description.ToLower().Contains(term))   ||
                    (r.DeviceBrand   != null && r.DeviceBrand.ToLower().Contains(term))   ||
                    (r.DeviceModel   != null && r.DeviceModel.ToLower().Contains(term))
                );
            }
            var data = repairs.Select(r => new {
                id            = r.Id,
                repairNumber  = r.RepairNumber,
                customerName  = r.CustomerName,
                customerPhone = r.CustomerPhone,
                deviceBrand   = r.DeviceBrand,
                deviceModel   = r.DeviceModel,
                description   = r.Description,
                status        = r.Status,
                paymentStatus = r.PaymentStatus,
                cost          = r.Cost,
                remainingDues = r.RemainingDues,
                createdDate   = ToUserTime(r.CreatedDate).ToString("MMM dd, yyyy hh:mm tt")
            });
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repairs JSON");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Deletes a repair via AJAX (no antiforgery, used by POS page).</summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteRepairJson(int id)
    {
        if (!IsAuthenticated())
            return Json(new { success = false, message = "Not authenticated" });
        if (!IsManager())
            return Json(new { success = false, message = "Only managers can delete repairs" });
        try
        {
            var success = await _repairApiService.DeleteAsync(id);
            return Json(new { success, message = success ? "Repair deleted successfully" : "Repair not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting repair {RepairId} via JSON", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDeviceCategories()
    {
        try
        {
            var categories = await _deviceCategoryApiService.GetAllAsync();
            
            if (categories == null)
            {
                return Json(new { success = false, message = ValidationMessages.Repair.FailedToRetrieveDeviceCategories });
            }

            var data = categories.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                icon = c.Icon,
                color = c.Color
            }).ToList();

            _logger.LogInformation("Retrieved {Count} device categories", data.Count);
            return Json(new { success = true, data });
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
            List<DeviceModelDto>? models;
            
            if (brandId > 0 && categoryId > 0)
            {
                models = await _deviceModelApiService.GetByBrandAndCategoryAsync(brandId, categoryId);
            }
            else
            {
                models = await _deviceModelApiService.GetAllAsync();
                
                if (models != null)
                {
                    if (categoryId > 0)
                        models = models.Where(m => m.DeviceCategoryId == categoryId).ToList();
                    
                    if (brandId > 0)
                        models = models.Where(m => m.BrandId == brandId).ToList();
                }
            }
            
            if (models == null)
            {
                return Json(new { success = false, message = ValidationMessages.Repair.FailedToRetrieveDeviceModels });
            }

            var data = models.Select(m => new
            {
                id = m.Id,
                name = m.Name,
                icon = m.Icon,
                color = m.Color
            }).ToList();

            _logger.LogInformation("Retrieved {Count} device models", data.Count);
            return Json(new { success = true, data });
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
            var brands = await _brandApiService.GetAllAsync();
            
            if (brands == null)
            {
                return Json(new { success = false, message = ValidationMessages.Repair.FailedToRetrieveBrands });
            }

            var data = brands.Select(b => new
            {
                id = b.Id,
                name = b.Name,
                icon = b.Icon,
                color = b.Color
            }).ToList();

            _logger.LogInformation("Retrieved {Count} brands", data.Count);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving brands");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentMethods()
    {
        return await GetLookupJsonAsync(LookupCategories.PaymentMethod, "payment methods");
    }

    [HttpGet]
    public async Task<IActionResult> GetRepairStatuses()
    {
        return await GetLookupJsonAsync(LookupCategories.RepairStatus, "repair statuses");
    }

    [HttpGet]
    public async Task<IActionResult> GetPaymentStatuses()
    {
        return await GetLookupJsonAsync(LookupCategories.PaymentStatus, "payment statuses");
    }

    [HttpGet]
    public async Task<IActionResult> GetInventoryParts(int brandId = 0, int categoryId = 0, int modelId = 0)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var items = await _inventoryApiService.GetInventoryAsync(currentShopId);

            if (items == null)
                return Json(new { success = false, message = ValidationMessages.Repair.FailedToRetrieveInventoryParts });

            var filtered = items.AsEnumerable();

            if (brandId > 0)
                filtered = filtered.Where(i => i.BrandId == brandId);
            if (categoryId > 0)
                filtered = filtered.Where(i => i.DeviceCategoryId == categoryId);
            if (modelId > 0)
                filtered = filtered.Where(i => i.DeviceModelId == modelId);

            var data = filtered.Select(i => new
            {
                id = i.Id,
                name = i.Name,
                costPrice = i.CostPrice,
                retailPrice = i.RetailPrice,
                wholesalePrice = i.WholesalePrice,
                price = i.WholesalePrice > 0 ? i.WholesalePrice : i.RetailPrice,
                stock = i.CurrentStock,
                inStock = i.CurrentStock > 0,
                brand = i.BrandName,
                category = i.DeviceCategoryName,
                model = i.DeviceModelName,
                sku = i.SKU,
                description = i.Description,
                notes = i.Notes
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} inventory parts for brand={BrandId} category={CategoryId} model={ModelId}",
                data.Count, brandId, categoryId, modelId);

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory parts");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// AJAX endpoint: Save a repair order submitted from the Create page JS form.
    /// Accepts JSON body; uses X-Requested-With header instead of antiforgery token.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SaveRepairJson([FromBody] SaveRepairJsonRequest request)
    {
        if (!IsAuthenticated())
            return Json(new { success = false, message = "Not authenticated" });
        try
        {
            var currentShopId = GetCurrentShopId();
            // Validate stock for each part before saving
            if (request.Parts != null && request.Parts.Any())
            {
                var allItems = await _inventoryApiService.GetInventoryAsync(currentShopId);
                foreach (var part in request.Parts)
                {
                    var item = allItems?.FirstOrDefault(i => i.Id == part.InventoryItemId);
                    if (item == null)
                        return Json(new { success = false, message = $"Part ID {part.InventoryItemId} not found in inventory." });
                    if (part.Quantity > item.CurrentStock)
                        return Json(new { success = false, message = $"Insufficient stock for '{item.Name}': requested {part.Quantity}, available {item.CurrentStock}." });
                }
            }
            // Build the repair create request
            var createRequest = new CreateRepairRequest
            {
                ShopId = currentShopId,
                CustomerId = request.CustomerId,
                BrandId = request.BrandId > 0 ? request.BrandId : (int?)null,
                DeviceCategoryId = request.DeviceCategoryId > 0 ? request.DeviceCategoryId : (int?)null,
                DeviceModelId = request.DeviceModelId > 0 ? request.DeviceModelId : (int?)null,
                Description = request.Description ?? string.Empty,
                Comments = request.Comments ?? string.Empty,
                Cost = request.Cost > 0 ? request.Cost : 0m,
                AmountPaid = request.AmountPaid >= 0 ? request.AmountPaid : 0m,
                // Map the string from JS ("Unpaid", "Paid", "Partial") to the enum
                PaymentStatus = (request.PaymentStatus ?? "Unpaid").ToLower() switch
                {
                    "paid"    => PaymentStatus.Paid,
                    "partial" => PaymentStatus.Partial,
                    _         => PaymentStatus.Unpaid
                },
                CompanyId = request.CompanyId > 0 ? request.CompanyId : (int?)null
            };
            var repair = await _repairApiService.CreateAsync(createRequest);
            if (repair == null)
                return Json(new { success = false, message = ValidationMessages.Repair.ErrorCreatingRepair });
            // Add parts to the repair (with quantity and unit price per part)
            if (request.Parts != null && request.Parts.Any())
            {
                var addPartsRequest = new AddRepairPartsRequest
                {
                    Parts = request.Parts.Select(p => new RepairPartRequest
                    {
                        InventoryItemId = p.InventoryItemId,
                        Quantity = p.Quantity > 0 ? p.Quantity : 1,
                        UnitPrice = p.UnitPrice,
                        PartName = p.PartName
                    }).ToList(),
                    ShopId = currentShopId,
                    UserId = GetCurrentUserId()
                };
                await _repairApiService.AddPartsToRepairAsync(repair.Id, addPartsRequest);
            }
            // Persist individual payment/refund entries from the Create form
            if (request.Payments != null && request.Payments.Any())
            {
                var userId = GetCurrentUserId();
                foreach (var p in request.Payments)
                {
                    if (p.Amount <= 0) continue;
                    DateTime? paidAt = null;
                    if (!string.IsNullOrWhiteSpace(p.PaidAt) && DateTime.TryParse(p.PaidAt, out var pd))
                        paidAt = pd;
                    await _repairApiService.AddRepairPaymentAsync(repair.Id, new AddRepairPaymentRequestDto
                    {
                        Type          = p.Type ?? "Payment",
                        Amount        = p.Amount,
                        PaymentMethod = p.PaymentMethod ?? "Cash",
                        Reference     = p.Reference,
                        Notes         = p.Notes,
                        PaidAt        = paidAt
                    });
                }
            }
            _logger.LogInformation("Repair {RepairNumber} saved via JSON endpoint by user {UserId}",
                repair.RepairNumber, GetCurrentUserId());
            return Json(new { success = true, message = "Repair saved successfully!", repairId = repair.Id, repairNumber = repair.RepairNumber });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving repair via JSON endpoint");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers(string searchTerm = AppConstants.EmptyString)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            var customers = await _customerApiService.GetByShopAsync(currentShopId);
            
            if (customers == null)
            {
                return Json(new { success = false, message = ValidationMessages.Repair.FailedToRetrieveCustomers });
            }

            // Filter by search term if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                customers = customers.Where(c =>
                    (c.FirstName != null && c.FirstName.ToLower().Contains(searchTerm)) ||
                    (c.LastName != null && c.LastName.ToLower().Contains(searchTerm)) ||
                    (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm))
                ).ToList();
            }

            var data = customers.Select(c => new
            {
                id = c.Id,
                firstName = c.FirstName,
                lastName = c.LastName,
                phone = c.Phone,
                email = c.Email,
                address = c.Address
            }).ToList();

            _logger.LogInformation("Retrieved {Count} customers", data.Count);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers");
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Load view data for create/edit repair forms
    /// </summary>
    private async Task LoadCreateViewDataAsync(int shopId)
    {
        try
        {
            var customers = await _customerApiService.GetByShopAsync(shopId);
            var devices = await _deviceApiService.GetByShopAsync(shopId);

            ViewBag.Customers = customers?.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.FirstName} {c.LastName} - {c.Phone}"
            }).ToList() ?? new List<SelectListItem>();

            ViewBag.Devices = devices?.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.DisplayName ?? $"{d.Brand} {d.Model}"
            }).ToList() ?? new List<SelectListItem>();

            // Load lookup values using BaseController method
            ViewBag.RepairStatuses = await GetLookupSelectListAsync(LookupCategories.RepairStatus);
            ViewBag.PaymentStatuses = await GetLookupSelectListAsync(LookupCategories.PaymentStatus);

            _logger.LogInformation(
                "Loaded {CustomerCount} customers, {DeviceCount} devices for repair form",
                customers?.Count() ?? 0, devices?.Count() ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create view data");
            ViewBag.Customers = new List<SelectListItem>();
            ViewBag.Devices = new List<SelectListItem>();
            ViewBag.RepairStatuses = new List<SelectListItem>();
            ViewBag.PaymentStatuses = new List<SelectListItem>();
        }
    }

    /// <summary>
    /// Process repair parts and manage inventory transactions
    /// </summary>
    private async Task ProcessRepairPartsAsync(List<int> inventoryPartIds, RepairDto repair, int shopId)
    {
        try
        {
            var request = new AddRepairPartsRequest
            {
                InventoryPartIds = inventoryPartIds,
                ShopId = shopId,
                UserId = GetCurrentUserId()
            };

            await _repairApiService.AddPartsToRepairAsync(repair.Id, request);
            
            _logger.LogInformation(
                "Parts added to repair {RepairNumber} via API",
                repair.RepairNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing repair parts for repair {RepairId}", repair.Id);
            throw;
        }
    }

    /// <summary>
    /// Generic method to get lookup values as JSON for AJAX calls
    /// </summary>
    private async Task<IActionResult> GetLookupJsonAsync(string category, string friendlyName)
    {
        try
        {
            var lookupValues = await GetLookupValuesAsync(category);
            
            if (lookupValues == null)
            {
                return Json(new { success = false, message = $"Failed to retrieve {friendlyName}" });
            }

            var data = lookupValues.Select(lv => new
            {
                id = lv.Id,
                name = lv.Value,
                icon = lv.Icon,
                color = lv.ColorCode
            }).ToList();

            _logger.LogInformation("Retrieved {Count} {Name}", data.Count, friendlyName);
            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {Name}", friendlyName);
            return Json(new { success = false, message = ex.Message });
        }
    }

    #endregion

    /// <summary>
    /// AJAX endpoint: Get parts used in a repair (for Edit page pre-population).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRepairParts(int repairId)
    {
        try
        {
            var parts = await _repairApiService.GetRepairPartsAsync(repairId);
            return Json(new { success = true, data = parts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting parts for repair {RepairId}", repairId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// AJAX endpoint: Get existing payment history for a repair (for Edit page pre-population).
    /// Returns a list of payment lines with method, amount, reference, and date.
    /// </summary>
    [HttpGet]
    public IActionResult GetExistingPayments(int repairId)
    {
        // Payments are stored in the Repair's PaymentStatus field only (no separate payment table for repairs).
        // Return an empty list — the view will show the current payment status and allow adding new payments.
        // The total paid is derived from the repair's Cost and PaymentStatus on the Edit page.
        return Json(new { success = true, data = new List<object>() });
    }

    /// <summary>
    /// AJAX endpoint: Get the full activity / audit history for a repair.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRepairActivities(int repairId)
    {
        try
        {
            var activities = await _repairApiService.GetRepairActivitiesAsync(repairId);
            return Json(new { success = true, data = activities });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activities for repair {RepairId}", repairId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    // ── Payment Ledger AJAX Endpoints ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetRepairPayments(int repairId)
    {
        try
        {
            var payments = await _repairApiService.GetRepairPaymentsAsync(repairId);
            return Json(new { success = true, data = payments });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for repair {RepairId}", repairId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> AddRepairPayment([FromBody] AddRepairPaymentWebRequest request)
    {
        try
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request" });
            var dto = new AddRepairPaymentRequestDto
            {
                Type          = request.Type ?? "Payment",
                Amount        = request.Amount,
                PaymentMethod = request.PaymentMethod ?? "Cash",
                Reference     = request.Reference,
                Notes         = request.Notes,
                PaidAt        = request.PaidAt
            };
            var payment = await _repairApiService.AddRepairPaymentAsync(request.RepairId, dto);
            if (payment == null)
                return Json(new { success = false, message = "Failed to add payment" });
            return Json(new { success = true, data = payment });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to repair");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateRepairPayment([FromBody] UpdateRepairPaymentWebRequest request)
    {
        try
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request" });
            var dto = new UpdateRepairPaymentRequestDto
            {
                Amount        = request.Amount,
                PaymentMethod = request.PaymentMethod ?? "Cash",
                Reference     = request.Reference,
                Notes         = request.Notes,
                PaidAt        = request.PaidAt
            };
            var payment = await _repairApiService.UpdateRepairPaymentAsync(request.PaymentId, dto);
            if (payment == null)
                return Json(new { success = false, message = "Payment not found" });
            return Json(new { success = true, data = payment });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId}", request?.PaymentId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteRepairPayment([FromBody] DeleteRepairPaymentWebRequest request)
    {
        try
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request" });
            var success = await _repairApiService.DeleteRepairPaymentAsync(request.PaymentId);
            return Json(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment {PaymentId}", request?.PaymentId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// AJAX endpoint: Update a repair from the Edit page JS form.
    /// Replaces parts list and records new payment entries.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateRepairJson([FromBody] UpdateRepairJsonRequest request)
    {
        try
        {
            if (request == null)
                return Json(new { success = false, message = "Invalid request" });

            var currentShopId = GetCurrentShopId();

            // If paymentStatus is null/empty, leave it null so RecalculateAmountPaidAsync owns it.
            // Only accept an explicit override if the caller sends one.
            string? paymentStatusStr = null;
            if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            {
                paymentStatusStr = request.PaymentStatus.ToLower() switch
                {
                    "paid"     => "Paid",
                    "partial"  => "Partial",
                    "refunded" => "Refunded",
                    _          => null   // ignore unknown values — let server recalculate
                };
            }

            // Map repair status string (from JS) to stored format
            var statusStr = (request.Status ?? string.Empty).ToLower() switch
            {
                "in-progress"  => "InProgress",
                "inprogress"   => "InProgress",
                "pending"      => "Pending",
                "waiting"      => "WaitingParts",
                "waitingparts" => "WaitingParts",
                "ready"        => "ReadyForPickup",
                "completed"    => "Completed",
                "cancelled"    => "Cancelled",
                "on-hold"      => "OnHold",
                "onhold"       => "OnHold",
                "unrepairable" => "Unrepairable",
                "abandoned"    => "Abandoned",
                var s when !string.IsNullOrWhiteSpace(s) => s,
                _ => null
            };

            var updateRequest = new UpdateRepairRequestDto
            {
                BrandId         = request.BrandId > 0 ? request.BrandId : (int?)null,
                DeviceCategoryId= request.DeviceCategoryId > 0 ? request.DeviceCategoryId : (int?)null,
                DeviceModelId   = request.DeviceModelId > 0 ? request.DeviceModelId : (int?)null,
                Description     = request.Description,
                Comments        = request.Comments,
                Cost            = request.Cost > 0 ? request.Cost : (decimal?)null,
                // AmountPaid intentionally null — RecalculateAmountPaidAsync (called when
                // PaymentStatus == null) always derives AmountPaid from the payment ledger.
                AmountPaid      = null,
                Status          = statusStr,
                PaymentStatus   = paymentStatusStr
            };

            var repair = await _repairApiService.UpdateAsync(request.RepairId, updateRequest);
            if (repair == null)
                return Json(new { success = false, message = "Repair not found or update failed" });

            // Always replace the full parts set (removes old parts, restores stock, adds new set)
            var replacePartsRequest = new AddRepairPartsRequest
            {
                Parts = (request.Parts ?? new List<RepairPartRequest>()).Select(p => new RepairPartRequest
                {
                    InventoryItemId = p.InventoryItemId,
                    Quantity        = p.Quantity > 0 ? p.Quantity : 1,
                    UnitPrice       = p.UnitPrice,
                    PartName        = p.PartName
                }).ToList(),
                ShopId = currentShopId,
                UserId = GetCurrentUserId()
            };
            await _repairApiService.ReplacePartsAsync(repair.Id, replacePartsRequest);

            _logger.LogInformation("Repair {RepairId} updated via JSON endpoint by user {UserId}",
                repair.Id, GetCurrentUserId());
            return Json(new { success = true, message = "Repair updated successfully!", repairId = repair.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating repair via JSON");
            return Json(new { success = false, message = "An error occurred while updating the repair" });
        }
    }
}
