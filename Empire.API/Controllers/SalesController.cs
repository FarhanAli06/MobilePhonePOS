using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Sale;

namespace Empire.API.Controllers;

/// <summary>
/// Sales and POS endpoints
/// </summary>
[Authorize]
public class SalesController : BaseApiController
{
    private readonly IPOSService _posService;
    private readonly ILogger<SalesController> _logger;

    public SalesController(IPOSService posService, ILogger<SalesController> logger)
    {
        _posService = posService;
        _logger = logger;
    }

    /// <summary>
    /// Get all sales for current shop
    /// </summary>
    /// <param name="startDate">Start date filter</param>
    /// <param name="endDate">End date filter</param>
    /// <returns>List of sales</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSales([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var sales = await _posService.GetSalesByShopAsync(shopId);
            return SuccessResponse(sales);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales");
            return ErrorResponse("Error retrieving sales", 500);
        }
    }

    /// <summary>
    /// Get sale by ID
    /// </summary>
    /// <param name="id">Sale ID</param>
    /// <returns>Sale details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSale(int id)
    {
        try
        {
            var sale = await _posService.GetSaleByIdAsync(id);

            if (sale == null)
                return NotFoundResponse("Sale not found");

            return SuccessResponse(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sale {SaleId}", id);
            return ErrorResponse("Error retrieving sale", 500);
        }
    }

    /// <summary>
    /// Create new sale (checkout)
    /// </summary>
    /// <param name="request">Sale details</param>
    /// <returns>Created sale with invoice</returns>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSale([FromBody] CreateSaleRequest request)
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
            var sale = await _posService.CreateSaleAsync(request);

            _logger.LogInformation("Sale created: {SaleId} by user {UserId}", sale.Id, userId);

            return StatusCode(201, new
            {
                success = true,
                message = "Sale completed successfully",
                data = sale,
                invoiceNumber = sale.InvoiceNumber,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sale");
            return ErrorResponse(ex.Message, 500);
        }
    }

    /// <summary>
    /// Get sales statistics
    /// </summary>
    /// <param name="startDate">Start date</param>
    /// <param name="endDate">End date</param>
    /// <returns>Sales statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var sales = await _posService.GetSalesByShopAsync(shopId);

            var statistics = new
            {
                totalSales = sales.Count(),
                totalRevenue = sales.Sum(s => s.TotalAmount),
                totalTax = sales.Sum(s => s.TaxAmount),
                totalDiscount = sales.Sum(s => s.DiscountAmount),
                averageSaleAmount = sales.Any() ? sales.Average(s => s.TotalAmount) : 0,
                cashSales = sales.Count(s => s.PaymentMethod == "Cash"),
                cardSales = sales.Count(s => s.PaymentMethod == "Card"),
                paidSales = sales.Count(s => s.PaymentStatus == "Paid"),
                partialSales = sales.Count(s => s.PaymentStatus == "Partial"),
                unpaidSales = sales.Count(s => s.PaymentStatus == "Unpaid")
            };

            return SuccessResponse(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales statistics");
            return ErrorResponse("Error retrieving statistics", 500);
        }
    }

    /// <summary>
    /// Get today's sales summary
    /// </summary>
    /// <returns>Today's sales summary</returns>
    [HttpGet("today")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodaysSales()
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var sales = await _posService.GetSalesByShopAsync(shopId);
            sales = sales.Where(s => s.SaleDate >= today && s.SaleDate < tomorrow);

            var summary = new
            {
                date = today,
                totalSales = sales.Count(),
                totalRevenue = sales.Sum(s => s.TotalAmount),
                sales = sales.OrderByDescending(s => s.SaleDate)
            };

            return SuccessResponse(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's sales");
            return ErrorResponse("Error retrieving today's sales", 500);
        }
    }

    /// <summary>
    /// Get available products for POS
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="categoryId">Category filter</param>
    /// <param name="brandId">Brand filter</param>
    /// <returns>List of available products</returns>
    [HttpGet("products")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPOSProducts(
        [FromQuery] string? searchTerm,
        [FromQuery] int? categoryId,
        [FromQuery] int? brandId)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var products = await _posService.GetAvailableProductsAsync(shopId);
            return SuccessResponse(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving POS products");
            return ErrorResponse("Error retrieving products", 500);
        }
    }

    /// <summary>
    /// Get comprehensive sales report data
    /// </summary>
    [HttpGet("sales-report-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSalesReportData(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var from = (startDate ?? DateTime.UtcNow.AddMonths(-1)).Date;
            var to   = (endDate   ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);
            var report = await _posService.GetSalesReportAsync(shopId, from, to);
            return SuccessResponse(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales report");
            return ErrorResponse("Error generating sales report", 500);
        }
    }

    /// <summary>
    /// Get profit report data
    /// </summary>
    [HttpGet("profit-report")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfitReport(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var from = (startDate ?? DateTime.UtcNow.AddMonths(-1)).Date;
            var to   = (endDate   ?? DateTime.UtcNow).Date.AddDays(1).AddTicks(-1);
            var report = await _posService.GetProfitReportAsync(shopId, from, to);
            return SuccessResponse(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating profit report");
            return ErrorResponse("Error generating profit report", 500);
        }
    }

    /// <summary>
    /// Get sale summary (counts and amounts by status)
    /// </summary>
    [HttpGet("sale-summary")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaleSummary()
    {
        try
        {
            var shopId = GetCurrentShopId();
            if (shopId == 0)
                return UnauthorizedResponse("No shop selected");

            var summary = await _posService.GetSaleSummaryAsync(shopId);
            return SuccessResponse(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale summary");
            return ErrorResponse("Error getting sale summary", 500);
        }
    }
}
