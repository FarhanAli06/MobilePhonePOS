using Microsoft.EntityFrameworkCore;
using Empire.Application.DTOs.Repair;
using Empire.Application.Interfaces;
using Empire.Domain.Entities;
using Empire.Domain.Enums;
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
            Issue = request.Issue,
            Description = request.Description,
            Comments = request.Comments,
            Status = "InProgress", // Default status
            PaymentStatus = request.PaymentStatus.ToString(),
            Cost = request.Cost,
            CreatedBy = createdByUserId > 0 ? createdByUserId : (int?)null,
            CreatedDate = DateTime.UtcNow
        };

        await _repairRepository.AddAsync(repair);
        await _repairRepository.SaveChangesAsync();

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
                r.Issue.ToLower().Contains(searchTerm) ||
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

        if (!string.IsNullOrWhiteSpace(request.Issue))
            repair.Issue = request.Issue;

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

        repair.ModifiedBy = modifiedByUserId > 0 ? modifiedByUserId : (int?)null;
        repair.ModifiedDate = DateTime.UtcNow;

        await _repairRepository.UpdateAsync(repair);
        await _repairRepository.SaveChangesAsync();

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

    private RepairDto MapToRepairDto(Repair repair)
    {
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
            Issue = repair.Issue,
            Description = repair.Description,
            Comments = repair.Comments,
            Status = repair.Status,
            PaymentStatus = repair.PaymentStatus,
            Cost = repair.Cost,
            CreatedDate = repair.CreatedDate,
            CompletedDate = repair.CompletedDate,
            CreatedByUser = repair.CreatedByUser != null 
                ? $"{repair.CreatedByUser.FirstName} {repair.CreatedByUser.LastName}".Trim() 
                : "Unknown",
            ModifiedByUser = repair.ModifiedByUser != null 
                ? $"{repair.ModifiedByUser.FirstName} {repair.ModifiedByUser.LastName}".Trim() 
                : null
        };
    }
}

