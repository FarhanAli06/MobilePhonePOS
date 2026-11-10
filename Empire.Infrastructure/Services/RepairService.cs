using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Repair;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Interfaces;
using Empire.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Empire.Infrastructure.Services;

public class RepairService : IRepairService
{
    private readonly IRepairRepository _repairRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IDeviceRepository _deviceRepository;
    private readonly IShopRepository _shopRepository;
    private readonly EmpireDbContext _context;
    private readonly ILogger<RepairService> _logger;

    public RepairService(
        IRepairRepository repairRepository,
        ICustomerRepository customerRepository,
        IDeviceRepository deviceRepository,
        IShopRepository shopRepository,
        EmpireDbContext context,
        ILogger<RepairService> logger)
    {
        _repairRepository = repairRepository;
        _customerRepository = customerRepository;
        _deviceRepository = deviceRepository;
        _shopRepository = shopRepository;
        _context = context;
        _logger = logger;
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
            PartCost = Math.Max(0, request.PartCost),
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
            .FirstOrDefaultAsync(r => r.Id == repairId && r.ShopId == shopId && !r.IsDeleted);

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
            .Where(r => r.ShopId == filter.ShopId && !r.IsDeleted);

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
        var repair = await _context.Repairs
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.InventoryItem)
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.BatchAllocations)
                    .ThenInclude(allocation => allocation.InventoryBatch)
            .FirstOrDefaultAsync(r => r.Id == repairId && !r.IsDeleted);
        if (repair == null)
            return null;

        var previousStatus = repair.Status;
        var returnedPartCount = 0;

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
            if (string.Equals(request.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            {
                var waitingParts = repair.RepairParts
                    .Where(part => !part.IsDeleted && !part.IsAllocatedFromStock)
                    .Select(part => part.InventoryItem?.Name ?? $"Part #{part.InventoryItemId}")
                    .Distinct()
                    .ToList();
                if (waitingParts.Count > 0)
                {
                    throw new InvalidOperationException(
                        $"This repair cannot be completed until these parts are received and assigned: {string.Join(", ", waitingParts)}.");
                }
            }
            repair.Status = request.Status;
            if (request.Status == "Completed")
                repair.CompletedDate = DateTime.UtcNow;
        }

        if (request.PaymentStatus != null)
            repair.PaymentStatus = request.PaymentStatus;

        if (request.Cost.HasValue)
            repair.Cost = request.Cost.Value;
        if (request.PartCost.HasValue)
            repair.PartCost = Math.Max(0, request.PartCost.Value);
        if (request.AmountPaid.HasValue)
            repair.AmountPaid = request.AmountPaid.Value;
        repair.ModifiedBy = modifiedByUserId > 0 ? modifiedByUserId : (int?)null;
        repair.ModifiedDate = DateTime.UtcNow;

        if (request.Status != null && IsInventoryReturnStatus(request.Status) && !IsInventoryReturnStatus(previousStatus))
        {
            returnedPartCount = await ReturnActiveRepairPartsAsync(
                repair,
                modifiedByUserId,
                "STATUS",
                $"Repair status changed to {request.Status}");
        }

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
        if (returnedPartCount > 0) updateDesc.Add($"{returnedPartCount} inventory part line(s) returned to stock");
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

    public async Task<bool> DeleteRepairAsync(int repairId, int shopId, int userId)
    {
        var repair = await _context.Repairs
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.InventoryItem)
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.BatchAllocations)
                    .ThenInclude(allocation => allocation.InventoryBatch)
            .FirstOrDefaultAsync(r => r.Id == repairId && r.ShopId == shopId && !r.IsDeleted);
        if (repair == null)
            return false;

        await ReturnActiveRepairPartsAsync(repair, userId, "TRASH", "Repair moved to Trash");
        repair.IsDeleted = true;
        repair.ModifiedBy = userId > 0 ? userId : repair.ModifiedBy;
        repair.ModifiedDate = DateTime.UtcNow;
        _context.Repairs.Update(repair);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<RepairDto>> GetDeletedRepairsAsync(int shopId)
    {
        var repairs = await _context.Repairs
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceCategory)
            .Include(r => r.DeviceModel)
            .Include(r => r.Shop)
            .Include(r => r.CreatedByUser)
            .Include(r => r.ModifiedByUser)
            .Where(r => r.ShopId == shopId && r.IsDeleted)
            .OrderByDescending(r => r.ModifiedDate ?? r.CreatedDate)
            .ToListAsync();

        return repairs.Select(MapToRepairDto);
    }

    public async Task<bool> RestoreRepairAsync(int repairId, int shopId, int userId)
    {
        const string trashMarker = "[INVENTORY_RETURNED:TRASH]";
        var repair = await _context.Repairs
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.InventoryItem)
                    .ThenInclude(item => item.InventoryBatches)
            .FirstOrDefaultAsync(r => r.Id == repairId && r.ShopId == shopId && r.IsDeleted);
        if (repair == null) return false;

        var partsToRestore = repair.RepairParts
            .Where(part => part.IsDeleted && (part.Notes ?? string.Empty).Contains(trashMarker, StringComparison.Ordinal))
            .ToList();

        foreach (var part in partsToRestore)
        {
            if (!part.IsAllocatedFromStock) continue;
            var item = part.InventoryItem;
            var batchStock = item.InventoryBatches.Where(batch => !batch.IsDeleted).Sum(batch => batch.RemainingQuantity);
            if (item.CurrentStock < part.Quantity || batchStock < part.Quantity)
                throw new InvalidOperationException($"Cannot restore this repair because {item.Name} no longer has {part.Quantity} available in inventory.");
        }

        foreach (var part in partsToRestore)
        {
            if (!part.IsAllocatedFromStock)
            {
                part.IsDeleted = false;
                part.ModifiedDate = DateTime.UtcNow;
                part.Notes = RemoveInventoryReturnMarker(part.Notes, "TRASH");
                continue;
            }
            var item = part.InventoryItem;
            var previousStock = item.CurrentStock;
            var remaining = part.Quantity;
            decimal totalCost = 0m;
            foreach (var batch in item.InventoryBatches
                         .Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                         .OrderBy(batch => batch.ReceivedDate).ThenBy(batch => batch.Id))
            {
                if (remaining == 0) break;
                var used = Math.Min(remaining, batch.RemainingQuantity);
                batch.RemainingQuantity -= used;
                remaining -= used;
                totalCost += used * batch.UnitCost;
                _context.RepairPartBatchAllocations.Add(new RepairPartBatchAllocation
                {
                    RepairPartId = part.Id,
                    InventoryBatchId = batch.Id,
                    Quantity = used,
                    UnitCost = batch.UnitCost,
                    CreatedDate = DateTime.UtcNow
                });
            }

            item.CurrentStock -= part.Quantity;
            part.ActualUnitCost = part.Quantity > 0 ? Math.Round(totalCost / part.Quantity, 2) : 0m;
            part.IsDeleted = false;
            part.ModifiedDate = DateTime.UtcNow;
            part.Notes = RemoveInventoryReturnMarker(part.Notes, "TRASH");
            _context.StockMovements.Add(new StockMovement
            {
                InventoryItemId = item.Id,
                CreatedByUserId = EffectiveUserId(userId, repair),
                MovementType = "OUT",
                Quantity = part.Quantity,
                PreviousStock = previousStock,
                NewStock = item.CurrentStock,
                Reason = "RepairRestored",
                ReferenceNumber = repair.RepairNumber,
                UnitCost = part.ActualUnitCost,
                TotalCost = totalCost,
                MovementDate = DateTime.UtcNow,
                Notes = $"Inventory reapplied when Repair #{repair.RepairNumber} was restored from Trash",
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            });
        }

        repair.IsDeleted = false;
        repair.ModifiedBy = userId > 0 ? userId : repair.ModifiedBy;
        repair.ModifiedDate = DateTime.UtcNow;
        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId = repair.Id,
            Action = "Restored",
            Description = $"Repair restored from Trash. {partsToRestore.Count} inventory part line(s) reapplied.",
            PerformedByUserId = userId > 0 ? userId : (int?)null,
            PerformedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> PermanentlyDeleteAllRepairsAsync(int shopId)
    {
        var repairs = await _context.Repairs
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.InventoryItem)
            .Include(r => r.RepairParts)
                .ThenInclude(rp => rp.BatchAllocations)
                    .ThenInclude(allocation => allocation.InventoryBatch)
            .Where(r => r.ShopId == shopId && r.IsDeleted)
            .ToListAsync();
        if (repairs.Count == 0) return 0;

        foreach (var repair in repairs)
            await ReturnActiveRepairPartsAsync(repair, 0, "TRASH", "Deleted repair inventory recovery");
        _context.Repairs.RemoveRange(repairs);
        await _context.SaveChangesAsync();
        return repairs.Count;
    }

    private static bool IsInventoryReturnStatus(string? status)
    {
        var normalized = (status ?? string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
        return normalized.Equals("Cancelled", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Canceled", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Abandoned", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Unrepairable", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("NotRepairable", StringComparison.OrdinalIgnoreCase);
    }

    private static int EffectiveUserId(int requestedUserId, Repair repair)
        => requestedUserId > 0 ? requestedUserId : repair.ModifiedBy ?? repair.CreatedBy ?? 1;

    private static string? RemoveInventoryReturnMarker(string? notes, string marker)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var cleaned = notes.Replace($"[INVENTORY_RETURNED:{marker}]", string.Empty, StringComparison.Ordinal).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string MarkInventoryReturned(string? notes, string marker)
    {
        var tag = $"[INVENTORY_RETURNED:{marker}]";
        var existing = RemoveInventoryReturnMarker(notes, marker) ?? string.Empty;
        var maximumExistingLength = Math.Max(0, 200 - tag.Length - 1);
        if (existing.Length > maximumExistingLength) existing = existing[..maximumExistingLength].TrimEnd();
        return string.IsNullOrWhiteSpace(existing) ? tag : $"{existing} {tag}";
    }

    private async Task<int> ReturnActiveRepairPartsAsync(Repair repair, int userId, string marker, string reason)
    {
        var activeParts = repair.RepairParts.Where(part => !part.IsDeleted).ToList();
        foreach (var part in activeParts)
        {
            // A requested/backordered part was never removed from inventory, so there is
            // nothing to return when its repair is cancelled or moved to Trash.
            if (!part.IsAllocatedFromStock)
            {
                part.IsDeleted = true;
                part.ModifiedDate = DateTime.UtcNow;
                part.Notes = MarkInventoryReturned(part.Notes, marker);
                continue;
            }
            var item = part.InventoryItem;
            var restoredFromAllocations = 0;
            foreach (var allocation in part.BatchAllocations.Where(allocation => !allocation.IsDeleted))
            {
                allocation.InventoryBatch.RemainingQuantity += allocation.Quantity;
                restoredFromAllocations += allocation.Quantity;
                allocation.IsDeleted = true;
                allocation.ModifiedDate = DateTime.UtcNow;
            }

            if (restoredFromAllocations < part.Quantity)
            {
                var missing = part.Quantity - restoredFromAllocations;
                var recoveryBatch = new InventoryBatch
                {
                    InventoryItemId = item.Id,
                    ReceivedQuantity = missing,
                    RemainingQuantity = missing,
                    UnitCost = part.ActualUnitCost,
                    Vendor = "Repair inventory return",
                    ReferenceNumber = repair.RepairNumber,
                    ReceivedDate = DateTime.UtcNow,
                    Notes = reason,
                    CreatedDate = DateTime.UtcNow
                };
                _context.InventoryBatches.Add(recoveryBatch);
            }

            var previousStock = item.CurrentStock;
            item.CurrentStock += part.Quantity;
            part.IsDeleted = true;
            part.ModifiedDate = DateTime.UtcNow;
            part.Notes = MarkInventoryReturned(part.Notes, marker);
            _context.StockMovements.Add(new StockMovement
            {
                InventoryItemId = item.Id,
                CreatedByUserId = EffectiveUserId(userId, repair),
                MovementType = "IN",
                Quantity = part.Quantity,
                PreviousStock = previousStock,
                NewStock = item.CurrentStock,
                Reason = marker == "TRASH" ? "RepairDeleted" : "RepairCancelled",
                ReferenceNumber = repair.RepairNumber,
                UnitCost = part.ActualUnitCost,
                TotalCost = part.ActualUnitCost * part.Quantity,
                MovementDate = DateTime.UtcNow,
                Notes = $"{reason} for Repair #{repair.RepairNumber}",
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            });
        }
        await Task.CompletedTask;
        return activeParts.Count;
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
            .Where(r => r.CustomerId == customerId && r.ShopId == shopId && !r.IsDeleted)
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
        var lines = new List<(int inventoryItemId, int quantity, decimal unitPrice, string? notes, bool allowBackorder)>();

        if (request.Parts != null && request.Parts.Any())
        {
            // Detailed lines with explicit quantity/price
            foreach (var p in request.Parts)
                lines.Add((p.InventoryItemId, p.Quantity > 0 ? p.Quantity : 1, p.UnitPrice, p.Notes, p.AllowBackorder));
        }
        else if (request.InventoryPartIds != null && request.InventoryPartIds.Any())
        {
            // Legacy flat list — quantity 1, price from inventory
            foreach (var id in request.InventoryPartIds)
                lines.Add((id, 1, 0m, null, false));
        }

        foreach (var (inventoryItemId, quantity, unitPrice, notes, allowBackorder) in lines)
        {
            var item = await _context.InventoryItems
                .Include(i => i.InventoryBatches)
                .FirstOrDefaultAsync(i => i.Id == inventoryItemId);
            if (item == null) continue;

            if (item.CurrentStock < quantity)
            {
                if (!allowBackorder)
                    throw new InvalidOperationException($"Insufficient stock for {item.Name}. Available: {item.CurrentStock}, requested: {quantity}.");

                var pendingPrice = unitPrice > 0 ? unitPrice : item.RetailPrice;
                _context.RepairParts.Add(new RepairPart
                {
                    RepairId = repairId,
                    InventoryItemId = inventoryItemId,
                    Quantity = quantity,
                    UnitPrice = pendingPrice,
                    ActualUnitCost = 0m,
                    Notes = notes,
                    IsAllocatedFromStock = false,
                    StockAllocatedDate = null,
                    IsDeleted = false,
                    CreatedDate = DateTime.UtcNow
                });
                repair.Status = "WaitingForParts";
                repair.ModifiedDate = DateTime.UtcNow;
                _context.RepairActivities.Add(new RepairActivity
                {
                    RepairId = repairId,
                    Action = "PartBackordered",
                    Description = $"Waiting for {quantity} × {item.Name}. Available stock at request time: {item.CurrentStock}.",
                    PerformedByUserId = request.UserId > 0 ? request.UserId : (int?)null,
                    PerformedAt = DateTime.UtcNow
                });
                continue;
            }

            // Backward compatibility: preserve any legacy stock as an opening batch.
            var batchStock = item.InventoryBatches.Where(b => !b.IsDeleted).Sum(b => b.RemainingQuantity);
            if (batchStock < item.CurrentStock)
            {
                var openingQty = item.CurrentStock - batchStock;
                var openingBatch = new InventoryBatch
                {
                    InventoryItemId = item.Id, ReceivedQuantity = openingQty, RemainingQuantity = openingQty,
                    UnitCost = item.CostPrice, Vendor = "Opening inventory", ReferenceNumber = "OPENING",
                    ReceivedDate = item.CreatedDate, Notes = "Legacy stock preserved for FIFO", CreatedDate = DateTime.UtcNow
                };
                _context.InventoryBatches.Add(openingBatch);
                item.InventoryBatches.Add(openingBatch);
            }

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
                IsAllocatedFromStock = true,
                StockAllocatedDate = DateTime.UtcNow,
                Notes = notes,
                IsDeleted = false,
                CreatedDate = DateTime.UtcNow
            };
            _context.RepairParts.Add(repairPart);
            await _context.SaveChangesAsync();

            var remainingToAllocate = quantity;
            decimal actualTotalCost = 0m;
            foreach (var batch in item.InventoryBatches
                         .Where(b => !b.IsDeleted && b.RemainingQuantity > 0)
                         .OrderBy(b => b.ReceivedDate).ThenBy(b => b.Id))
            {
                if (remainingToAllocate == 0) break;
                var used = Math.Min(batch.RemainingQuantity, remainingToAllocate);
                batch.RemainingQuantity -= used;
                actualTotalCost += used * batch.UnitCost;
                remainingToAllocate -= used;
                _context.RepairPartBatchAllocations.Add(new RepairPartBatchAllocation
                {
                    RepairPartId = repairPart.Id, InventoryBatch = batch, Quantity = used,
                    UnitCost = batch.UnitCost, CreatedDate = DateTime.UtcNow
                });
            }
            repairPart.ActualUnitCost = quantity > 0 ? Math.Round(actualTotalCost / quantity, 2) : 0m;

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
                UnitCost = repairPart.ActualUnitCost,
                TotalCost = actualTotalCost,
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
            .Include(rp => rp.BatchAllocations)
                .ThenInclude(a => a.InventoryBatch)
            .Where(rp => rp.RepairId == repairId && !rp.IsDeleted)
            .ToListAsync();

        foreach (var rp in existingParts)
        {
            rp.IsDeleted = true;
            rp.ModifiedDate = DateTime.UtcNow;
            _context.RepairParts.Update(rp);

            if (rp.InventoryItem != null && rp.IsAllocatedFromStock)
            {
                foreach (var allocation in rp.BatchAllocations.Where(a => !a.IsDeleted))
                {
                    allocation.InventoryBatch.RemainingQuantity += allocation.Quantity;
                    allocation.IsDeleted = true;
                    allocation.ModifiedDate = DateTime.UtcNow;
                }
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
                    UnitCost        = rp.ActualUnitCost,
                    TotalCost       = rp.ActualUnitCost * rp.Quantity,
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
            TotalPrice      = rp.TotalPrice,
            ActualUnitCost  = rp.ActualUnitCost,
            IsAllocatedFromStock = rp.IsAllocatedFromStock,
            AvailableStock = rp.InventoryItem?.CurrentStock ?? 0,
            IsStockReady = !rp.IsAllocatedFromStock && (rp.InventoryItem?.CurrentStock ?? 0) >= rp.Quantity
        });
    }

    public async Task<IEnumerable<WaitingPartAlertDto>> GetReadyWaitingPartsAsync(int shopId)
    {
        var now = DateTime.UtcNow;
        var pending = await _context.RepairParts
            .AsNoTracking()
            .Include(part => part.Repair).ThenInclude(repair => repair.Customer)
            .Include(part => part.Repair).ThenInclude(repair => repair.Brand)
            .Include(part => part.Repair).ThenInclude(repair => repair.DeviceModel)
            .Include(part => part.InventoryItem)
            .Where(part => !part.IsDeleted && !part.IsAllocatedFromStock &&
                           !part.Repair.IsDeleted && part.Repair.ShopId == shopId &&
                           part.InventoryItem.CurrentStock >= part.Quantity)
            .OrderBy(part => part.CreatedDate)
            .ToListAsync();

        return pending.Select(part => new WaitingPartAlertDto
        {
            RepairPartId = part.Id,
            RepairId = part.RepairId,
            RepairNumber = part.Repair.RepairNumber ?? string.Empty,
            CustomerName = part.Repair.Customer == null
                ? string.Empty
                : $"{part.Repair.Customer.FirstName} {part.Repair.Customer.LastName}".Trim(),
            CustomerPhone = part.Repair.Customer?.Phone ?? string.Empty,
            Device = string.Join(" ", new[] { part.Repair.Brand?.Name, part.Repair.DeviceModel?.Name }
                .Where(value => !string.IsNullOrWhiteSpace(value))),
            InventoryItemId = part.InventoryItemId,
            PartName = part.InventoryItem.Name,
            SKU = part.InventoryItem.SKU,
            QuantityNeeded = part.Quantity,
            AvailableStock = part.InventoryItem.CurrentStock,
            RequestedDate = part.CreatedDate,
            DaysWaiting = Math.Max(0, (int)(now.Date - part.CreatedDate.Date).TotalDays)
        });
    }

    public async Task<bool> AllocateWaitingPartAsync(int repairPartId, int shopId, int userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var part = await _context.RepairParts
            .Include(value => value.Repair)
            .Include(value => value.InventoryItem).ThenInclude(item => item.InventoryBatches)
            .FirstOrDefaultAsync(value => value.Id == repairPartId && !value.IsDeleted &&
                                          !value.IsAllocatedFromStock && value.Repair.ShopId == shopId &&
                                          !value.Repair.IsDeleted);
        if (part == null) return false;

        var item = part.InventoryItem;
        if (item.CurrentStock < part.Quantity)
            throw new InvalidOperationException($"{item.Name} is no longer available in the required quantity.");

        var batchStock = item.InventoryBatches.Where(batch => !batch.IsDeleted).Sum(batch => batch.RemainingQuantity);
        if (batchStock < item.CurrentStock)
        {
            var openingQuantity = item.CurrentStock - batchStock;
            var openingBatch = new InventoryBatch
            {
                InventoryItemId = item.Id,
                ReceivedQuantity = openingQuantity,
                RemainingQuantity = openingQuantity,
                UnitCost = item.CostPrice,
                Vendor = "Opening inventory",
                ReferenceNumber = "OPENING",
                ReceivedDate = item.CreatedDate,
                Notes = "Legacy stock preserved for FIFO",
                CreatedDate = DateTime.UtcNow
            };
            _context.InventoryBatches.Add(openingBatch);
            item.InventoryBatches.Add(openingBatch);
        }

        var remaining = part.Quantity;
        decimal actualTotalCost = 0m;
        foreach (var batch in item.InventoryBatches
                     .Where(batch => !batch.IsDeleted && batch.RemainingQuantity > 0)
                     .OrderBy(batch => batch.ReceivedDate).ThenBy(batch => batch.Id))
        {
            if (remaining == 0) break;
            var used = Math.Min(batch.RemainingQuantity, remaining);
            batch.RemainingQuantity -= used;
            remaining -= used;
            actualTotalCost += used * batch.UnitCost;
            _context.RepairPartBatchAllocations.Add(new RepairPartBatchAllocation
            {
                RepairPart = part,
                InventoryBatch = batch,
                Quantity = used,
                UnitCost = batch.UnitCost,
                CreatedDate = DateTime.UtcNow
            });
        }

        var previousStock = item.CurrentStock;
        item.CurrentStock -= part.Quantity;
        part.ActualUnitCost = part.Quantity > 0 ? Math.Round(actualTotalCost / part.Quantity, 2) : 0m;
        part.IsAllocatedFromStock = true;
        part.StockAllocatedDate = DateTime.UtcNow;
        part.ModifiedDate = DateTime.UtcNow;

        _context.StockMovements.Add(new StockMovement
        {
            InventoryItemId = item.Id,
            CreatedByUserId = userId > 0 ? userId : 1,
            MovementType = "OUT",
            Quantity = part.Quantity,
            PreviousStock = previousStock,
            NewStock = item.CurrentStock,
            Reason = "RepairUsage",
            ReferenceNumber = part.Repair.RepairNumber ?? part.RepairId.ToString(),
            UnitCost = part.ActualUnitCost,
            TotalCost = actualTotalCost,
            MovementDate = DateTime.UtcNow,
            Notes = $"Allocated to waiting Repair #{part.Repair.RepairNumber}",
            CreatedDate = DateTime.UtcNow
        });
        _context.RepairActivities.Add(new RepairActivity
        {
            RepairId = part.RepairId,
            Action = "WaitingPartAllocated",
            Description = $"{part.Quantity} × {item.Name} allocated from inventory.",
            PerformedByUserId = userId > 0 ? userId : (int?)null,
            PerformedAt = DateTime.UtcNow
        });

        var hasOtherPendingParts = await _context.RepairParts.AnyAsync(value =>
            value.RepairId == part.RepairId && value.Id != part.Id && !value.IsDeleted && !value.IsAllocatedFromStock);
        if (!hasOtherPendingParts && string.Equals(part.Repair.Status, "WaitingForParts", StringComparison.OrdinalIgnoreCase))
        {
            part.Repair.Status = "InProgress";
            part.Repair.ModifiedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
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

        // Payments accepted directly from a repair are real sales activity. POS
        // checkout already creates its own Sale before it writes the linked
        // RepairPayment, so only create a direct-payment sale when no matching
        // POS sale exists. The payment id marker lets edits/deletes stay in sync.
        await SyncDirectRepairPaymentSaleAsync(payment);

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
        await SyncDirectRepairPaymentSaleAsync(payment);

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
        await RemoveDirectRepairPaymentSaleAsync(paymentId);

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

    public async Task SyncDirectRepairPaymentSalesAsync(int shopId)
    {
        var payments = await _context.RepairPayments
            .AsNoTracking()
            .Where(payment => !payment.IsDeleted && payment.Repair.ShopId == shopId && !payment.Repair.IsDeleted)
            .OrderBy(payment => payment.PaidAt)
            .ThenBy(payment => payment.Id)
            .ToListAsync();
        foreach (var payment in payments)
        {
            try
            {
                await SyncDirectRepairPaymentSaleAsync(payment);
            }
            catch (Exception ex)
            {
                // A damaged legacy payment must not prevent every newer repair
                // payment from appearing on the Sales and Profit pages.
                _logger.LogError(ex,
                    "Could not synchronize repair payment {PaymentId} for repair {RepairId}",
                    payment.Id, payment.RepairId);
            }
        }
    }

    private static string DirectRepairSaleMarker(int repairId)
        => $"DirectRepair:{repairId}";

    private static string RepairPaymentTransactionMarker(int paymentId)
        => $"RepairPayment:{paymentId}";

    private async Task SyncDirectRepairPaymentSaleAsync(RepairPayment repairPayment)
    {
        // POS checkout supplies its sale number as the repair-payment reference.
        // That sale already contains the revenue and payment record.
        if (!string.IsNullOrWhiteSpace(repairPayment.Reference) &&
            await _context.Sales.AnyAsync(s => !s.IsDeleted && s.SaleNumber == repairPayment.Reference))
            return;
        if (!string.IsNullOrWhiteSpace(repairPayment.Reference) &&
            await _context.Payments.AnyAsync(payment => !payment.IsDeleted &&
                payment.TransactionId == repairPayment.Reference))
            return;

        var repair = await _context.Repairs
            .Include(r => r.Customer)
            .Include(r => r.Brand)
            .Include(r => r.DeviceModel)
            .FirstOrDefaultAsync(r => r.Id == repairPayment.RepairId && !r.IsDeleted);
        if (repair == null) return;

        var marker = DirectRepairSaleMarker(repair.Id);
        var transactionMarker = RepairPaymentTransactionMarker(repairPayment.Id);
        var repairSaleNumber = $"RPR-{repair.Id:D6}";
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Notes == marker || s.SaleNumber == repairSaleNumber);

        // Older builds could create the repair sale number without the marker.
        // Adopt that row instead of failing on the unique SaleNumber index.
        if (sale != null && string.IsNullOrWhiteSpace(sale.Notes))
            sale.Notes = marker;

        if (sale?.IsDeleted == true && !string.Equals(repairPayment.Type, "Refund", StringComparison.OrdinalIgnoreCase))
        {
            sale.IsDeleted = false;
            var archivedItem = sale.SaleItems.FirstOrDefault();
            if (archivedItem != null) archivedItem.IsDeleted = false;
        }

        // A refund can only attach to an existing direct repair sale.
        if (sale == null && string.Equals(repairPayment.Type, "Refund", StringComparison.OrdinalIgnoreCase))
            return;

        var userId = repairPayment.CreatedBy.GetValueOrDefault(repair.CreatedBy.GetValueOrDefault());
        if (userId <= 0 || !await _context.Users.AnyAsync(user => user.Id == userId && !user.IsDeleted))
        {
            userId = await _context.Users
                .Where(user => !user.IsDeleted && user.IsActive)
                .OrderBy(user => user.Id)
                .Select(user => user.Id)
                .FirstOrDefaultAsync();
        }
        if (userId <= 0)
        {
            _logger.LogWarning(
                "Repair payment {PaymentId} cannot create a sale because no active user exists",
                repairPayment.Id);
            return;
        }
        var itemName = string.Join(" ", new[] { repair.Brand?.Name, repair.DeviceModel?.Name }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(itemName)) itemName = "Repair";

        if (sale == null)
        {
            sale = new Sale
            {
                ShopId = repair.ShopId,
                CustomerId = repair.CustomerId,
                SaleNumber = repairSaleNumber,
                SubTotal = 0,
                TaxAmount = 0,
                DiscountAmount = 0,
                TotalAmount = 0,
                PaymentMethod = repairPayment.PaymentMethod ?? "Cash",
                PaymentStatus = repair.PaymentStatus,
                Notes = marker,
                SaleDate = repairPayment.PaidAt,
                CreatedByUserId = userId,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            };
            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            sale.SaleItems.Add(new SaleItem
            {
                SaleId = sale.Id,
                RepairId = repair.Id,
                ItemName = itemName,
                Description = string.IsNullOrWhiteSpace(repair.Description)
                    ? $"Upfront payment for {repair.RepairNumber}"
                    : repair.Description,
                Quantity = 1,
                UnitPrice = 0,
                TotalPrice = 0,
                CostPrice = 0,
                IsCustomItem = false,
                IsTaxable = false,
                ItemStatus = "Repair",
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            });
            _context.SaleActivities.Add(new SaleActivity
            {
                SaleId = sale.Id,
                Action = "Created",
                Description = $"Created from upfront repair payment for {repair.RepairNumber}",
                PerformedByUserId = userId,
                PerformedAt = DateTime.UtcNow
            });
        }

        var salePayment = sale.Payments.FirstOrDefault(payment =>
            !payment.IsDeleted && payment.TransactionId == transactionMarker);
        if (salePayment == null)
        {
            salePayment = new Payment
            {
                SaleId = sale.Id,
                UserId = userId,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            };
            sale.Payments.Add(salePayment);
        }

        salePayment.Amount = Math.Max(0, repairPayment.Amount);
        salePayment.PaymentMethod = repairPayment.PaymentMethod ?? "Cash";
        salePayment.Type = string.Equals(repairPayment.Type, "Refund", StringComparison.OrdinalIgnoreCase)
            ? "Refund" : "Payment";
        salePayment.TransactionId = transactionMarker;
        salePayment.ModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var directPayments = sale.Payments.Where(payment => !payment.IsDeleted).ToList();
        var grossReceived = directPayments
            .Where(payment => !string.Equals(payment.Type, "Refund", StringComparison.OrdinalIgnoreCase))
            .Sum(payment => payment.Amount);
        sale.SubTotal = grossReceived;
        sale.TotalAmount = grossReceived;
        sale.PaymentMethod = repairPayment.PaymentMethod ?? sale.PaymentMethod;
        sale.PaymentStatus = repair.PaymentStatus;
        var linkedRepairPaymentIds = directPayments
            .Select(payment => ParseRepairPaymentTransactionMarker(payment.TransactionId))
            .Where(paymentId => paymentId.HasValue)
            .Select(paymentId => paymentId!.Value)
            .ToList();
        if (linkedRepairPaymentIds.Count > 0)
        {
            var latestPaymentDate = await _context.RepairPayments
                .Where(payment => linkedRepairPaymentIds.Contains(payment.Id) && !payment.IsDeleted)
                .MaxAsync(payment => (DateTime?)payment.PaidAt);
            if (latestPaymentDate.HasValue)
                sale.SaleDate = latestPaymentDate.Value;
        }
        sale.ModifiedDate = DateTime.UtcNow;
        var saleItem = sale.SaleItems.FirstOrDefault(item => !item.IsDeleted);
        if (saleItem != null)
        {
            saleItem.UnitPrice = grossReceived;
            saleItem.TotalPrice = grossReceived;
            saleItem.ModifiedDate = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    private static int? ParseRepairPaymentTransactionMarker(string? transactionId)
    {
        const string prefix = "RepairPayment:";
        if (string.IsNullOrWhiteSpace(transactionId) ||
            !transactionId.StartsWith(prefix, StringComparison.Ordinal))
            return null;
        return int.TryParse(transactionId[prefix.Length..], out var paymentId) && paymentId > 0
            ? paymentId
            : null;
    }

    private async Task RemoveDirectRepairPaymentSaleAsync(int repairPaymentId)
    {
        var transactionMarker = RepairPaymentTransactionMarker(repairPaymentId);
        var sale = await _context.Sales
            .Include(s => s.SaleItems)
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => !s.IsDeleted &&
                s.Payments.Any(payment => !payment.IsDeleted && payment.TransactionId == transactionMarker));
        if (sale == null) return;

        var removedPayment = sale.Payments.First(payment =>
            !payment.IsDeleted && payment.TransactionId == transactionMarker);
        removedPayment.IsDeleted = true;
        removedPayment.ModifiedDate = DateTime.UtcNow;

        var remainingGross = sale.Payments
            .Where(payment => !payment.IsDeleted && payment.Id != removedPayment.Id &&
                !string.Equals(payment.Type, "Refund", StringComparison.OrdinalIgnoreCase))
            .Sum(payment => payment.Amount);
        sale.SubTotal = remainingGross;
        sale.TotalAmount = remainingGross;
        var repairId = sale.SaleItems.FirstOrDefault(item => !item.IsDeleted)?.RepairId;
        if (repairId.HasValue)
        {
            var repair = await _context.Repairs.AsNoTracking()
                .FirstOrDefaultAsync(value => value.Id == repairId.Value);
            if (repair != null) sale.PaymentStatus = repair.PaymentStatus;
        }
        sale.ModifiedDate = DateTime.UtcNow;
        var saleItem = sale.SaleItems.FirstOrDefault(item => !item.IsDeleted);
        if (saleItem != null)
        {
            saleItem.UnitPrice = remainingGross;
            saleItem.TotalPrice = remainingGross;
        }
        if (remainingGross <= 0)
        {
            sale.IsDeleted = true;
            foreach (var item in sale.SaleItems.Where(i => !i.IsDeleted)) item.IsDeleted = true;
            foreach (var payment in sale.Payments.Where(p => !p.IsDeleted)) payment.IsDeleted = true;
        }
        await _context.SaveChangesAsync();
    }

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
            PartCost = repair.PartCost > 0
                ? repair.PartCost
                : repair.RepairParts?.Where(part => !part.IsDeleted).Sum(part => part.ActualUnitCost * part.Quantity) ?? 0,
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
