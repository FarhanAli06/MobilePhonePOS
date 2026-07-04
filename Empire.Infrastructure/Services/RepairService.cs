using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Repair;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;

namespace Empire.Infrastructure.Services;

public class RepairService : IRepairService
{
    private readonly IRepairRepository _repairRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IShopRepository _shopRepository;
    private readonly EmpireDbContext _context;

    public RepairService(
        IRepairRepository repairRepository,
        ICustomerRepository customerRepository,
        IDeviceRepository deviceRepository,
        IShopRepository shopRepository,
        EmpireDbContext context)
    {
        _repairRepository = repairRepository;
        _customerRepository = customerRepository;
        _deviceRepository = deviceRepository;
        _shopRepository = shopRepository;
        _context = context;
    }

    public async Task<RepairDto> CreateRepairAsync(CreateRepairRequest request, int createdByUserId)
    {
        // Validate that customer exists
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
        if (customer == null || customer.ShopId != request.ShopId)
            throw new ArgumentException("Customer not found or does not belong to the specified shop");

        // Generate repair number
        var repairNumber = await _repairRepository.GenerateRepairNumberAsync(request.ShopId);

        var repair = new Repair
        {
            ShopId = request.ShopId,
            CustomerId = request.CustomerId,
            BrandId = request.BrandId,
            DeviceCategoryId = request.DeviceCategoryId,
            DeviceModelId = request.DeviceModelId,
            RepairNumber = repairNumber,
            Description = request.Description,
            Comments = request.Comments,
            Status = "InProgress", // Default status
            PaymentStatus = request.PaymentStatus.ToString(),
            Cost = request.Cost,
            AmountPaid = request.AmountPaid,
            CompanyId = request.CompanyId,
            CreatedBy = createdByUserId > 0 ? createdByUserId : (int?)null,
            CreatedDate = DateTime.UtcNow
        };

        await _repairRepository.AddAsync(repair);
        await _repairRepository.SaveChangesAsync();

        // Log activity
        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId = repair.Id,
            Action = "Created",
            Description = $"Repair #{repair.RepairNumber} created for {request.Description}",
            NewValues = $"Status={repair.Status}, PaymentStatus={repair.PaymentStatus}, Cost={repair.Cost}",
            PerformedByUserId = createdByUserId > 0 ? createdByUserId : (int?)null,
            PerformedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Reload the repair with navigation properties
        var createdRepair = await _context.Repairs
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .Include(r => r.Shop)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ModifiedByUser)
            .FirstOrDefaultAsync(r => r.Id == repair.Id);

