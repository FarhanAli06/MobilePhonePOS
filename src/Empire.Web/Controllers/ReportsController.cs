using Microsoft.AspNetCore.Mvc;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Empire.Web.Controllers;

public class ReportsController : Controller
{
    private readonly EmpireDbContext _context;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(EmpireDbContext context, ILogger<ReportsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        if (!IsAuthenticated())
            return RedirectToAction("Dashboard", "Home");

        return View();
    }

    public async Task<IActionResult> ProfitReport()
    {
        // Temporarily disabled for debugging 302 redirect
        // if (!IsAuthenticated())
        //     return RedirectToAction("Dashboard", "Home");

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

            // Load filter dropdowns
        ViewBag.Brands = await _context.Brands
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Name })
            .ToListAsync();

        ViewBag.Categories = await _context.DeviceCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        ViewBag.Customers = await _context.Customers
            .Where(c => c.ShopId == currentShopId)
            .OrderBy(c => c.FirstName)
            .Select(c => new { c.Id, Name = c.FirstName + " " + c.LastName })
            .ToListAsync();

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
                fromDate = from;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to.AddDays(1); // Include entire day

            // Query sales
            var salesQuery = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.InventoryItem)
                        .ThenInclude(ii => ii.Brand)
                .Include(s => s.SaleItems)
                    .ThenInclude(si => si.InventoryItem)
                        .ThenInclude(ii => ii.DeviceCategory)
                .Where(s => s.ShopId == currentShopId)
                .AsQueryable();

            // Apply filters
            if (fromDate.HasValue)
                salesQuery = salesQuery.Where(s => s.SaleDate >= fromDate.Value);

            if (toDate.HasValue)
                salesQuery = salesQuery.Where(s => s.SaleDate < toDate.Value);

            if (customerId.HasValue && customerId.Value > 0)
                salesQuery = salesQuery.Where(s => s.CustomerId == customerId.Value);

            if (!string.IsNullOrEmpty(paymentStatus))
                salesQuery = salesQuery.Where(s => s.PaymentStatus == paymentStatus);

            var sales = await salesQuery.ToListAsync();

            // Calculate profit for each sale
            var profitData = new List<object>();
            decimal totalRevenue = 0;
            decimal totalCost = 0;
            decimal totalProfit = 0;

            foreach (var sale in sales)
            {
                foreach (var saleItem in sale.SaleItems)
                {
                    // Apply brand and category filters
                    if (brandId.HasValue && brandId.Value > 0 && 
                        saleItem.InventoryItem?.BrandId != brandId.Value)
                        continue;

                    if (categoryId.HasValue && categoryId.Value > 0 && 
                        saleItem.InventoryItem?.DeviceCategoryId != categoryId.Value)
                        continue;

                    var revenue = saleItem.UnitPrice * saleItem.Quantity;
                    var cost = saleItem.CostPrice * saleItem.Quantity; // Use captured cost at time of sale
                    var profit = revenue - cost;
                    var margin = revenue > 0 ? (profit / revenue * 100) : 0;

                    totalRevenue += revenue;
                    totalCost += cost;
                    totalProfit += profit;

                    profitData.Add(new
                    {
                        saleDate = sale.SaleDate,
                        invoiceNumber = sale.SaleNumber,
                        customer = sale.Customer != null ? sale.Customer.FirstName + " " + sale.Customer.LastName : "Walk-in",
                        itemName = saleItem.InventoryItem?.Name ?? "Custom Item",
                        brand = saleItem.InventoryItem?.Brand?.Name ?? "-",
                        category = saleItem.InventoryItem?.DeviceCategory?.Name ?? "-",
                        quantity = saleItem.Quantity,
                        revenue = revenue,
                        cost = cost,
                        profit = profit,
                        margin = margin,
                        paymentStatus = sale.PaymentStatus
                    });
                }
            }

            var summary = new
            {
                totalRevenue = totalRevenue,
                totalCost = totalCost,
                totalProfit = totalProfit,
                profitMargin = totalRevenue > 0 ? (totalProfit / totalRevenue * 100) : 0,
                salesCount = sales.Count,
                itemsSold = profitData.Count
            };

            return Json(new { success = true, data = profitData, summary = summary });
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
                fromDate = from;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to.AddDays(1);

            var query = _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.ShopId == currentShopId)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(s => s.SaleDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.SaleDate < toDate.Value);

            var sales = await query.ToListAsync();

            var summary = new
            {
                totalSales = sales.Count,
                totalRevenue = sales.Sum(s => s.TotalAmount),
                paidSales = sales.Count(s => s.PaymentStatus == "Paid"),
                pendingSales = sales.Count(s => s.PaymentStatus == "Pending"),
                paidAmount = sales.Where(s => s.PaymentStatus == "Paid").Sum(s => s.TotalAmount),
                pendingAmount = sales.Where(s => s.PaymentStatus == "Pending").Sum(s => s.TotalAmount)
            };

            return Json(new { success = true, data = summary });
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

    private int GetCurrentShopId()
    {
        var shopIdString = HttpContext.Session.GetString("CurrentShopId");
        if (int.TryParse(shopIdString, out int shopId) && shopId > 0)
        {
            return shopId;
        }
        return 0;
    }

    /// <summary>
    /// Sales Report Page
    /// </summary>
    public async Task<IActionResult> SalesReport()
    {
        // Temporarily disabled for debugging 302 redirect
        // if (!IsAuthenticated())
        //     return RedirectToAction("Dashboard", "Home");

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

            // Load filter dropdowns
            ViewBag.Customers = await _context.Customers
                .Where(c => c.ShopId == currentShopId)
                .OrderBy(c => c.FirstName)
                .Select(c => new { c.Id, Name = c.FirstName + " " + c.LastName })
                .ToListAsync();

            // Get users who have access to this shop through UserShopRoles
            ViewBag.Users = await _context.UserShopRoles
                .Where(usr => usr.ShopId == currentShopId && usr.IsActive)
                .Select(usr => new { usr.User.Id, usr.User.Username })
                .Distinct()
                .OrderBy(u => u.Username)
                .ToListAsync();

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
                fromDate = from;

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to.AddDays(1); // Include the entire day

            // Build query
            var query = _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Include(s => s.Payments)
                .Include(s => s.CreatedByUser)
                .Where(s => s.ShopId == currentShopId)
                .AsQueryable();

            // Apply filters
            if (fromDate.HasValue)
                query = query.Where(s => s.SaleDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(s => s.SaleDate < toDate.Value);

            if (customerId.HasValue)
                query = query.Where(s => s.CustomerId == customerId.Value);

            if (!string.IsNullOrEmpty(paymentStatus))
                query = query.Where(s => s.PaymentStatus == paymentStatus);

            if (!string.IsNullOrEmpty(paymentMethod))
                query = query.Where(s => s.Payments.Any(p => p.PaymentMethod == paymentMethod));

            if (userId.HasValue)
                query = query.Where(s => s.CreatedByUserId == userId.Value);

            var sales = await query.OrderByDescending(s => s.SaleDate).ToListAsync();

            // Filter by item type if specified
            if (!string.IsNullOrEmpty(itemType))
            {
                sales = sales.Where(s => s.SaleItems.Any(item => 
                    (itemType == "Device" && item.ItemName.Contains("Device")) ||
                    (itemType == "Inventory" && !item.ItemName.Contains("Device") && !item.ItemName.Contains("Repair")) ||
                    (itemType == "Repair" && item.ItemName.Contains("Repair"))
                )).ToList();
            }

            // Calculate summary statistics
            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var totalSales = sales.Count;
            var totalPaid = sales.Where(s => s.PaymentStatus == "Paid").Sum(s => s.TotalAmount);
            var totalPartial = sales.Where(s => s.PaymentStatus == "Partial").Sum(s => s.TotalAmount);
            var totalUnpaid = sales.Where(s => s.PaymentStatus == "Unpaid").Sum(s => s.TotalAmount);

            // Sales by date (for line chart)
            var salesByDate = sales
                .GroupBy(s => s.SaleDate.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    count = g.Count(),
                    revenue = g.Sum(s => s.TotalAmount)
                })
                .OrderBy(x => x.date)
                .ToList();

            // Sales by payment method (for pie chart)
            var salesByPaymentMethod = sales
                .SelectMany(s => s.Payments)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new
                {
                    method = g.Key,
                    amount = g.Sum(p => p.Amount),
                    count = g.Count()
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            // Top selling items (for bar chart)
            var topItems = sales
                .SelectMany(s => s.SaleItems)
                .GroupBy(item => item.ItemName)
                .Select(g => new
                {
                    name = g.Key,
                    quantity = g.Sum(i => i.Quantity),
                    revenue = g.Sum(i => i.TotalPrice)
                })
                .OrderByDescending(x => x.revenue)
                .Take(10)
                .ToList();

            // Sales by payment status (for bar chart)
            var salesByStatus = new[]
            {
                new { status = "Paid", count = sales.Count(s => s.PaymentStatus == "Paid"), amount = totalPaid },
                new { status = "Partial", count = sales.Count(s => s.PaymentStatus == "Partial"), amount = totalPartial },
                new { status = "Unpaid", count = sales.Count(s => s.PaymentStatus == "Unpaid"), amount = totalUnpaid }
            };

            // Sales by item type (for donut chart)
            var itemsByType = sales
                .SelectMany(s => s.SaleItems)
                .GroupBy(item => 
                    item.ItemName.Contains("Repair") ? "Repair" :
                    item.ItemName.Contains("Device") ? "Device" : "Inventory")
                .Select(g => new
                {
                    type = g.Key,
                    quantity = g.Sum(i => i.Quantity),
                    revenue = g.Sum(i => i.TotalPrice)
                })
                .ToList();

            // Detailed sales list
            var salesList = sales.Select(s => new
            {
                id = s.Id,
                invoiceNumber = s.SaleNumber,
                date = s.SaleDate.ToString("yyyy-MM-dd HH:mm"),
                customer = s.Customer != null ? $"{s.Customer.FirstName} {s.Customer.LastName}" : "Walk-in",
                items = s.SaleItems.Count,
                subtotal = s.SubTotal,
                tax = s.TaxAmount,
                discount = s.DiscountAmount,
                total = s.TotalAmount,
                paymentStatus = s.PaymentStatus,
                paymentMethods = string.Join(", ", s.Payments.Select(p => p.PaymentMethod)),
                cashier = s.CreatedByUser?.Username ?? "System"
            }).ToList();

            var result = new
            {
                success = true,
                summary = new
                {
                    totalRevenue,
                    totalSales,
                    totalPaid,
                    totalPartial,
                    totalUnpaid,
                    averageSale = totalSales > 0 ? totalRevenue / totalSales : 0
                },
                charts = new
                {
                    salesByDate,
                    salesByPaymentMethod,
                    topItems,
                    salesByStatus,
                    itemsByType
                },
                sales = salesList
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales report data");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Get sales analytics data (Inventory Cost, Sales, Profit) aggregated by day/month/year
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSalesAnalytics(
        string? dateFrom,
        string? dateTo,
        string groupBy = "day") // day, month, year
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
                fromDate = from;
            else
                fromDate = DateTime.Now.AddMonths(-1); // Default to last month

            if (!string.IsNullOrEmpty(dateTo) && DateTime.TryParse(dateTo, out var to))
                toDate = to.AddDays(1); // Include the entire day
            else
                toDate = DateTime.Now.AddDays(1);

            // Build query for sales with items
            var salesQuery = _context.Sales
                .Include(s => s.SaleItems)
                .Where(s => s.ShopId == currentShopId)
                .Where(s => s.SaleDate >= fromDate.Value && s.SaleDate < toDate.Value)
                .AsQueryable();

            var sales = await salesQuery.ToListAsync();

            // Group sales data by the specified period
            var analyticsData = new List<object>();

            if (groupBy.ToLower() == "day")
            {
                // Group by day
                var dailyData = sales
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new
                    {
                        date = g.Key,
                        inventoryCost = g.Sum(s => s.SaleItems.Sum(si => si.CostPrice * si.Quantity)),
                        sales = g.Sum(s => s.TotalAmount),
                        profit = g.Sum(s => s.SaleItems.Sum(si => (si.UnitPrice - si.CostPrice) * si.Quantity))
                    })
                    .OrderBy(x => x.date)
                    .ToList();

                analyticsData = dailyData.Select(d => new
                {
                    period = d.date.ToString("yyyy-MM-dd"),
                    displayLabel = d.date.ToString("MMM dd"),
                    inventoryCost = Math.Round(d.inventoryCost, 2),
                    sales = Math.Round(d.sales, 2),
                    profit = Math.Round(d.profit, 2)
                } as object).ToList();
            }
            else if (groupBy.ToLower() == "month")
            {
                // Group by month
                var monthlyData = sales
                    .GroupBy(s => new { s.SaleDate.Year, s.SaleDate.Month })
                    .Select(g => new
                    {
                        year = g.Key.Year,
                        month = g.Key.Month,
                        inventoryCost = g.Sum(s => s.SaleItems.Sum(si => si.CostPrice * si.Quantity)),
                        sales = g.Sum(s => s.TotalAmount),
                        profit = g.Sum(s => s.SaleItems.Sum(si => (si.UnitPrice - si.CostPrice) * si.Quantity))
                    })
                    .OrderBy(x => x.year).ThenBy(x => x.month)
                    .ToList();

                analyticsData = monthlyData.Select(d => new
                {
                    period = $"{d.year}-{d.month:D2}",
                    displayLabel = new DateTime(d.year, d.month, 1).ToString("MMM yyyy"),
                    inventoryCost = Math.Round(d.inventoryCost, 2),
                    sales = Math.Round(d.sales, 2),
                    profit = Math.Round(d.profit, 2)
                } as object).ToList();
            }
            else if (groupBy.ToLower() == "year")
            {
                // Group by year
                var yearlyData = sales
                    .GroupBy(s => s.SaleDate.Year)
                    .Select(g => new
                    {
                        year = g.Key,
                        inventoryCost = g.Sum(s => s.SaleItems.Sum(si => si.CostPrice * si.Quantity)),
                        sales = g.Sum(s => s.TotalAmount),
                        profit = g.Sum(s => s.SaleItems.Sum(si => (si.UnitPrice - si.CostPrice) * si.Quantity))
                    })
                    .OrderBy(x => x.year)
                    .ToList();

                analyticsData = yearlyData.Select(d => new
                {
                    period = d.year.ToString(),
                    displayLabel = d.year.ToString(),
                    inventoryCost = Math.Round(d.inventoryCost, 2),
                    sales = Math.Round(d.sales, 2),
                    profit = Math.Round(d.profit, 2)
                } as object).ToList();
            }

            // Calculate totals
            var totalInventoryCost = sales.Sum(s => s.SaleItems.Sum(si => si.CostPrice * si.Quantity));
            var totalSales = sales.Sum(s => s.TotalAmount);
            var totalProfit = sales.Sum(s => s.SaleItems.Sum(si => (si.UnitPrice - si.CostPrice) * si.Quantity));
            var profitMargin = totalSales > 0 ? (totalProfit / totalSales * 100) : 0;

            var result = new
            {
                success = true,
                groupBy = groupBy,
                dateRange = new
                {
                    from = fromDate.Value.ToString("yyyy-MM-dd"),
                    to = toDate.Value.AddDays(-1).ToString("yyyy-MM-dd")
                },
                data = analyticsData,
                summary = new
                {
                    totalInventoryCost = Math.Round(totalInventoryCost, 2),
                    totalSales = Math.Round(totalSales, 2),
                    totalProfit = Math.Round(totalProfit, 2),
                    profitMargin = Math.Round(profitMargin, 2)
                }
            };

            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales analytics data");
            return Json(new { success = false, message = ex.Message });
        }
    }
}
