using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Empire.Web.Services;
using Empire.Web.Services.API;
using Empire.Web.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Empire.Web.Services.Lookup;
using Empire.Web.Services.Brand;
using Empire.Web.Services.DeviceCategory;
using Empire.Web.Services.Customer;
using Empire.Web.Services.Sales;
using Empire.Web.Services.Helper;
using Empire.Web.DTOs.Common;
using Empire.Web.DTOs.Reports;
using Empire.Web.DTOs.Lookup;
using Empire.Web.DTOs.Sale;
namespace Empire.Web.Controllers;

public class ReportsController : BaseController
{
    private readonly ILogger<ReportsController> _logger;
    private readonly ILookupApiService _lookupApiService;
    private readonly IBrandApiService _brandApi;
    private readonly IDeviceCategoryApiService _deviceCategoryApi;
    private readonly ICustomerApiService _customerApi;
    private readonly ISalesApiService _salesApi;
    private readonly IHelperService _helperService;

    public ReportsController(ILogger<ReportsController> logger,
        ILookupApiService lookupApiService,
        IBrandApiService brandApi,
        IDeviceCategoryApiService deviceCategoryApi,
        ICustomerApiService customerApi,
        ISalesApiService salesApi,
        IHelperService helperService,
        ITimezoneService timezoneService) : base(logger, timezoneService)
    {
        _logger = logger;
        _lookupApiService = lookupApiService;
        _brandApi = brandApi;
        _deviceCategoryApi = deviceCategoryApi;
        _customerApi = customerApi;
        _salesApi = salesApi;
        _helperService = helperService;
    }

    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Dashboard", "Home");

