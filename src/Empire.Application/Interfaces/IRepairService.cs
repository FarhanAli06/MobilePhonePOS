using Empire.Application.DTOs.Repair;

namespace Empire.Application.Interfaces;

public interface IRepairService
{
    Task<RepairDto> CreateRepairAsync(CreateRepairRequest request, int createdByUserId);
    Task<RepairDto?> GetRepairByIdAsync(int repairId, int shopId);
    Task<IEnumerable<RepairDto>> GetRepairsAsync(RepairFilterRequest filter);
    Task<RepairDto?> UpdateRepairAsync(int repairId, UpdateRepairRequest request, int modifiedByUserId);
    Task<bool> DeleteRepairAsync(int repairId, int shopId);
    Task<IEnumerable<RepairDto>> GetRepairsByCustomerAsync(int customerId, int shopId);
}

