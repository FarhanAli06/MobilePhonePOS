using Empire.Application.Interfaces;
using Empire.Application.DTOs.Sale;
using Empire.Application.DTOs.Inventory;
using Empire.Infrastructure.Data;
using Empire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Empire.Infrastructure.Services;

public class POSService : IPOSService
{
    private readonly EmpireDbContext _context;
    private readonly ILogger<POSService> _logger;
    private readonly IRepairService _repairService;

    public POSService(
        EmpireDbContext context,
        ILogger<POSService> logger,
        IRepairService repairService)
    {
        _context = context;
        _logger = logger;
        _repairService = repairService;
    }

    public async Task<IEnumerable<SaleDto>> GetSalesByShopAsync(int shopId)
    {
        try
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Where(s => s.ShopId == shopId && !s.IsDeleted)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return sales.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<SaleDto?> GetSaleByIdAsync(int id)
    {
        try
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            return sale == null ? null : MapToDto(sale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<SaleDto>> GetByShopAsync(int shopId)
        => await GetSalesByShopAsync(shopId);

    public async Task<IEnumerable<InventoryDto>> GetAvailableProductsAsync(int shopId)
    {
        try
        {
            var items = await _context.InventoryItems
                .Include(i => i.Item)
                .Where(i => i.ShopId == shopId && i.CurrentStock > 0 && !i.IsDeleted)
                .ToListAsync();

            return items.Select(i => new InventoryDto
            {
                Id         = i.Id,
                ShopId     = i.ShopId,
                Name       = i.Name,
                Stock      = i.CurrentStock,
                RetailPrice = i.RetailPrice,
                CostPrice  = i.CostPrice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available products for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<SaleDto> CreateSaleAsync(CreateSaleRequest request)
    {
        // Wrap in execution strategy to support SQL Server retry policy
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // ── 1. Generate a unique sale number ──────────────────────────────
                var saleNumber = await GenerateSaleNumberAsync(request.ShopId);

                // ── 2. Determine primary payment method for the legacy column ──────
                var primaryMethod = request.Payments.FirstOrDefault()?.PaymentMethod ?? "Cash";

                // ── 3. Determine payment status ───────────────────────────────────
                var paidAmount  = request.Payments.Sum(p => p.Amount);
                var payStatus   = !string.IsNullOrWhiteSpace(request.PaymentStatus)
                                    ? request.PaymentStatus
                                    : (paidAmount >= request.TotalAmount ? "Paid"
                                       : paidAmount > 0 ? "Partial"
                                       : "Unpaid");

                // ── 4. Create Sale header ─────────────────────────────────────────
                var sale = new Sale
                {
                    ShopId           = request.ShopId,
                    CustomerId       = request.CustomerId,
                    SaleNumber       = saleNumber,
                    SubTotal         = request.SubTotal > 0 ? request.SubTotal : request.TotalAmount,
                    TaxAmount        = request.TaxAmount,
                    DiscountAmount   = request.DiscountAmount,
                    TotalAmount      = request.TotalAmount,
                    PaymentMethod    = primaryMethod,
                    PaymentStatus    = payStatus,
                    Notes            = request.Notes,
                    SaleDate         = DateTime.UtcNow,
                    CreatedDate      = DateTime.UtcNow,
                    CreatedByUserId  = request.CreatedByUserId > 0 ? request.CreatedByUserId : 1,
                    IsDeleted        = false
                };

                _context.Sales.Add(sale);
                await _context.SaveChangesAsync(); // get sale.Id

                // ── 5. Create SaleItems ───────────────────────────────────────────
                foreach (var item in request.Items)
                {
                    // ── Resolve cost price from DB when not provided ─────────────
                    decimal resolvedCost = item.Cost ?? 0m;
                    if (resolvedCost == 0m)
                    {
                        var invId = item.InventoryItemId ?? (item.ItemType == "Inventory" ? item.ItemReferenceId : null);
                        if (invId.HasValue)
                        {
                            var invItem = await _context.InventoryItems.FindAsync(invId.Value);
                            if (invItem != null) resolvedCost = invItem.CostPrice;
                        }
                        else if (item.ItemType == "Device" && item.ItemReferenceId.HasValue)
                        {
                            var device = await _context.Devices.FindAsync(item.ItemReferenceId.Value);
                            if (device != null) resolvedCost = device.BuyingPrice ?? 0m;
                        }
                        else if (item.ItemType == "Repair" && item.RepairId.HasValue)
                        {
                            // Cost = sum of repair parts cost (use join, not Include+SumAsync which is unsupported in EF Core)
                            var repairPartsCost = await (
                                from rp in _context.RepairParts
                                join inv in _context.InventoryItems on rp.InventoryItemId equals inv.Id
                                where rp.RepairId == item.RepairId.Value
                                select inv.CostPrice * rp.Quantity
                            ).SumAsync();
                            resolvedCost = repairPartsCost;
                        }
                    }

                    var saleItem = new SaleItem
                    {
                        SaleId          = sale.Id,
                        InventoryItemId = item.InventoryItemId ?? (item.ItemType == "Inventory" ? item.ItemReferenceId : null),
                        RepairId        = item.RepairId,
                        ItemName        = item.ItemName,
                        Description     = item.Description ?? string.Empty,
                        Quantity        = item.Quantity,
                        UnitPrice       = item.UnitPrice,
                        TotalPrice      = item.TotalPrice > 0 ? item.TotalPrice : item.UnitPrice * item.Quantity,
                        DiscountAmount  = item.DiscountAmount,
                        CostPrice       = resolvedCost,
                        Note            = item.Note,
                        IsCustomItem    = item.IsCustomItem,
                        IsTaxable       = item.IsTaxable,
                        CreatedDate     = DateTime.UtcNow,
                        IsDeleted       = false
                    };
                    _context.SaleItems.Add(saleItem);

                    // Deduct stock for inventory items
                    if (saleItem.InventoryItemId.HasValue)
                    {
                        var inv = await _context.InventoryItems.FindAsync(saleItem.InventoryItemId.Value);
                        if (inv != null)
                        {
                            inv.CurrentStock = Math.Max(0, inv.CurrentStock - item.Quantity);
                            inv.ModifiedDate = DateTime.UtcNow;

                            _context.StockMovements.Add(new StockMovement
                            {
                                InventoryItemId  = inv.Id,
                                CreatedByUserId  = request.CreatedByUserId > 0 ? request.CreatedByUserId : 1,
                                MovementType     = "OUT",
                                Quantity         = item.Quantity,
                                PreviousStock    = inv.CurrentStock + item.Quantity,
                                NewStock         = inv.CurrentStock,
                                Reason           = $"POS Sale {saleNumber}",
                                ReferenceNumber  = saleNumber,
                                MovementDate     = DateTime.UtcNow,
                                CreatedDate      = DateTime.UtcNow,
                                IsDeleted        = false
                            });
                        }
                    }

                    // Mark device as sold
                    if (item.ItemType == "Device" && item.ItemReferenceId.HasValue)
                    {
                        var soldDevice = await _context.Devices.FindAsync(item.ItemReferenceId.Value);
                        if (soldDevice != null)
                        {
                            soldDevice.IsSold             = true;
                            soldDevice.SoldDate           = DateTime.UtcNow;
                            soldDevice.SoldToCustomerId   = request.CustomerId > 0 ? request.CustomerId : (int?)null;
                            soldDevice.ModifiedDate       = DateTime.UtcNow;
                        }
                    }
                }

                // ── 6. Create Payment records ─────────────────────────────────────
                foreach (var payment in request.Payments)
                {
                    _context.Payments.Add(new Payment
                    {
                        SaleId        = sale.Id,
                        Amount        = payment.Amount,
                        PaymentMethod = payment.PaymentMethod,
                        TransactionId = payment.TransactionId,
                        UserId        = request.CreatedByUserId > 0 ? request.CreatedByUserId : 1,
                        CreatedDate   = DateTime.UtcNow,
                        IsDeleted     = false
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Sale {SaleNumber} created successfully (Id={SaleId})", saleNumber, sale.Id);

                // ── 7. Update repair payment status for any repair items sold ─────
                // Must be done OUTSIDE the transaction (after commit) to avoid nested tx issues
                var repairIds = request.Items
                    .Where(i => i.RepairId.HasValue)
                    .Select(i => i.RepairId!.Value)
                    .Distinct()
                    .ToList();

                foreach (var rid in repairIds)
                {
                    try
                    {
                        // Add a payment entry to the repair ledger for this sale
                        var paidForRepair = request.Payments.Sum(p => p.Amount);
                        var repairItem    = request.Items.FirstOrDefault(i => i.RepairId == rid);
                        if (repairItem != null)
                        {
                            var addPayReq = new Empire.Application.DTOs.Repair.AddRepairPaymentRequest
                            {
                                RepairId      = rid,
                                Amount        = repairItem.TotalPrice > 0 ? repairItem.TotalPrice : repairItem.UnitPrice * repairItem.Quantity,
                                PaymentMethod = request.Payments.FirstOrDefault()?.PaymentMethod ?? "Cash",
                                Type          = "Payment",
                                Reference     = saleNumber,
                                Notes         = $"POS Sale {saleNumber}",
                                PaidAt        = DateTime.UtcNow,
                                UserId        = request.CreatedByUserId > 0 ? request.CreatedByUserId : (int?)null
                            };
                            await _repairService.AddRepairPaymentAsync(addPayReq);
                        }
                        else
                        {
                            await _repairService.RecalculateRepairPaymentStatusAsync(rid);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update repair {RepairId} payment status after sale {SaleNumber}", rid, saleNumber);
                    }
                }

                return MapToDto(sale);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                _logger.LogError(ex, "Error creating sale");
                throw;
            }
        });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<string> GenerateSaleNumberAsync(int shopId)
    {
        var today  = DateTime.UtcNow;
        var prefix = $"SAL-{shopId:D2}-{today:yyyyMMdd}-";
        var count  = await _context.Sales
            .CountAsync(s => s.ShopId == shopId && s.SaleDate.Date == today.Date);
        return $"{prefix}{(count + 1):D4}";
    }

    private static SaleDto MapToDto(Sale s) => new()
    {
        Id             = s.Id,
        SaleNumber     = s.SaleNumber ?? "",
        InvoiceNumber  = s.SaleNumber ?? "",
        CustomerName   = s.Customer != null
                            ? $"{s.Customer.FirstName} {s.Customer.LastName}".Trim()
                            : "",
        SubTotal       = s.SubTotal,
        TaxAmount      = s.TaxAmount,
        DiscountAmount = s.DiscountAmount,
        TotalAmount    = s.TotalAmount,
        PaymentMethod  = s.PaymentMethod ?? "",
        PaymentStatus  = s.PaymentStatus ?? "",
        SaleDate       = s.SaleDate,
        ItemCount      = s.SaleItems?.Count ?? 0,
        Items          = s.SaleItems?.Select(i => new SaleItemDto
        {
            Id              = i.Id,
            ItemName        = i.ItemName,
            Description     = i.Description,
            Quantity        = i.Quantity,
            UnitPrice       = i.UnitPrice,
            TotalPrice      = i.TotalPrice,
            CostPrice       = i.CostPrice,
            IsCustomItem    = i.IsCustomItem,
            InventoryItemId = i.InventoryItemId,
            RepairId        = i.RepairId
        }).ToList() ?? new(),
        Payments       = s.Payments?.Select(p => new SalePaymentDto
        {
            Id            = p.Id,
            PaymentMethod = p.PaymentMethod,
            Amount        = p.Amount,
            TransactionId = p.TransactionId
        }).ToList() ?? new()
    };

    // ── Report helpers ────────────────────────────────────────────────────────

    public async Task<SalesReportData?> GetSalesReportAsync(int shopId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Include(s => s.Payments)
                .Where(s => s.ShopId == shopId && !s.IsDeleted
                         && s.SaleDate >= startDate && s.SaleDate <= endDate)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var totalPaid    = sales.Where(s => s.PaymentStatus == "Paid").Sum(s => s.TotalAmount);
            var totalPartial = sales.Where(s => s.PaymentStatus == "Partial").Sum(s => s.TotalAmount);
            var totalUnpaid  = sales.Where(s => s.PaymentStatus == "Unpaid").Sum(s => s.TotalAmount);

            return new SalesReportData
            {
                Summary = new SalesReportSummary
                {
                    TotalRevenue = totalRevenue,
                    TotalSales   = sales.Count,
                    TotalPaid    = totalPaid,
                    TotalPartial = totalPartial,
                    TotalUnpaid  = totalUnpaid,
                    AverageSale  = sales.Count > 0 ? totalRevenue / sales.Count : 0
                },
                SalesByDate = sales
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new SalesByDateItem { Date = g.Key, Count = g.Count(), Revenue = g.Sum(s => s.TotalAmount) })
                    .OrderBy(x => x.Date).ToList(),
                SalesByPaymentMethod = sales
                    .SelectMany(s => s.Payments ?? new List<Payment>())
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new SalesByMethodItem { Method = g.Key, Amount = g.Sum(p => p.Amount), Count = g.Count() })
                    .ToList(),
                SalesByStatus = sales
                    .GroupBy(s => s.PaymentStatus ?? "Unknown")
                    .Select(g => new SalesByStatusItem { Status = g.Key, Count = g.Count(), Amount = g.Sum(s => s.TotalAmount) })
                    .ToList(),
                TopItems = sales
                    .SelectMany(s => s.SaleItems ?? new List<SaleItem>())
                    .GroupBy(i => i.ItemName)
                    .Select(g => new TopSaleItem { Name = g.Key, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice) })
                    .OrderByDescending(x => x.Revenue).Take(10).ToList(),
                Sales = sales.Select(MapToDto).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating sales report for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<ProfitReportData?> GetProfitReportAsync(int shopId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var items = await _context.SaleItems
                .Include(i => i.Sale).ThenInclude(s => s.Customer)
                .Include(i => i.InventoryItem)
                .Where(i => !i.IsDeleted && i.Sale.ShopId == shopId
                         && i.Sale.SaleDate >= startDate && i.Sale.SaleDate <= endDate
                         && !i.Sale.IsDeleted)
                .ToListAsync();

            // Pre-load repair parts costs for all repair items where CostPrice was not recorded
            var repairIds = items.Where(i => i.RepairId.HasValue && i.CostPrice == 0)
                                 .Select(i => i.RepairId!.Value)
                                 .Distinct().ToList();
            var repairPartsCosts = new Dictionary<int, decimal>();
            if (repairIds.Any())
            {
                var partsCosts = await (
                    from rp in _context.RepairParts
                    join inv in _context.InventoryItems on rp.InventoryItemId equals inv.Id
                    where repairIds.Contains(rp.RepairId)
                    group new { inv.CostPrice, rp.Quantity } by rp.RepairId into g
                    select new { RepairId = g.Key, TotalCost = g.Sum(x => x.CostPrice * x.Quantity) }
                ).ToListAsync();
                repairPartsCosts = partsCosts.ToDictionary(x => x.RepairId, x => x.TotalCost);
            }

            var rows = new List<ProfitReportRow>();
            foreach (var i in items)
            {
                decimal effectiveCost = i.CostPrice;
                if (effectiveCost == 0)
                {
                    if (i.RepairId.HasValue && repairPartsCosts.TryGetValue(i.RepairId.Value, out var rc))
                        effectiveCost = rc;
                    else if (i.InventoryItem != null)
                        effectiveCost = i.InventoryItem.CostPrice;
                }

                rows.Add(new ProfitReportRow
                {
                    SaleDate      = i.Sale.SaleDate,
                    InvoiceNumber = i.Sale.SaleNumber ?? "",
                    Customer      = i.Sale.Customer != null
                                        ? $"{i.Sale.Customer.FirstName} {i.Sale.Customer.LastName}".Trim()
                                        : "",
                    ItemName      = i.ItemName,
                    Quantity      = i.Quantity,
                    Revenue       = i.TotalPrice,
                    Cost          = effectiveCost * i.Quantity,
                    Profit        = i.TotalPrice - (effectiveCost * i.Quantity),
                    Margin        = i.TotalPrice > 0
                                        ? Math.Round((i.TotalPrice - effectiveCost * i.Quantity) / i.TotalPrice * 100, 2)
                                        : 0,
                    PaymentStatus = i.Sale.PaymentStatus ?? ""
                });
            }

            return new ProfitReportData
            {
                Items = rows,
                Summary = new ProfitReportSummary
                {
                    TotalRevenue  = rows.Sum(r => r.Revenue),
                    TotalCost     = rows.Sum(r => r.Cost),
                    TotalProfit   = rows.Sum(r => r.Profit),
                    ProfitMargin  = rows.Sum(r => r.Revenue) > 0
                                        ? Math.Round(rows.Sum(r => r.Profit) / rows.Sum(r => r.Revenue) * 100, 2)
                                        : 0,
                    SalesCount    = rows.Select(r => r.InvoiceNumber).Distinct().Count(),
                    ItemsSold     = rows.Sum(r => r.Quantity)
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating profit report for shop {ShopId}", shopId);
            throw;
        }
    }

    public async Task<SalePaymentDto?> AddSalePaymentAsync(int saleId, string paymentMethod, decimal amount, string? transactionId, int userId)
    {
        try
        {
            var sale = await _context.Sales
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId && !s.IsDeleted);
            if (sale == null) return null;

            var payment = new Payment
            {
                SaleId        = saleId,
                Amount        = amount,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId,
                UserId        = userId > 0 ? userId : 1,
                CreatedDate   = DateTime.UtcNow,
                IsDeleted     = false
            };
            _context.Payments.Add(payment);

            // Recalculate payment status
            var totalPaid = sale.Payments.Where(p => !p.IsDeleted).Sum(p => p.Amount) + amount;
            sale.PaymentStatus = totalPaid >= sale.TotalAmount - 0.005m ? "Paid"
                               : totalPaid > 0 ? "Partial"
                               : "Unpaid";
            sale.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Payment {Amount} added to sale {SaleId}", amount, saleId);

            return new SalePaymentDto
            {
                Id            = payment.Id,
                PaymentMethod = payment.PaymentMethod,
                Amount        = payment.Amount,
                TransactionId = payment.TransactionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding payment to sale {SaleId}", saleId);
            throw;
        }
    }

    public async Task<bool> UpdateSalePaymentAsync(int paymentId, string paymentMethod, decimal amount, string? transactionId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Sale)
                .FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted);
            if (payment == null) return false;

            payment.PaymentMethod = paymentMethod;
            payment.Amount        = amount;
            payment.TransactionId = transactionId;
            payment.ModifiedDate  = DateTime.UtcNow;

            // Recalculate payment status on the sale
            var sale = payment.Sale;
            if (sale != null)
            {
                var allPayments = await _context.Payments
                    .Where(p => p.SaleId == sale.Id && !p.IsDeleted)
                    .ToListAsync();
                var totalPaid = allPayments.Sum(p => p.Id == paymentId ? amount : p.Amount);
                sale.PaymentStatus = totalPaid >= sale.TotalAmount - 0.005m ? "Paid"
                                   : totalPaid > 0 ? "Partial"
                                   : "Unpaid";
                sale.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<bool> DeleteSalePaymentAsync(int paymentId)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Sale)
                .FirstOrDefaultAsync(p => p.Id == paymentId && !p.IsDeleted);
            if (payment == null) return false;

            payment.IsDeleted    = true;
            payment.ModifiedDate = DateTime.UtcNow;

            // Recalculate payment status on the sale
            var sale = payment.Sale;
            if (sale != null)
            {
                var remaining = await _context.Payments
                    .Where(p => p.SaleId == sale.Id && !p.IsDeleted && p.Id != paymentId)
                    .SumAsync(p => p.Amount);
                sale.PaymentStatus = remaining >= sale.TotalAmount - 0.005m ? "Paid"
                                   : remaining > 0 ? "Partial"
                                   : "Unpaid";
                sale.ModifiedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<SaleSummaryData?> GetSaleSummaryAsync(int shopId)
    {
        try
        {
            var sales = await _context.Sales
                .Where(s => s.ShopId == shopId && !s.IsDeleted)
                .ToListAsync();

            return new SaleSummaryData
            {
                TotalSales     = sales.Count,
                TotalRevenue   = sales.Sum(s => s.TotalAmount),
                PaidSales      = sales.Count(s => s.PaymentStatus == "Paid"),
                PartialSales   = sales.Count(s => s.PaymentStatus == "Partial"),
                UnpaidSales    = sales.Count(s => s.PaymentStatus == "Unpaid"),
                PaidAmount     = sales.Where(s => s.PaymentStatus == "Paid").Sum(s => s.TotalAmount),
                PartialAmount  = sales.Where(s => s.PaymentStatus == "Partial").Sum(s => s.TotalAmount),
                UnpaidAmount   = sales.Where(s => s.PaymentStatus == "Unpaid").Sum(s => s.TotalAmount)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sale summary for shop {ShopId}", shopId);
            throw;
        }
    }
}
