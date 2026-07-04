using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Empire.Application.Interfaces;
using Empire.Application.DTOs.Sale;
using Empire.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Empire.API.Controllers;

/// <summary>
/// Sales and POS endpoints
/// </summary>
[Authorize]
public class SalesController : BaseApiController
{
    private readonly IPOSService _posService;
    private readonly ILogger<SalesController> _logger;
    private readonly EmpireDbContext _context;

    public SalesController(IPOSService posService, ILogger<SalesController> logger, EmpireDbContext context)
    {
        _posService = posService;
        _logger = logger;
        _context = context;
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

    // ── Payment management ────────────────────────────────────────────────────

    /// <summary>Get full sale detail including items and payments</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaleDetail(int id)
    {
        try
        {
            // Load sale with Shop and Customer navigation properties
            var saleEntity = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.Shop)
                .Include(s => s.SaleItems)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (saleEntity == null) return NotFoundResponse("Sale not found");

            var totalPaid = saleEntity.Payments?.Where(p => !p.IsDeleted).Sum(p => p.Amount) ?? 0m;
            var result = new
            {
                id            = saleEntity.Id,
                invoiceNumber = saleEntity.SaleNumber,
                saleDate      = saleEntity.SaleDate,
                paymentStatus = saleEntity.PaymentStatus,
                subtotal      = saleEntity.SubTotal,
                tax           = saleEntity.TaxAmount,
                discount      = saleEntity.DiscountAmount,
                total         = saleEntity.TotalAmount,
                totalPaid     = totalPaid,
                balance       = Math.Max(0m, saleEntity.TotalAmount - totalPaid),
                shop = saleEntity.Shop == null ? null : new
                {
                    name    = saleEntity.Shop.Name,
                    address = saleEntity.Shop.Address ?? "",
                    city    = saleEntity.Shop.City ?? "",
                    state   = saleEntity.Shop.State ?? "",
                    zip     = saleEntity.Shop.ZipCode ?? "",
                    phone   = saleEntity.Shop.Phone ?? "",
                    email   = saleEntity.Shop.Email ?? "",
                    logo    = saleEntity.Shop.LogoPath ?? ""
                },
                customer = saleEntity.Customer == null ? new { name = "Walk-in", phone = "", email = "", address = "" }
                    : new
                    {
                        name    = (saleEntity.Customer.FirstName + " " + (saleEntity.Customer.LastName ?? "")).Trim(),
                        phone   = saleEntity.Customer.Phone ?? "",
                        email   = saleEntity.Customer.Email ?? "",
                        address = (saleEntity.Customer.Address ?? "") + (saleEntity.Customer.City != null ? ", " + saleEntity.Customer.City : "")
                    },
                items = saleEntity.SaleItems.Where(i => !i.IsDeleted).Select(i => new {
                    id        = i.Id,
                    name      = i.ItemName,
                    description = i.Description ?? "",
                    qty       = i.Quantity,
                    unitPrice = i.UnitPrice,
                    discount  = i.DiscountAmount,
                    total     = i.TotalPrice
                }),
                payments = saleEntity.Payments?.Where(p => !p.IsDeleted).Select(p => new {
                    id            = p.Id,
                    method        = p.PaymentMethod,
                    amount        = p.Amount,
                    transactionId = p.TransactionId,
                    date          = p.CreatedDate
                })
            };
            return SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale detail {Id}", id);
            return ErrorResponse("Error getting sale detail", 500);
        }
    }

    /// <summary>Add a payment to an existing sale</summary>
    [HttpPost("{id:int}/payments")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddSalePayment(int id, [FromBody] AddSalePaymentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var payment = await _posService.AddSalePaymentAsync(id, request.PaymentMethod, request.Amount, request.TransactionId, userId);
            if (payment == null) return NotFoundResponse("Sale not found");
            return SuccessResponse(new { id = payment.Id, method = payment.PaymentMethod, amount = payment.Amount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to sale {Id}", id);
            return ErrorResponse("Error adding payment", 500);
        }
    }

    /// <summary>Update an existing sale payment</summary>
    [HttpPut("payments/{paymentId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSalePayment(int paymentId, [FromBody] AddSalePaymentRequest request)
    {
        try
        {
            var ok = await _posService.UpdateSalePaymentAsync(paymentId, request.PaymentMethod, request.Amount, request.TransactionId);
            return ok ? SuccessResponse(new { updated = true }) : NotFoundResponse("Payment not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId}", paymentId);
            return ErrorResponse("Error updating payment", 500);
        }
    }

    /// <summary>Delete a sale payment</summary>
    [HttpDelete("payments/{paymentId:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSalePayment(int paymentId)
    {
        try
        {
            var ok = await _posService.DeleteSalePaymentAsync(paymentId);
            return ok ? SuccessResponse(new { deleted = true }) : NotFoundResponse("Payment not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment {PaymentId}", paymentId);
            return ErrorResponse("Error deleting payment", 500);
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
