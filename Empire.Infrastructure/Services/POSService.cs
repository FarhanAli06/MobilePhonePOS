using Empire.Application.Interfaces;
using Empire.Application.DTOs.Sale;
using Empire.Application.DTOs.Inventory;
using Empire.Infrastructure.Data;
using Empire.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
            await _repairService.SyncDirectRepairPaymentSalesAsync(shopId);
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Include(s => s.Payments)
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
                    Device? soldDevice = null;
                    var resolvedCost = item.Cost ?? 0m;

                    if (string.Equals(item.ItemType, "Device", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!item.ItemReferenceId.HasValue || item.ItemReferenceId.Value <= 0)
                            throw new InvalidOperationException("The device sale is missing its device record. Remove it from the cart and add it again.");

                        soldDevice = await _context.Devices.FirstOrDefaultAsync(d =>
                            d.Id == item.ItemReferenceId.Value &&
                            d.ShopId == request.ShopId &&
                            !d.IsDeleted);

                        if (soldDevice == null)
                            throw new InvalidOperationException("The selected device could not be found.");
                        if (soldDevice.IsSold || !soldDevice.IsAvailableForSale)
                            throw new InvalidOperationException("The selected device is no longer available for sale.");

                        // The authoritative cost comes from the device record, not
                        // from editable browser data.
                        resolvedCost = soldDevice.BuyingPrice ?? 0m;
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
                        ItemStatus      = string.IsNullOrWhiteSpace(item.ItemStatus) ? "In Stock" : item.ItemStatus.Trim(),
                        IsCustomItem    = item.IsCustomItem,
                        IsTaxable       = item.IsTaxable,
                        CreatedDate     = DateTime.UtcNow,
                        IsDeleted       = false
                    };
                    _context.SaleItems.Add(saleItem);

                    if (soldDevice != null)
                    {
                        soldDevice.IsSold = true;
                        soldDevice.IsAvailableForSale = false;
                        soldDevice.LifecycleStatus = "Sold";
                        soldDevice.SoldDate = DateTime.UtcNow;
                        soldDevice.SoldToCustomerId = request.CustomerId;
                        soldDevice.ModifiedDate = DateTime.UtcNow;
                        _context.DeviceActivities.Add(new DeviceActivity { DeviceId = soldDevice.Id, Action = "Sold", Description = $"Sold on {saleNumber}", NewValues = JsonSerializer.Serialize(new { soldDevice.IsSold, soldDevice.IsAvailableForSale, soldDevice.LifecycleStatus, soldDevice.SoldToCustomerId }), PerformedByUserId = request.CreatedByUserId > 0 ? request.CreatedByUserId : 1, PerformedAt = DateTime.UtcNow });
                    }

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
                        Type          = "Payment",
                        CreatedDate   = DateTime.UtcNow,
                        IsDeleted     = false
                    });
                }

                await _context.SaveChangesAsync();
                _context.SaleActivities.Add(new SaleActivity { SaleId = sale.Id, Action = "Created", Description = "Sale completed", NewValues = SaleSnapshot(sale), PerformedByUserId = request.CreatedByUserId > 0 ? request.CreatedByUserId : 1, PerformedAt = DateTime.UtcNow });
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

                var unallocatedPayment = Math.Max(0, request.Payments.Sum(payment => payment.Amount));
                foreach (var rid in repairIds)
                {
                    try
                    {
                        // Allocate only money actually collected. This prevents a
                        // partial POS payment from marking the full repair paid.
                        var repairItem    = request.Items.FirstOrDefault(i => i.RepairId == rid);
                        if (repairItem != null)
                        {
                            var repairCharge = repairItem.TotalPrice > 0
                                ? repairItem.TotalPrice
                                : repairItem.UnitPrice * repairItem.Quantity;
                            var paidForRepair = Math.Min(Math.Max(0, repairCharge), unallocatedPayment);
                            if (paidForRepair <= 0)
                            {
                                await _repairService.RecalculateRepairPaymentStatusAsync(rid);
                                continue;
                            }
                            var addPayReq = new Empire.Application.DTOs.Repair.AddRepairPaymentRequest
                            {
                                RepairId      = rid,
                                Amount        = paidForRepair,
                                PaymentMethod = request.Payments.FirstOrDefault()?.PaymentMethod ?? "Cash",
                                Type          = "Payment",
                                Reference     = saleNumber,
                                Notes         = $"POS Sale {saleNumber}",
                                PaidAt        = DateTime.UtcNow,
                                UserId        = request.CreatedByUserId > 0 ? request.CreatedByUserId : (int?)null
                            };
                            await _repairService.AddRepairPaymentAsync(addPayReq);
                            unallocatedPayment -= paidForRepair;
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

    public async Task<SaleDto> UpdateSaleAsync(int saleId, CreateSaleRequest request)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var sale = await _context.Sales
                    .Include(s => s.Customer)
                    .Include(s => s.SaleItems)
                    .Include(s => s.Payments)
                    .FirstOrDefaultAsync(s => s.Id == saleId && s.ShopId == request.ShopId && !s.IsDeleted);

                if (sale == null)
                    throw new KeyNotFoundException("Sale not found.");

                var oldSnapshot = SaleSnapshot(sale);
                var directRepairId = GetDirectRepairId(sale.Notes);

                sale.CustomerId = request.CustomerId;
                sale.SubTotal = request.SubTotal;
                sale.TaxAmount = request.TaxAmount;
                sale.DiscountAmount = request.DiscountAmount;
                sale.TotalAmount = request.TotalAmount;
                sale.PaymentStatus = string.IsNullOrWhiteSpace(request.PaymentStatus)
                    ? sale.PaymentStatus
                    : request.PaymentStatus;
                sale.PaymentMethod = request.Payments.FirstOrDefault()?.PaymentMethod ?? sale.PaymentMethod;
                // Preserve the internal link used to keep a repair-page payment
                // synchronized with this editable sale record.
                if (!directRepairId.HasValue)
                    sale.Notes = request.Notes ?? sale.Notes;
                sale.ModifiedDate = DateTime.UtcNow;
                sale.ModifiedDateUtc = DateTime.UtcNow;

                var existingItems = sale.SaleItems
                    .Where(i => !i.IsDeleted)
                    .OrderBy(i => i.Id)
                    .ToList();

                for (var index = 0; index < request.Items.Count; index++)
                {
                    var source = request.Items[index];
                    var target = index < existingItems.Count
                        ? existingItems[index]
                        : new SaleItem
                        {
                            SaleId = sale.Id,
                            CreatedDate = DateTime.UtcNow,
                            IsDeleted = false
                        };

                    if (index >= existingItems.Count)
                        _context.SaleItems.Add(target);

                    target.InventoryItemId = source.InventoryItemId ?? target.InventoryItemId;
                    target.RepairId = source.RepairId ?? target.RepairId;
                    target.ItemName = source.ItemName;
                    target.Description = source.Description ?? string.Empty;
                    target.Quantity = source.Quantity;
                    target.UnitPrice = source.UnitPrice;
                    target.TotalPrice = source.TotalPrice > 0
                        ? source.TotalPrice
                        : source.UnitPrice * source.Quantity;
                    target.DiscountAmount = source.DiscountAmount;
                    target.CostPrice = source.Cost ?? target.CostPrice;
                    target.Note = source.Note;
                    target.ItemStatus = string.IsNullOrWhiteSpace(source.ItemStatus) ? target.ItemStatus : source.ItemStatus;
                    target.IsCustomItem = source.IsCustomItem;
                    target.IsTaxable = source.IsTaxable;
                    target.ModifiedDate = DateTime.UtcNow;
                    target.ModifiedDateUtc = DateTime.UtcNow;

                    // Repair older device sales that were saved before DeviceId and
                    // buying cost were handled. The edit payload retains the IMEI in
                    // the description, allowing an exact and safe match.
                    var device = await ResolveDeviceForSaleItemAsync(source, request.ShopId);
                    if (device != null)
                    {
                        target.CostPrice = device.BuyingPrice ?? target.CostPrice;
                        device.IsSold = true;
                        device.IsAvailableForSale = false;
                        device.SoldDate ??= sale.SaleDate;
                        device.SoldToCustomerId = request.CustomerId;
                        device.ModifiedDate = DateTime.UtcNow;
                    }
                }

                foreach (var removedItem in existingItems.Skip(request.Items.Count))
                {
                    removedItem.IsDeleted = true;
                    removedItem.ModifiedDate = DateTime.UtcNow;
                    removedItem.ModifiedDateUtc = DateTime.UtcNow;
                }

                var existingPayments = sale.Payments
                    .Where(p => !p.IsDeleted && p.Type != "Refund")
                    .OrderBy(p => p.Id)
                    .ToList();

                for (var index = 0; index < request.Payments.Count; index++)
                {
                    var source = request.Payments[index];
                    var target = index < existingPayments.Count
                        ? existingPayments[index]
                        : new Payment
                        {
                            SaleId = sale.Id,
                            CreatedDate = DateTime.UtcNow,
                            UserId = request.CreatedByUserId > 0 ? request.CreatedByUserId : 1,
                            IsDeleted = false
                        };

                    if (index >= existingPayments.Count)
                        _context.Payments.Add(target);

                    target.Amount = source.Amount;
                    target.PaymentMethod = source.PaymentMethod;
                    target.TransactionId = source.TransactionId;
                    target.Type = string.IsNullOrWhiteSpace(source.Type) ? "Payment" : source.Type;
                    target.ModifiedDate = DateTime.UtcNow;
                    target.ModifiedDateUtc = DateTime.UtcNow;
                }

                foreach (var removedPayment in existingPayments.Skip(request.Payments.Count))
                {
                    removedPayment.IsDeleted = true;
                    removedPayment.ModifiedDate = DateTime.UtcNow;
                    removedPayment.ModifiedDateUtc = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                _context.SaleActivities.Add(new SaleActivity { SaleId = sale.Id, Action = "Corrected", Description = string.IsNullOrWhiteSpace(request.ChangeReason) ? "Sale corrected" : request.ChangeReason.Trim(), OldValues = oldSnapshot, NewValues = SaleSnapshot(sale), PerformedByUserId = request.CreatedByUserId > 0 ? request.CreatedByUserId : 1, PerformedAt = DateTime.UtcNow });
                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                if (directRepairId.HasValue)
                {
                    var linkedPayments = sale.Payments
                        .Select(payment => new
                        {
                            RepairPaymentId = GetRepairPaymentId(payment.TransactionId),
                            payment.IsDeleted,
                            payment.Amount,
                            payment.PaymentMethod,
                            payment.Type
                        })
                        .Where(payment => payment.RepairPaymentId.HasValue)
                        .ToList();
                    foreach (var linked in linkedPayments)
                    {
                        if (linked.IsDeleted)
                        {
                            await _repairService.DeleteRepairPaymentAsync(linked.RepairPaymentId!.Value);
                            continue;
                        }
                        var repairPayment = await _context.RepairPayments
                            .AsNoTracking()
                            .FirstOrDefaultAsync(payment => payment.Id == linked.RepairPaymentId!.Value);
                        if (repairPayment == null) continue;
                        await _repairService.UpdateRepairPaymentAsync(repairPayment.Id,
                            new Empire.Application.DTOs.Repair.UpdateRepairPaymentRequest
                            {
                                Amount = linked.Amount,
                                PaymentMethod = linked.PaymentMethod,
                                Reference = repairPayment.Reference,
                                Notes = repairPayment.Notes,
                                PaidAt = sale.SaleDate
                            });
                    }
                }

                return MapToDto(sale);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<IEnumerable<SaleHistoryDto>> GetSaleHistoryAsync(int saleId, int shopId)
    {
        var ownsSale = await _context.Sales.AnyAsync(x => x.Id == saleId && x.ShopId == shopId && !x.IsDeleted);
        if (!ownsSale) throw new KeyNotFoundException("Sale not found.");
        return await _context.SaleActivities.AsNoTracking().Where(x => x.SaleId == saleId)
            .OrderByDescending(x => x.PerformedAt)
            .Select(x => new SaleHistoryDto { Id = x.Id, Action = x.Action, Description = x.Description, OldValues = x.OldValues, NewValues = x.NewValues, PerformedAt = x.PerformedAt, PerformedBy = x.PerformedByUser == null ? "System" : x.PerformedByUser.FirstName + " " + x.PerformedByUser.LastName })
            .ToListAsync();
    }

    public async Task<IEnumerable<SaleReturnHistoryDto>> GetSaleReturnsAsync(int saleId, int shopId)
    {
        var ownsSale = await _context.Sales.AnyAsync(x => x.Id == saleId && x.ShopId == shopId && !x.IsDeleted);
        if (!ownsSale) throw new KeyNotFoundException("Sale not found.");

        return await _context.SaleReturns.AsNoTracking()
            .Where(x => x.SaleId == saleId)
            .OrderByDescending(x => x.ProcessedAt)
            .Select(x => new SaleReturnHistoryDto
            {
                Id = x.Id,
                ReturnNumber = x.ReturnNumber,
                RefundMethod = x.RefundMethod,
                RefundAmount = x.RefundAmount,
                Reason = x.Reason,
                ProcessedAt = x.ProcessedAt,
                ProcessedBy = x.ProcessedByUser == null ? "System" : x.ProcessedByUser.FirstName + " " + x.ProcessedByUser.LastName,
                Items = x.Items.Select(item => new SaleReturnItemHistoryDto
                {
                    ItemName = item.SaleItem.ItemName,
                    Quantity = item.Quantity,
                    RefundAmount = item.TotalRefundAmount,
                    Disposition = item.Disposition
                }).ToList()
            }).ToListAsync();
    }

    public async Task<SaleReturnResultDto> ProcessReturnAsync(int saleId, ProcessSaleReturnRequest request)
    {
        if (request.Items.Count == 0 || request.Items.Any(x => x.Quantity <= 0 || x.RefundAmount < 0))
            throw new InvalidOperationException("Select at least one valid item to return.");
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("A return reason is required.");

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            var sale = await _context.Sales.Include(x => x.SaleItems).Include(x => x.Payments)
                .FirstOrDefaultAsync(x => x.Id == saleId && x.ShopId == request.ShopId && !x.IsDeleted)
                ?? throw new KeyNotFoundException("Sale not found.");
            var prior = await _context.SaleReturnItems.Where(x => x.SaleReturn.SaleId == saleId)
                .GroupBy(x => x.SaleItemId).Select(g => new { Id = g.Key, Quantity = g.Sum(x => x.Quantity) }).ToDictionaryAsync(x => x.Id, x => x.Quantity);

            var number = $"RET-{sale.Id:D6}-{await _context.SaleReturns.CountAsync(x => x.SaleId == saleId) + 1:D2}";
            var record = new SaleReturn { SaleId = saleId, ReturnNumber = number, RefundMethod = request.RefundMethod, Reason = request.Reason.Trim(), Notes = request.Notes?.Trim() ?? string.Empty, ProcessedByUserId = request.UserId > 0 ? request.UserId : 1, ProcessedAt = DateTime.UtcNow };
            _context.SaleReturns.Add(record);
            var directRepairId = GetDirectRepairId(sale.Notes);
            var repairRefunds = new Dictionary<int, decimal>();

            decimal totalRefund = 0;
            foreach (var requested in request.Items)
            {
                var item = sale.SaleItems.FirstOrDefault(x => x.Id == requested.SaleItemId && !x.IsDeleted) ?? throw new InvalidOperationException("A selected sale item was not found.");
                var alreadyReturned = prior.GetValueOrDefault(item.Id);
                if (requested.Quantity > item.Quantity - alreadyReturned)
                    throw new InvalidOperationException($"{item.ItemName} only has {item.Quantity - alreadyReturned} remaining to return.");
                var itemRefund = Math.Round(requested.RefundAmount, 2);
                totalRefund += itemRefund;
                record.Items.Add(new SaleReturnItem { SaleItemId = item.Id, Quantity = requested.Quantity, UnitRefundAmount = requested.Quantity == 0 ? 0 : itemRefund / requested.Quantity, TotalRefundAmount = itemRefund, Disposition = requested.Disposition });

                if (directRepairId.HasValue && item.RepairId == directRepairId && itemRefund > 0)
                    repairRefunds[item.RepairId.Value] = repairRefunds.GetValueOrDefault(item.RepairId.Value) + itemRefund;

                if (item.InventoryItemId.HasValue && requested.Disposition.Equals("Restock", StringComparison.OrdinalIgnoreCase))
                {
                    var inv = await _context.InventoryItems.FindAsync(item.InventoryItemId.Value);
                    if (inv != null)
                    {
                        var before = inv.CurrentStock; inv.CurrentStock += requested.Quantity; inv.ModifiedDate = DateTime.UtcNow;
                        _context.StockMovements.Add(new StockMovement { InventoryItemId = inv.Id, CreatedByUserId = record.ProcessedByUserId, MovementType = "RETURN", Quantity = requested.Quantity, PreviousStock = before, NewStock = inv.CurrentStock, Reason = $"Return {number}", ReferenceNumber = number, UnitCost = item.CostPrice, TotalCost = item.CostPrice * requested.Quantity, MovementDate = DateTime.UtcNow, CreatedDate = DateTime.UtcNow });
                    }
                }

                var device = await ResolveDeviceFromSaleItemAsync(item, sale.ShopId);
                if (device != null)
                {
                    var disposition = string.IsNullOrWhiteSpace(requested.Disposition) ? "NeedsInspection" : requested.Disposition;
                    device.IsSold = false; device.SoldDate = null; device.SoldToCustomerId = null;
                    device.LifecycleStatus = disposition;
                    device.IsAvailableForSale = disposition.Equals("Available", StringComparison.OrdinalIgnoreCase) || disposition.Equals("Restock", StringComparison.OrdinalIgnoreCase);
                    if (device.IsAvailableForSale) device.LifecycleStatus = "Available";
                    _context.DeviceActivities.Add(new DeviceActivity { DeviceId = device.Id, Action = "Returned", Description = $"Returned on {number}; disposition: {device.LifecycleStatus}", NewValues = JsonSerializer.Serialize(new { device.IsSold, device.IsAvailableForSale, device.LifecycleStatus }), PerformedByUserId = record.ProcessedByUserId, PerformedAt = DateTime.UtcNow });
                }
            }

            record.RefundAmount = totalRefund;
            if (totalRefund > 0)
                _context.Payments.Add(new Payment { SaleId = saleId, Amount = totalRefund, PaymentMethod = request.RefundMethod, Type = "Refund", TransactionId = number, UserId = record.ProcessedByUserId, CreatedDate = DateTime.UtcNow });
            var netPaid = sale.Payments.Where(x => !x.IsDeleted).Sum(x => x.Type == "Refund" ? -x.Amount : x.Amount) - totalRefund;
            sale.PaymentStatus = netPaid <= 0 ? "Refunded" : netPaid < sale.TotalAmount ? "Partial" : "Paid";
            sale.ModifiedDate = DateTime.UtcNow;
            _context.SaleActivities.Add(new SaleActivity { SaleId = saleId, Action = "Return", Description = $"{number}: {request.Reason.Trim()} (${totalRefund:0.00})", NewValues = JsonSerializer.Serialize(new { ReturnNumber = number, Refund = totalRefund, sale.PaymentStatus }), PerformedByUserId = record.ProcessedByUserId, PerformedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            foreach (var repairRefund in repairRefunds)
            {
                await _repairService.AddRepairPaymentAsync(new Empire.Application.DTOs.Repair.AddRepairPaymentRequest
                {
                    RepairId = repairRefund.Key,
                    Type = "Refund",
                    Amount = repairRefund.Value,
                    PaymentMethod = request.RefundMethod,
                    Reference = number,
                    Notes = $"Refund processed from Sales page: {request.Reason.Trim()}",
                    PaidAt = DateTime.UtcNow,
                    UserId = record.ProcessedByUserId
                });
            }
            return new SaleReturnResultDto { Id = record.Id, ReturnNumber = number, RefundAmount = totalRefund, PaymentStatus = sale.PaymentStatus };
        });
    }

    private static int? GetDirectRepairId(string? notes)
    {
        const string prefix = "DirectRepair:";
        if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith(prefix, StringComparison.Ordinal))
            return null;
        return int.TryParse(notes[prefix.Length..], out var id) && id > 0 ? id : null;
    }

    private static int? GetRepairPaymentId(string? transactionId)
    {
        const string prefix = "RepairPayment:";
        if (string.IsNullOrWhiteSpace(transactionId) || !transactionId.StartsWith(prefix, StringComparison.Ordinal))
            return null;
        return int.TryParse(transactionId[prefix.Length..], out var id) && id > 0 ? id : null;
    }

    private async Task<Device?> ResolveDeviceFromSaleItemAsync(SaleItem item, int shopId)
    {
        var request = new SaleItemRequest { ItemName = item.ItemName, Description = item.Description };
        return await ResolveDeviceForSaleItemAsync(request, shopId);
    }

    private static string SaleSnapshot(Sale sale) => JsonSerializer.Serialize(new { sale.CustomerId, sale.SubTotal, sale.TaxAmount, sale.DiscountAmount, sale.TotalAmount, sale.PaymentMethod, sale.PaymentStatus, Items = sale.SaleItems?.Where(x => !x.IsDeleted).Select(x => new { x.Id, x.ItemName, x.Quantity, x.UnitPrice, x.TotalPrice }), Payments = sale.Payments?.Where(x => !x.IsDeleted).Select(x => new { x.Id, x.Amount, x.PaymentMethod, x.Type }) });

    private async Task<Device?> ResolveDeviceForSaleItemAsync(SaleItemRequest item, int shopId)
    {
        if (string.Equals(item.ItemType, "Device", StringComparison.OrdinalIgnoreCase)
            && item.ItemReferenceId.HasValue)
        {
            return await _context.Devices.FirstOrDefaultAsync(d =>
                d.Id == item.ItemReferenceId.Value && d.ShopId == shopId && !d.IsDeleted);
        }

        const string marker = "IMEI:";
        var description = item.Description ?? string.Empty;
        var markerIndex = description.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return null;

        var imeiStart = markerIndex + marker.Length;
        var imeiEnd = description.IndexOf('|', imeiStart);
        var imei = (imeiEnd >= 0 ? description[imeiStart..imeiEnd] : description[imeiStart..]).Trim();
        if (string.IsNullOrWhiteSpace(imei))
            return null;

        return await _context.Devices.FirstOrDefaultAsync(d =>
            d.ShopId == shopId && !d.IsDeleted && d.IMEISerialNumber == imei);
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
        CustomerId     = s.CustomerId,
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
        ItemCount      = s.SaleItems?.Count(i => !i.IsDeleted) ?? 0,
        Items          = s.SaleItems?.Where(i => !i.IsDeleted).Select(i => new SaleItemDto
        {
            Id              = i.Id,
            ItemName        = i.ItemName,
            Description     = i.Description,
            Quantity        = i.Quantity,
            UnitPrice       = i.UnitPrice,
            TotalPrice      = i.TotalPrice,
            CostPrice       = i.CostPrice,
            ItemStatus      = i.ItemStatus,
            IsCustomItem    = i.IsCustomItem,
            InventoryItemId = i.InventoryItemId,
            RepairId        = i.RepairId
        }).ToList() ?? new(),
        Payments       = s.Payments?.Where(p => !p.IsDeleted).Select(p => new SalePaymentDto
        {
            Id            = p.Id,
            PaymentMethod = p.PaymentMethod,
            Amount        = p.Amount,
            TransactionId = p.TransactionId,
            Type          = p.Type
        }).ToList() ?? new()
    };

    // ── Report helpers ────────────────────────────────────────────────────────

    public async Task<SalesReportData?> GetSalesReportAsync(int shopId, DateTime startDate, DateTime endDate)
    {
        try
        {
            await _repairService.SyncDirectRepairPaymentSalesAsync(shopId);
            var sales = await _context.Sales
                .Include(s => s.Customer)
                .Include(s => s.SaleItems)
                .Include(s => s.Payments)
                .Where(s => s.ShopId == shopId && !s.IsDeleted
                         && s.SaleDate >= startDate && s.SaleDate <= endDate)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var totalRevenue = sales.Sum(s => s.TotalAmount - s.Payments.Where(p => !p.IsDeleted && p.Type == "Refund").Sum(p => p.Amount));
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
                    .Select(g => new SalesByDateItem { Date = g.Key, Count = g.Count(), Revenue = g.Sum(s => s.TotalAmount - s.Payments.Where(p => !p.IsDeleted && p.Type == "Refund").Sum(p => p.Amount)) })
                    .OrderBy(x => x.Date).ToList(),
                SalesByPaymentMethod = sales
                    .SelectMany(s => s.Payments ?? new List<Payment>())
                    .GroupBy(p => p.PaymentMethod)
                    .Select(g => new SalesByMethodItem { Method = g.Key, Amount = g.Sum(p => p.Type == "Refund" ? -p.Amount : p.Amount), Count = g.Count() })
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
            // Repair-page payments are synchronized into Sales before either
            // report is calculated. This also backfills payments created by
            // an older build of the POS.
            await _repairService.SyncDirectRepairPaymentSalesAsync(shopId);
            var items = await _context.SaleItems
                .Include(i => i.Sale).ThenInclude(s => s.Customer)
                .Include(i => i.Sale).ThenInclude(s => s.Payments)
                .Where(i => !i.IsDeleted && i.Sale.ShopId == shopId
                         && i.Sale.SaleDate >= startDate && i.Sale.SaleDate <= endDate
                         && !i.Sale.IsDeleted && !i.RepairId.HasValue)
                .ToListAsync();

            var itemIds = items.Select(i => i.Id).ToList();
            var returnedByItem = await _context.SaleReturnItems
                .Where(r => itemIds.Contains(r.SaleItemId))
                .GroupBy(r => r.SaleItemId)
                .Select(g => new { SaleItemId = g.Key, Quantity = g.Sum(x => x.Quantity), Refund = g.Sum(x => x.TotalRefundAmount) })
                .ToDictionaryAsync(x => x.SaleItemId);

            var rows = items.Select(i =>
            {
                var returned = returnedByItem.GetValueOrDefault(i.Id);
                var netQuantity = Math.Max(0, i.Quantity - (returned?.Quantity ?? 0));
                var netRevenue = Math.Max(0, i.TotalPrice - (returned?.Refund ?? 0));
                var netCost = i.CostPrice * netQuantity;
                return new ProfitReportRow
                {
                SaleDate      = i.Sale.SaleDate,
                InvoiceNumber = i.Sale.SaleNumber ?? "",
                Customer      = i.Sale.Customer != null
                                    ? $"{i.Sale.Customer.FirstName} {i.Sale.Customer.LastName}".Trim()
                                    : "",
                ItemName      = i.ItemName,
                Quantity      = netQuantity,
                Revenue       = netRevenue,
                Collected     = CalculateCollectedShare(i),
                Balance       = Math.Max(0, netRevenue - CalculateCollectedShare(i)),
                Cost          = netCost,
                Profit        = netRevenue - netCost,
                Margin        = netRevenue > 0
                                    ? Math.Round((netRevenue - netCost) / netRevenue * 100, 2)
                                    : 0,
                PaymentStatus = i.Sale.PaymentStatus ?? "",
                PaymentMethod = string.Join(", ", i.Sale.Payments
                    .Where(payment => !payment.IsDeleted)
                    .Select(payment => payment.PaymentMethod)
                    .Where(method => !string.IsNullOrWhiteSpace(method))
                    .Distinct()),
                Source = returned == null ? "POS Sale" : "POS Sale (adjusted for return)"
                };
            }).Where(row => row.Quantity > 0 || row.Revenue > 0).ToList();

            // Repair payments accepted directly on the Repair page live in the
            // repair ledger, not the POS payment table. Include each repair once,
            // anchored to its most recent payment date, and exclude repair SaleItems above
            // so later POS checkout cannot double-count the job.
            var paidRepairs = await _context.Repairs
                .AsNoTracking()
                .Include(repair => repair.Customer)
                .Include(repair => repair.Brand)
                .Include(repair => repair.DeviceCategory)
                .Include(repair => repair.DeviceModel)
                .Include(repair => repair.RepairPayments)
                .Include(repair => repair.RepairParts)
                .Where(repair => repair.ShopId == shopId && !repair.IsDeleted &&
                    repair.RepairPayments.Any(payment => !payment.IsDeleted))
                .ToListAsync();

            foreach (var repair in paidRepairs)
            {
                var activePayments = repair.RepairPayments
                    .Where(payment => !payment.IsDeleted)
                    .ToList();
                var latestPaymentDate = activePayments.Max(payment => payment.PaidAt);
                if (latestPaymentDate < startDate || latestPaymentDate > endDate)
                    continue;

                var collected = activePayments
                    .Sum(payment => string.Equals(payment.Type, "Refund", StringComparison.OrdinalIgnoreCase)
                        ? -payment.Amount
                        : payment.Amount);
                var actualPartCost = repair.RepairParts
                    .Where(part => !part.IsDeleted && part.IsAllocatedFromStock)
                    .Sum(part => part.ActualUnitCost * part.Quantity);
                var cost = actualPartCost > 0 ? actualPartCost : repair.PartCost;
                var revenue = repair.Cost;
                rows.Add(new ProfitReportRow
                {
                    SaleDate = latestPaymentDate,
                    InvoiceNumber = repair.RepairNumber,
                    Customer = $"{repair.Customer?.FirstName} {repair.Customer?.LastName}".Trim(),
                    ItemName = string.Join(" — ", new[]
                    {
                        repair.DeviceModel?.Name,
                        repair.Description
                    }.Where(value => !string.IsNullOrWhiteSpace(value))),
                    Brand = repair.Brand?.Name ?? string.Empty,
                    Category = repair.DeviceCategory?.Name ?? "Repair",
                    Quantity = 1,
                    Revenue = revenue,
                    Collected = Math.Max(0, collected),
                    Balance = Math.Max(0, revenue - collected),
                    Cost = cost,
                    Profit = revenue - cost,
                    Margin = revenue > 0 ? Math.Round((revenue - cost) / revenue * 100, 2) : 0,
                    PaymentStatus = repair.PaymentStatus,
                    PaymentMethod = string.Join(", ", activePayments
                        .Select(payment => payment.PaymentMethod)
                        .Where(method => !string.IsNullOrWhiteSpace(method))
                        .Distinct()),
                    Source = "Repair Payment"
                });
            }

            rows = rows.OrderByDescending(row => row.SaleDate).ToList();

            return new ProfitReportData
            {
                Items = rows,
                Summary = new ProfitReportSummary
                {
                    TotalRevenue  = rows.Sum(r => r.Revenue),
                    TotalCollected = rows.Sum(r => r.Collected),
                    OutstandingBalance = rows.Sum(r => r.Balance),
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

    private static decimal CalculateCollectedShare(SaleItem item)
    {
        var sale = item.Sale;
        var totalCollected = sale.Payments
            .Where(payment => !payment.IsDeleted)
            .Sum(payment => payment.Type == "Refund" ? -payment.Amount : payment.Amount);
        if (sale.TotalAmount <= 0)
            return 0;
        var share = item.TotalPrice / sale.TotalAmount;
        return Math.Max(0, Math.Min(item.TotalPrice, Math.Round(totalCollected * share, 2)));
    }

    public async Task<SaleSummaryData?> GetSaleSummaryAsync(int shopId)
    {
        try
        {
            await _repairService.SyncDirectRepairPaymentSalesAsync(shopId);
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