        return View();
    }

    public async Task<IActionResult> ProfitReport()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                ViewBag.Brands = new List<object>();
                ViewBag.Categories = new List<object>();
                ViewBag.Customers = new List<object>();
                TempData["Error"] = "Invalid shop session. CurrentShopId is 0. Session UserId: " + HttpContext.Session.GetInt32("UserId") + ", CurrentShopId: " + HttpContext.Session.GetString("CurrentShopId");
                return View();
            }

            // Load filter dropdowns using HelperService
            var brands = await _brandApi.GetAllAsync();
            ViewBag.Brands = _helperService.CreateDropdownList(
                brands ?? Enumerable.Empty<dynamic>(),
                b => b.Id,
                b => b.Name,
                b => b.IsActive);

            var categories = await _deviceCategoryApi.GetAllAsync();
            ViewBag.Categories = _helperService.CreateDropdownList(
                categories ?? Enumerable.Empty<dynamic>(),
                c => c.Id,
                c => c.Name,
                c => c.IsActive);

            var customers = await _customerApi.GetByShopAsync(currentShopId);
            ViewBag.Customers = _helperService.CreateDropdownList(
                customers ?? Enumerable.Empty<dynamic>(),
                c => c.Id,
                c => _helperService.FormatFullName(c.FirstName, c.LastName));

            // Load payment statuses from lookup API (safe — empty list if not seeded yet)
            try
            {
                var paymentStatuses = await _lookupApiService.GetByCategoryAsync("PaymentStatus");
                ViewBag.PaymentStatuses = (paymentStatuses ?? Enumerable.Empty<LookupValueDto>())
                    .Select(ps => new SelectListItem { Value = ps.Value, Text = ps.Value })
                    .ToList();
            }
            catch
            {
                ViewBag.PaymentStatuses = new List<SelectListItem>();
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profit report page");
            TempData["Error"] = "Error loading profit report: " + ex.Message;
            return RedirectToAction("Dashboard", "Home");
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetProfitData(
        string? dateFrom,
        string? dateTo,
        int? brandId,
        int? categoryId,
        int? customerId,
        string? paymentStatus)
    {
        try
        {
            var currentShopId = GetCurrentShopId();

            // Parse dates
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
                fromDate = from.Date;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to.Date.AddDays(1).AddTicks(-1);

            var startDate = fromDate ?? DateTime.UtcNow.Date.AddMonths(-1);
            var endDate   = toDate   ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var report = await _salesApi.GetProfitReportAsync(currentShopId, startDate, endDate);
            if (report == null)
            {
                return Json(new { success = false, message = "Error generating profit report" });
            }

            var profitData = report.Items.Select(item => new
            {
                saleDate = item.SaleDate,
                invoiceNumber = item.InvoiceNumber,
                customer = item.Customer,
                itemName = item.ItemName,
                brand = item.Brand,
                category = item.Category,
                quantity = item.Quantity,
                revenue = item.Revenue,
                cost = item.Cost,
                profit = item.Profit,
                margin = item.Margin,
                paymentStatus = item.PaymentStatus
            });

            var summary = new
            {
                totalRevenue = report.Summary.TotalRevenue,
                totalCost = report.Summary.TotalCost,
                totalProfit = report.Summary.TotalProfit,
                profitMargin = report.Summary.ProfitMargin,
                salesCount = report.Summary.SalesCount,
                itemsSold = report.Summary.ItemsSold
            };

            return Json(new { success = true, data = profitData, summary });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating profit report");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetSalesSummary(string? dateFrom, string? dateTo)
    {
        try
        {
            var currentShopId = GetCurrentShopId();

            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
                fromDate = from.Date;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to.Date.AddDays(1).AddTicks(-1);

            var summary = await _salesApi.GetSaleSummaryAsync(currentShopId, fromDate ?? DateTime.UtcNow.Date.AddMonths(-1), toDate ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1));
            if (summary == null)
            {
                return Json(new { success = false, message = "Error getting sales summary" });
            }

            var data = new
            {
                totalSales = summary.TotalSales,
                totalRevenue = summary.TotalRevenue,
                paidSales = summary.PaidSales,
                pendingSales = summary.PendingSales,
                paidAmount = summary.PaidAmount,
                pendingAmount = summary.PendingAmount
            };

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales summary");
            return Json(new { success = false, message = ex.Message });
        }
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Session.GetInt32("UserId").HasValue;
    }

    /// <summary>
    /// Sales Report Page
    /// </summary>
    public async Task<IActionResult> SalesReport()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                ViewBag.Customers = new List<object>();
                ViewBag.Users = new List<object>();
                TempData["Error"] = "Invalid shop session. CurrentShopId is 0. Session UserId: " + HttpContext.Session.GetInt32("UserId") + ", CurrentShopId: " + HttpContext.Session.GetString("CurrentShopId");
                return View();
            }

            // Load filter dropdowns from API
            var customers = await _customerApi.GetByShopAsync(currentShopId);
            ViewBag.Customers = customers?
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .Select(c => new DropdownItemDto { Id = c.Id, Name = $"{c.FirstName} {c.LastName}" })
                .ToList() ?? new List<DropdownItemDto>(); // Note: Users would need to come from UserApiService (to be created if needed)
            ViewBag.Users = new List<object>(); // Placeholder - implement UserApiService if needed

            // Load payment statuses from lookup API (safe — empty list if not seeded yet)
            try
            {
                var paymentStatuses = await _lookupApiService.GetByCategoryAsync("PaymentStatus");
                ViewBag.PaymentStatuses = paymentStatuses?.Select(ps => new SelectListItem
                {
                    Value = ps.Value,
                    Text = ps.Value
                }).ToList() ?? new List<SelectListItem>();
            }
            catch
            {
                ViewBag.PaymentStatuses = new List<SelectListItem>();
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sales report page");
            TempData["Error"] = "Error loading sales report: " + ex.Message;
            return RedirectToAction("Dashboard", "Home");
        }
    }

    /// <summary>
    /// Get comprehensive sales report data with filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSalesReportData(
        string? dateFrom,
        string? dateTo,
        int? customerId,
        string? paymentStatus,
        string? paymentMethod,
        string? itemType,
        int? userId)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                return Json(new { success = false, message = "Invalid shop session" });
            }

            // Parse dates
            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var from))
                fromDate = from.Date; // Start of day

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to.Date.AddDays(1).AddTicks(-1); // End of that day (23:59:59.9999999)

            // Default: last month start-of-day to end of today
            var startDate = fromDate ?? DateTime.UtcNow.Date.AddMonths(-1);
            var endDate   = toDate   ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            var report = await _salesApi.GetSalesReportAsync(currentShopId, startDate, endDate);
            if (report == null)
            {
                return Json(new { success = false, message = "Error generating sales report" });
            }

            var result = new
            {
                success = true,
                summary = new
                {
                    totalRevenue = report.Summary.TotalRevenue,
                    totalSales   = report.Summary.TotalSales,
                    totalPaid    = report.Summary.TotalPaid,
                    totalPartial = report.Summary.TotalPartial,
                    totalUnpaid  = report.Summary.TotalUnpaid,
                    averageSale  = report.Summary.AverageSale
                },
                charts = new
                {
                    salesByDate = report.SalesByDate.Select(x => new
                    {
                        date    = x.Date,
                        count   = x.Count,
                        revenue = x.Revenue
                    }),
                    salesByPaymentMethod = report.SalesByPaymentMethod.Select(x => new
                    {
                        method = x.Method,
                        amount = x.Amount,
                        count  = x.Count
                    }),
                    topItems = report.TopItems.Select(x => new
                    {
                        name     = x.Name,
                        quantity = x.Quantity,
                        revenue  = x.Revenue
                    }),
                    salesByStatus = report.SalesByStatus.Select(x => new
                    {
                        status = x.Status,
                        count  = x.Count,
                        amount = x.Amount
                    }),
                    itemsByType = new List<object>()  // not tracked at sale level
                },
                sales = report.Sales.Select(s => new
                {
                    id             = s.Id,
                    invoiceNumber  = s.InvoiceNumber,
                    date           = s.SaleDate,
                    customer       = s.CustomerName ?? "",
                    items          = s.ItemCount,
                    subtotal       = s.SubTotal,
                    tax            = s.TaxAmount,
                    discount       = s.DiscountAmount,
                    total          = s.TotalAmount,
                    paymentStatus  = s.PaymentStatus,
                    paymentMethods = s.Payments != null
                        ? string.Join(", ", s.Payments.Select(p => p.PaymentMethod).Distinct())
                        : s.PaymentMethod,
                    totalPaid      = s.Payments != null ? s.Payments.Sum(p => p.Amount) : 0m,
                    balance        = Math.Max(0m, s.TotalAmount - (s.Payments != null ? s.Payments.Sum(p => p.Amount) : 0m)),
                    cashier        = ""  // not tracked on SaleDto
                })
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales report");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get sales analytics data grouped by day/month/year for the analytics chart.
    /// Returns: { success, summary: { totalInventoryCost, totalSales, totalProfit, profitMargin },
    ///            data: [ { displayLabel, inventoryCost, sales, profit } ] }
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSalesAnalytics(
        string? dateFrom,
        string? dateTo,
        string? groupBy)
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
                return Json(new { success = false, message = "Invalid shop session" });

            // Parse date range
            DateTime? fromDate = null;
            DateTime? toDate   = null;
            if (!string.IsNullOrEmpty(dateFrom) && DateTime.TryParse(dateFrom, out var fd)) fromDate = fd.Date;
            if (!string.IsNullOrEmpty(dateTo)   && DateTime.TryParse(dateTo,   out var td)) toDate   = td.Date.AddDays(1).AddTicks(-1);

            var startDate = fromDate ?? DateTime.UtcNow.Date.AddMonths(-1);
            var endDate   = toDate   ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);

            // Reuse the profit report which already has cost + revenue per sale-item
            var profitReport = await _salesApi.GetProfitReportAsync(currentShopId, startDate, endDate);
            if (profitReport == null)
                return Json(new { success = false, message = "Error generating analytics" });

            var items = profitReport.Items ?? new List<ProfitReportItemDto>();

            // Group by the requested period
            var gb = (groupBy ?? "day").ToLower();

            var grouped = gb switch
            {
                "month" => items.GroupBy(i => new { i.SaleDate.Year, i.SaleDate.Month })
                               .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                               .Select(g => new
                               {
                                   displayLabel  = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                                   inventoryCost = g.Sum(i => i.Cost),
                                   sales         = g.Sum(i => i.Revenue),
                                   profit        = g.Sum(i => i.Profit)
                               }).Cast<object>().ToList(),
                "year"  => items.GroupBy(i => i.SaleDate.Year)
                               .OrderBy(g => g.Key)
                               .Select(g => new
                               {
                                   displayLabel  = g.Key.ToString(),
                                   inventoryCost = g.Sum(i => i.Cost),
                                   sales         = g.Sum(i => i.Revenue),
                                   profit        = g.Sum(i => i.Profit)
                               }).Cast<object>().ToList(),
                _       => items.GroupBy(i => i.SaleDate.Date)
                               .OrderBy(g => g.Key)
                               .Select(g => new
                               {
                                   displayLabel  = g.Key.ToString("MMM dd"),
                                   inventoryCost = g.Sum(i => i.Cost),
                                   sales         = g.Sum(i => i.Revenue),
                                   profit        = g.Sum(i => i.Profit)
                               }).Cast<object>().ToList()
            };

            var totalCost    = items.Sum(i => i.Cost);
            var totalSales   = items.Sum(i => i.Revenue);
            var totalProfit  = items.Sum(i => i.Profit);
            var profitMargin = totalSales > 0 ? Math.Round(totalProfit / totalSales * 100, 2) : 0m;

            return Json(new
            {
                success = true,
                summary = new
                {
                    totalInventoryCost = totalCost,
                    totalSales         = totalSales,
                    totalProfit        = totalProfit,
                    profitMargin       = profitMargin
                },
                data = grouped
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales analytics");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Inventory Report Page
    /// </summary>
    public async Task<IActionResult> InventoryReport()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                ViewBag.Brands = new List<object>();
                ViewBag.Categories = new List<object>();
                TempData["Error"] = "Invalid shop session";
                return View();
            }

            // Load filter dropdowns from API
            var brands = await _brandApi.GetAllAsync();
            ViewBag.Brands = brands?
                .Where(b => b.IsActive)
                .OrderBy(b => b.Name)
                .Select(b => new DropdownItemDto { Id = b.Id, Name = b.Name })
                .ToList() ?? new List<DropdownItemDto>();

            var categories = await _deviceCategoryApi.GetAllAsync();
            ViewBag.Categories = categories?
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new DropdownItemDto { Id = c.Id, Name = c.Name })
                .ToList() ?? new List<DropdownItemDto>();

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading inventory report page");
            TempData["Error"] = "Error loading inventory report: " + ex.Message;
            return RedirectToAction("Dashboard", "Home");
        }
    }

    /// <summary>
    /// Repair Report Page
    /// </summary>
    public async Task<IActionResult> RepairReport()
    {
        try
        {
            var currentShopId = GetCurrentShopId();
            if (currentShopId == 0)
            {
                ViewBag.Customers = new List<object>();
                TempData["Error"] = "Invalid shop session";
                return View();
            }

            // Load filter dropdowns from API
            var customers = await _customerApi.GetByShopAsync(currentShopId);
            ViewBag.Customers = customers?
                .OrderBy(c => c.FirstName)
                .Select(c => new DropdownItemDto { Id = c.Id, Name = c.FirstName + " " + c.LastName })
                .ToList() ?? new List<DropdownItemDto>();

            // Load repair statuses from lookup API (safe — empty list if not seeded yet)
            try
            {
                var repairStatuses = await _lookupApiService.GetByCategoryAsync("RepairStatus");
                ViewBag.RepairStatuses = repairStatuses?.Select(rs => new SelectListItem
                {
                    Value = rs.Value,
                    Text = rs.Value
                }).ToList() ?? new List<SelectListItem>();
            }
            catch
            {
                ViewBag.RepairStatuses = new List<SelectListItem>();
            }

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading repair report page");
            TempData["Error"] = "Error loading repair report: " + ex.Message;
            return RedirectToAction("Dashboard", "Home");
        }
    }

    // ── Sale Payment Management (called from SalesReport page) ───────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetSaleDetail(int id)
    {
        try
        {
            var detail = await _salesApi.GetSaleDetailAsync(id);
            if (detail == null)
                return Json(new { success = false, message = "Sale not found" });
            return Json(new { success = true, data = detail });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale detail {Id}", id);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddSalePayment([FromBody] AddSalePaymentWebRequest request)
    {
        try
        {
            var result = await _salesApi.AddSalePaymentAsync(request.SaleId, request.PaymentMethod, request.Amount, request.TransactionId);
            if (result == null)
                return Json(new { success = false, message = "Failed to add payment" });
            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to sale {SaleId}", request.SaleId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSalePayment([FromBody] UpdateSalePaymentWebRequest request)
    {
        try
        {
            var ok = await _salesApi.UpdateSalePaymentAsync(request.PaymentId, request.PaymentMethod, request.Amount, request.TransactionId);
            return Json(new { success = ok });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sale payment {PaymentId}", request.PaymentId);
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSalePayment(int paymentId)
    {
        try
        {
            var ok = await _salesApi.DeleteSalePaymentAsync(paymentId);
            return Json(new { success = ok });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sale payment {PaymentId}", paymentId);
            return Json(new { success = false, message = ex.Message });
        }
    }
}