        return MapToRepairDto(createdRepair ?? repair);
    }

    public async Task<RepairDto?> GetRepairByIdAsync(int repairId, int shopId)
    {
        var repair = await _context.Repairs
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .Include(r => r.Shop)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ModifiedByUser)
            .FirstOrDefaultAsync(r => r.Id == repairId && r.ShopId == shopId);

        return repair != null ? MapToRepairDto(repair) : null;
    }

    public async Task<IEnumerable<RepairDto>> GetRepairsAsync(RepairFilterRequest filter)
    {
        var query = _context.Repairs
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .Include(r => r.Shop)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ModifiedByUser)
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.InventoryItem)
                    .ThenInclude(ii => ii.Item)
            .Where(r => r.ShopId == filter.ShopId);

        // Apply date range filter
        if (filter.StartDate.HasValue)
            query = query.Where(r => r.CreatedDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(r => r.CreatedDate <= filter.EndDate.Value.AddDays(1)); // Include end date

        // Apply status filter
        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value.ToString());

        // Apply payment status filter
        if (filter.PaymentStatus.HasValue)
            query = query.Where(r => r.PaymentStatus == filter.PaymentStatus.Value.ToString());

        // Apply customer filter
        if (filter.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == filter.CustomerId.Value);

        // Apply search term filter
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(r => 
                r.RepairNumber.ToLower().Contains(searchTerm) ||
                r.Description.ToLower().Contains(searchTerm) ||
                (r.Customer.FirstName + " " + r.Customer.LastName).ToLower().Contains(searchTerm) ||
                r.Customer.Phone.Contains(searchTerm));
        }

        var repairs = await query
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        return repairs.Select(MapToRepairDto);
    }

    public async Task<RepairDto?> UpdateRepairAsync(int repairId, UpdateRepairRequest request, int modifiedByUserId)
    {
        var repair = await _repairRepository.GetByIdAsync(repairId);
        if (repair == null)
            return null;

        // Update only provided fields
        if (request.BrandId.HasValue)
            repair.BrandId = request.BrandId;

        if (request.DeviceCategoryId.HasValue)
            repair.DeviceCategoryId = request.DeviceCategoryId;

        if (request.DeviceModelId.HasValue)
            repair.DeviceModelId = request.DeviceModelId;

        if (!string.IsNullOrWhiteSpace(request.Description))
            repair.Description = request.Description;

        if (!string.IsNullOrWhiteSpace(request.Comments))
            repair.Comments = request.Comments;

        if (request.Status != null)
        {
            repair.Status = request.Status;
            if (request.Status == "Completed")
                repair.CompletedDate = DateTime.UtcNow;
        }

        if (request.PaymentStatus != null)
            repair.PaymentStatus = request.PaymentStatus;

        if (request.Cost.HasValue)
            repair.Cost = request.Cost.Value;
        if (request.AmountPaid.HasValue)
            repair.AmountPaid = request.AmountPaid.Value;
        repair.ModifiedBy = modifiedByUserId > 0 ? modifiedByUserId : (int?)null;
        repair.ModifiedDate = DateTime.UtcNow;

        await _repairRepository.UpdateAsync(repair);
        await _repairRepository.SaveChangesAsync();

        // When PaymentStatus is not explicitly provided, recalculate from the ledger
        // so it is always consistent with actual payment entries.
        // Wrapped in try/catch: if RepairPayments table doesn't exist yet (migration pending),
        // the repair update still succeeds — payment status will be recalculated on next save.
        if (request.PaymentStatus == null)
        {
            try { await RecalculateAmountPaidAsync(repairId); }
            catch (Exception) { /* Table may not exist yet — migration pending */ }
        }

        // Log activity
        var updateDesc = new System.Collections.Generic.List<string>();
        if (request.Status != null) updateDesc.Add($"Status → {request.Status}");
        if (request.PaymentStatus != null) updateDesc.Add($"PaymentStatus → {request.PaymentStatus}");
        if (request.Cost.HasValue) updateDesc.Add($"Cost → {request.Cost.Value:F2}");
        if (request.AmountPaid.HasValue) updateDesc.Add($"AmountPaid → {request.AmountPaid.Value:F2}");
        if (!string.IsNullOrWhiteSpace(request.Description)) updateDesc.Add("Description updated");
        if (!string.IsNullOrWhiteSpace(request.Comments)) updateDesc.Add("Comments updated");
        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId = repairId,
            Action = "Updated",
            Description = updateDesc.Count > 0 ? string.Join(", ", updateDesc) : "Repair details updated",
            PerformedByUserId = modifiedByUserId > 0 ? modifiedByUserId : (int?)null,
            PerformedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // Reload the repair with navigation properties
        var updatedRepair = await _context.Repairs
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .Include(r => r.Shop)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ModifiedByUser)
            .FirstOrDefaultAsync(r => r.Id == repair.Id);

        return MapToRepairDto(updatedRepair ?? repair);
    }

    public async Task<bool> DeleteRepairAsync(int repairId, int shopId)
    {
        var repair = await _repairRepository.FirstOrDefaultAsync(r => r.Id == repairId && r.ShopId == shopId);
        if (repair == null)
            return false;

        await _repairRepository.DeleteAsync(repair);
        await _repairRepository.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<RepairDto>> GetRepairsByCustomerAsync(int customerId, int shopId)
    {
        var repairs = await _context.Repairs
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .Include(r => r.Shop)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ModifiedByUser)
            .Where(r => r.CustomerId == customerId && r.ShopId == shopId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

        return repairs.Select(MapToRepairDto);
    }

    /// <summary>
    /// Adds inventory parts to an existing repair.
    /// Supports both flat ID list (qty=1 each) and detailed line items with quantity/price.
    /// Records stock movements for each part used.
    /// </summary>
    public async Task<bool> AddPartsToRepairAsync(int repairId, AddRepairPartsRequest request, int shopId)
    {
        var repair = await _context.Repairs
            .FirstOrDefaultAsync(r => r.Id == repairId && r.ShopId == shopId);
        if (repair == null) return false;

        // Build the list of part lines to process
        var lines = new List<(int inventoryItemId, int quantity, decimal unitPrice, string? notes)>();

        if (request.Parts != null && request.Parts.Any())
        {
            // Detailed lines with explicit quantity/price
            foreach (var p in request.Parts)
                lines.Add((p.InventoryItemId, p.Quantity > 0 ? p.Quantity : 1, p.UnitPrice, p.Notes));
        }
        else if (request.InventoryPartIds != null && request.InventoryPartIds.Any())
        {
            // Legacy flat list — quantity 1, price from inventory
            foreach (var id in request.InventoryPartIds)
                lines.Add((id, 1, 0m, null));
        }

        foreach (var (inventoryItemId, quantity, unitPrice, notes) in lines)
        {
            var item = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId);
            if (item == null) continue;

            var effectivePrice = unitPrice > 0 ? unitPrice : item.RetailPrice;
            var previousStock = item.CurrentStock;
            var newStock = previousStock - quantity;

            // Add RepairPart record
            var repairPart = new RepairPart
            {
                RepairId = repairId,
                InventoryItemId = inventoryItemId,
                Quantity = quantity,
                UnitPrice = effectivePrice,
                Notes = notes,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };
            _context.RepairParts.Add(repairPart);

            // Deduct stock via StockMovement
            var movement = new StockMovement
            {
                InventoryItemId = inventoryItemId,
                CreatedByUserId = request.UserId > 0 ? request.UserId : 1,
                MovementType = "OUT",
                Quantity = quantity,
                PreviousStock = previousStock,
                NewStock = newStock < 0 ? 0 : newStock,
                Reason = "RepairUsage",
                ReferenceNumber = repair.RepairNumber ?? repairId.ToString(),
                UnitCost = item.CostPrice,
                TotalCost = item.CostPrice * quantity,
                MovementDate = DateTime.UtcNow,
                Notes = $"Used in Repair #{repair.RepairNumber}",
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };
            _context.StockMovements.Add(movement);

            // Update the item's current stock
            item.CurrentStock = newStock < 0 ? 0 : newStock;
            _context.InventoryItems.Update(item);
        }

        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Replaces all parts on a repair: soft-deletes existing parts (restoring stock),
    /// then adds the new set of parts (deducting stock).
    /// </summary>
    public async Task<bool> ReplacePartsAsync(int repairId, AddRepairPartsRequest request, int shopId)
    {
        var repair = await _context.Repairs
            .FirstOrDefaultAsync(r => r.Id == repairId && r.ShopId == shopId);
        if (repair == null) return false;

        // Step 1: Soft-delete existing parts and restore their stock
        var existingParts = await _context.RepairParts
            .Include(rp => rp.InventoryItem)
            .Where(rp => rp.RepairId == repairId && !rp.IsDeleted)
            .ToListAsync();

        foreach (var rp in existingParts)
        {
            rp.IsDeleted = true;
            rp.ModifiedDate = DateTime.UtcNow;
            _context.RepairParts.Update(rp);

            if (rp.InventoryItem != null)
            {
                var prevStock = rp.InventoryItem.CurrentStock;
                var restoredStock = prevStock + rp.Quantity;
                rp.InventoryItem.CurrentStock = restoredStock;
                _context.InventoryItems.Update(rp.InventoryItem);

                _context.StockMovements.Add(new StockMovement
                {
                    InventoryItemId = rp.InventoryItemId,
                    CreatedByUserId = request.UserId > 0 ? request.UserId : 1,
                    MovementType    = "IN",
                    Quantity        = rp.Quantity,
                    PreviousStock   = prevStock,
                    NewStock        = restoredStock,
                    Reason          = "RepairPartRemoved",
                    ReferenceNumber = repair.RepairNumber ?? repairId.ToString(),
                    UnitCost        = rp.InventoryItem.CostPrice,
                    TotalCost       = rp.InventoryItem.CostPrice * rp.Quantity,
                    MovementDate    = DateTime.UtcNow,
                    Notes           = $"Part removed/replaced on Repair #{repair.RepairNumber}",
                    IsDeleted       = false,
                    CreatedDate     = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        // Step 2: Add the new parts set (only if there are parts to add)
        bool addResult = true;
        if (request.Parts != null && request.Parts.Any())
            addResult = await AddPartsToRepairAsync(repairId, request, shopId);

        // Log parts update activity with detailed added/removed info
        var removedLines = new System.Text.StringBuilder();
        foreach (var rp in existingParts)
        {
            var itemName = rp.InventoryItem?.Name ?? $"Item#{rp.InventoryItemId}";
            removedLines.Append($"  - Removed: {itemName} x{rp.Quantity} @ ${rp.UnitPrice:F2}\n");
        }

        var addedLines = new System.Text.StringBuilder();
        if (request.Parts != null)
        {
            foreach (var p in request.Parts)
            {
                var addedItem = await _context.InventoryItems.FindAsync(p.InventoryItemId);
                var addedName = addedItem?.Name ?? $"Item#{p.InventoryItemId}";
                var effectivePrice = p.UnitPrice > 0 ? p.UnitPrice : (addedItem?.RetailPrice ?? 0);
                addedLines.Append($"  + Added: {addedName} x{p.Quantity} @ ${effectivePrice:F2}\n");
            }
        }

        var partsDesc = "Parts updated on repair.";
        if (removedLines.Length > 0 || addedLines.Length > 0)
            partsDesc = (removedLines.Length > 0 ? removedLines.ToString() : "") +
                        (addedLines.Length > 0 ? addedLines.ToString() : "");

        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId          = repairId,
            Action            = "PartsUpdated",
            Description       = partsDesc.TrimEnd(),
            PerformedByUserId = request.UserId > 0 ? request.UserId : (int?)null,
            PerformedAt       = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return addResult;
    }

    public async Task<IEnumerable<RepairPartDto>> GetRepairPartsAsync(int repairId, int shopId)
    {
        var parts = await _context.RepairParts
            .Include(rp => rp.InventoryItem)
            .Where(rp => rp.RepairId == repairId && !rp.IsDeleted)
            .ToListAsync();
        return parts.Select(rp => new RepairPartDto
        {
            Id              = rp.Id,
            RepairId        = rp.RepairId,
            InventoryItemId = rp.InventoryItemId,
            ItemName        = rp.InventoryItem?.Name ?? string.Empty,
            SKU             = rp.InventoryItem?.SKU  ?? string.Empty,
            Quantity        = rp.Quantity,
            UnitPrice       = rp.UnitPrice,
            TotalPrice      = rp.TotalPrice
        });
    }

    public async Task<IEnumerable<RepairActivityDto>> GetRepairActivitiesAsync(int repairId, int shopId)
    {
        // Verify the repair belongs to the shop
        var repairExists = await _context.Repairs.AnyAsync(r => r.Id == repairId && r.ShopId == shopId);
        if (!repairExists) return Enumerable.Empty<RepairActivityDto>();

        var activities = await _context.RepairActivities
            .Include(a => a.PerformedByUser)
            .Where(a => a.RepairId == repairId)
            .OrderByDescending(a => a.PerformedAt)
            .ToListAsync();

        return activities.Select(a => new RepairActivityDto
        {
            Id = a.Id,
            RepairId = a.RepairId,
            Action = a.Action,
            Description = a.Description,
            OldValues = a.OldValues,
            NewValues = a.NewValues,
            PerformedByUserId = a.PerformedByUserId,
            PerformedByUserName = a.PerformedByUser != null
                ? (string.IsNullOrWhiteSpace($"{a.PerformedByUser.FirstName} {a.PerformedByUser.LastName}".Trim())
                    ? a.PerformedByUser.Username
                    : $"{a.PerformedByUser.FirstName} {a.PerformedByUser.LastName}".Trim())
                : "System",
            PerformedAt = a.PerformedAt,
            PerformedAtFormatted = a.PerformedAt.ToString("MMM dd, yyyy hh:mm tt") + " UTC"
        });
    }

    // ── Payment Ledger ────────────────────────────────────────────────────────

    public async Task<IEnumerable<RepairPaymentDto>> GetRepairPaymentsAsync(int repairId)
    {
        try
        {
        return await _context.RepairPayments
            .Include(p => p.CreatedByUser)
            .Where(p => p.RepairId == repairId)
            .OrderByDescending(p => p.PaidAt)
            .Select(p => new RepairPaymentDto
            {
                Id = p.Id,
                RepairId = p.RepairId,
                Type = p.Type,
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod,
                Reference = p.Reference,
                Notes = p.Notes,
                PaidAt = p.PaidAt,
                CreatedByUser = p.CreatedByUser != null
                    ? (string.IsNullOrWhiteSpace($"{p.CreatedByUser.FirstName} {p.CreatedByUser.LastName}".Trim())
                        ? p.CreatedByUser.Username
                        : $"{p.CreatedByUser.FirstName} {p.CreatedByUser.LastName}".Trim())
                    : "System"
            })
            .ToListAsync();
        }
        catch (Exception) { return Enumerable.Empty<RepairPaymentDto>(); }
    }

    public async Task<RepairPaymentDto> AddRepairPaymentAsync(AddRepairPaymentRequest request)
    {
        var payment = new RepairPayment
        {
            RepairId = request.RepairId,
            Type = request.Type,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            Notes = request.Notes,
            PaidAt = request.PaidAt ?? DateTime.UtcNow,
            CreatedBy = request.UserId
        };
        _context.RepairPayments.Add(payment);
        await _context.SaveChangesAsync();

        await RecalculateAmountPaidAsync(request.RepairId);

        // Log payment activity
        var paymentTypeLabel = (payment.Type ?? "Payment").ToLower() == "refund" ? "Refund" : "Payment";
        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId          = request.RepairId,
            Action            = "PaymentAdded",
            Description       = $"{paymentTypeLabel} of ${payment.Amount:F2} added via {payment.PaymentMethod ?? "Cash"}" +
                                 (string.IsNullOrWhiteSpace(payment.Reference) ? "" : $" (Ref: {payment.Reference})"),
            PerformedByUserId = request.UserId > 0 ? request.UserId : (int?)null,
            PerformedAt       = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _context.Entry(payment).Reference(p => p.CreatedByUser).LoadAsync();
        return new RepairPaymentDto
        {
            Id = payment.Id,
            RepairId = payment.RepairId,
            Type = payment.Type,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Reference = payment.Reference,
            Notes = payment.Notes,
            PaidAt = payment.PaidAt,
            CreatedByUser = payment.CreatedByUser != null
                ? (string.IsNullOrWhiteSpace($"{payment.CreatedByUser.FirstName} {payment.CreatedByUser.LastName}".Trim())
                    ? payment.CreatedByUser.Username
                    : $"{payment.CreatedByUser.FirstName} {payment.CreatedByUser.LastName}".Trim())
                : "System"
        };
    }

    public async Task<RepairPaymentDto?> UpdateRepairPaymentAsync(int paymentId, UpdateRepairPaymentRequest request)
    {
        var payment = await _context.RepairPayments
            .Include(p => p.CreatedByUser)
            .FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null) return null;

        payment.Amount = request.Amount;
        payment.PaymentMethod = request.PaymentMethod;
        payment.Reference = request.Reference;
        payment.Notes = request.Notes;
        if (request.PaidAt.HasValue) payment.PaidAt = request.PaidAt.Value;
        payment.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        await RecalculateAmountPaidAsync(payment.RepairId);

        // Log payment update activity
        var updTypeLabel = (payment.Type ?? "Payment").ToLower() == "refund" ? "Refund" : "Payment";
        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId          = payment.RepairId,
            Action            = "PaymentUpdated",
            Description       = $"{updTypeLabel} #{payment.Id} updated: ${payment.Amount:F2} via {payment.PaymentMethod ?? "Cash"}" +
                                 (string.IsNullOrWhiteSpace(payment.Reference) ? "" : $" (Ref: {payment.Reference})"),
            PerformedByUserId = null,
            PerformedAt       = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new RepairPaymentDto
        {
            Id = payment.Id,
            RepairId = payment.RepairId,
            Type = payment.Type,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            Reference = payment.Reference,
            Notes = payment.Notes,
            PaidAt = payment.PaidAt,
            CreatedByUser = payment.CreatedByUser != null
                ? (string.IsNullOrWhiteSpace($"{payment.CreatedByUser.FirstName} {payment.CreatedByUser.LastName}".Trim())
                    ? payment.CreatedByUser.Username
                    : $"{payment.CreatedByUser.FirstName} {payment.CreatedByUser.LastName}".Trim())
                : "System"
        };
    }

    public async Task<bool> DeleteRepairPaymentAsync(int paymentId)
    {
        var payment = await _context.RepairPayments.FindAsync(paymentId);
        if (payment == null) return false;

        var repairId  = payment.RepairId;
        var amount    = payment.Amount;
        var method    = payment.PaymentMethod ?? "Cash";
        var typeLabel = (payment.Type ?? "Payment").ToLower() == "refund" ? "Refund" : "Payment";
        _context.RepairPayments.Remove(payment);
        await _context.SaveChangesAsync();
        await RecalculateAmountPaidAsync(repairId);

        // Log deletion activity
        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId          = repairId,
            Action            = "PaymentDeleted",
            Description       = $"{typeLabel} of ${amount:F2} via {method} deleted",
            PerformedByUserId = null,
            PerformedAt       = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    // Public wrapper so other services (e.g. POSService) can trigger recalculation
    public async Task RecalculateRepairPaymentStatusAsync(int repairId)
        => await RecalculateAmountPaidAsync(repairId);

    private async Task RecalculateAmountPaidAsync(int repairId)
    {
        var repair = await _context.Repairs.FindAsync(repairId);
        if (repair == null) return;

        var payments = await _context.RepairPayments
            .Where(p => p.RepairId == repairId)
            .ToListAsync();

        var totalPaid     = payments.Where(p => p.Type == "Payment").Sum(p => p.Amount);
        var totalRefunded = payments.Where(p => p.Type == "Refund").Sum(p => p.Amount);
        repair.AmountPaid = totalPaid - totalRefunded;

        var balance = repair.Cost - repair.AmountPaid;
        if (balance <= 0)
            repair.PaymentStatus = "Paid";
        else if (repair.AmountPaid > 0)
            repair.PaymentStatus = "Partial";
        else
            repair.PaymentStatus = "Unpaid";

        repair.ModifiedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private RepairDto MapToRepairDto(Repair repair)
    {
        var repairPartNames = repair.RepairParts?.
            Where(rp => rp.InventoryItem?.Item != null)
            .Select(rp => rp.InventoryItem.Item.Name)
            .Distinct()
            .ToList() ?? new List<string>();

        return new RepairDto
        {
            Id = repair.Id,
            ShopId = repair.ShopId,
            ShopName = repair.Shop?.Name ?? "",
            CustomerId = repair.CustomerId,
            CustomerName = $"{repair.Customer?.FirstName} {repair.Customer?.LastName}".Trim(),
            CustomerPhone = repair.Customer?.Phone ?? "",
            BrandId = repair.BrandId,
            DeviceBrand = repair.Brand?.Name ?? "",
            DeviceCategoryId = repair.DeviceCategoryId,
            DeviceCategory = repair.DeviceCategory?.Name ?? "",
            DeviceModelId = repair.DeviceModelId,
            DeviceModel = repair.DeviceModel?.Name ?? "",
            RepairNumber = repair.RepairNumber,
            Description = repair.Description,
            Comments = repair.Comments,
            Status = repair.Status,
            PaymentStatus = repair.PaymentStatus,
            Cost = repair.Cost,
            AmountPaid = repair.AmountPaid,
            RemainingDues = Math.Max(0, repair.Cost - repair.AmountPaid),
            CreatedDate = repair.CreatedDate,
            CompletedDate = repair.CompletedDate,
            CreatedByUser = repair.CreatedByUser != null
                ? (string.IsNullOrWhiteSpace($"{repair.CreatedByUser.FirstName} {repair.CreatedByUser.LastName}".Trim())
                    ? repair.CreatedByUser.Username
                    : $"{repair.CreatedByUser.FirstName} {repair.CreatedByUser.LastName}".Trim())
                : "Unknown",
            ModifiedByUser = repair.ModifiedByUser != null
                ? (string.IsNullOrWhiteSpace($"{repair.ModifiedByUser.FirstName} {repair.ModifiedByUser.LastName}".Trim())
                    ? repair.ModifiedByUser.Username
                    : $"{repair.ModifiedByUser.FirstName} {repair.ModifiedByUser.LastName}".Trim())
                : null,
            RepairPartNames = repairPartNames
        };
    }
}

